using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Класс юнита, управляемого игроком.
/// Логика (state machine, путь, атака) выполняется только на сервере,
/// а здоровье синхронизируется клиентам.
/// </summary>
public class Unit : NetworkBehaviour, IDamagable, IChunkObserver
{
    /// <summary>Половина высоты коллайдера юнита (в префабе BoxCollider2D 1x1).</summary>
    private const float UnitHalfHeight = 0.5f;

    /// <summary>Запас луча проверки опоры, если в префабе значение не задано.</summary>
    private const float DefaultGroundCheckDistance = 0.15f;

    /// <summary>
    /// Маска опоры по умолчанию: коллайдеры чанков создаются без слоя, то есть на Default.
    /// </summary>
    /// <remarks>
    /// Значение вычисляется лениво: LayerMask.GetMask нельзя вызывать из статического
    /// инициализатора MonoBehaviour — Unity бросает UnityException и тип перестаёт
    /// инициализироваться (TypeInitializationException).
    /// </remarks>
    private static int _defaultGroundMask = -1;

    private static int DefaultGroundMask
    {
        get
        {
            if (_defaultGroundMask < 0)
            {
                _defaultGroundMask = LayerMask.GetMask("Default");
            }

            return _defaultGroundMask;
        }
    }

    private float _speed = 2f;
    private bool _isGrounded = true;

    [Tooltip("Высота прыжка в блоках: должна перекрывать ступеньку рельефа.")]
    [SerializeField] private float jumpHeight = 1.25f;

    [Tooltip("Горизонтальная скорость в прыжке: от неё зависит ширина перепрыгиваемой ямы.")]
    [SerializeField] private float jumpHorizontalSpeed = 3.5f;

    [Tooltip("Пауза между прыжками, сек.")]
    [SerializeField] private float jumpCooldown = 0.6f;

    private float _jumpCooldownTimer;

    private IDamagable _target;
    private UnitWork _unitWork;
    private Rigidbody2D rb;

    /// <summary>Синхронизируемое здоровье.</summary>
    [SyncVar]
    private int _currentHealth;

    // State Machine для AI
    private UnitStateMachine _stateMachine;
    private bool _observerRegistered;

    /// <summary>Буфер и фильтр проверки опоры (не аллоцируются в FixedUpdate).</summary>
    private readonly List<RaycastHit2D> _groundHits = new List<RaycastHit2D>(4);
    private ContactFilter2D _groundFilter;

    [Header("Проверка опоры")]
    [Tooltip("Слои, на которых лежит земля. Пусто = Default.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Длина луча вниз от нижней грани коллайдера юнита.")]
    [SerializeField] private float groundCheckDistance = 0.15f;

    public UnitType unitType;
    public Player Owner;

    /// <summary>
    /// Положение юнита по X.
    /// </summary>
    public int X => (int)transform.position.x;

    /// <summary>
    /// Положение юнита по Y.
    /// </summary>
    public int Y => (int)transform.position.y;

    /// <summary>
    /// Текущее здоровье юнита.
    /// </summary>
    public int CurrentHealth
    {
        get => _currentHealth;
        private set => _currentHealth = value;
    }

    public bool IsGrounded
    {
        get { return _isGrounded; }
    }

    /// <summary>
    /// Скорость передвижения. Берётся из <see cref="UnitType"/>, если он задан:
    /// раньше MoveSpeed из ScriptableObject вообще не использовался.
    /// </summary>
    public float Speed => unitType != null && unitType.MoveSpeed > 0f ? unitType.MoveSpeed : _speed;

    public UnitStateMachine StateMachine
    {
        get
        {
            return _stateMachine;
        }
    }

    public IDamagable Target
    {
        get
        {
            return _target;
        }

        set
        {
            _target = value;
        }
    }

    /// <summary>
    /// Текущая работа юнита.
    /// </summary>
    public UnitWork CurrentUnitWork
    {
        get
        {
            return _unitWork;
        }
        set
        {
            _unitWork = value;
        }
    }

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Фильтр опоры: в проекте включён Queries Start In Colliders, поэтому луч
        // начинается в центре юнита, а попадание в собственный коллайдер отбрасывается.
        _groundFilter = new ContactFilter2D();
        _groundFilter.SetLayerMask(groundMask.value != 0 ? groundMask : DefaultGroundMask);
        _groundFilter.useTriggers = false;

        // Инициализация State Machine
        _stateMachine = new UnitStateMachine(this);

        if (isServer)
        {
            // Раньше здоровье никогда не задавалось из UnitType, поэтому юнит
            // начинал с 0 HP и умирал от первого удара.
            CurrentHealth = unitType != null ? unitType.Health : 1;

            if (GameKernel.Instance != null && GameKernel.Instance.TryGetSystem(out UnitSystem unitSystem))
            {
                unitSystem.RegisterUnit(this);
            }
        }

        TryRegisterObserver();
    }

    private void OnDestroy()
    {
        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (kernel.TryGetSystem(out UnitSystem unitSystem))
        {
            unitSystem.UnregisterUnit(this);
        }

        if (_observerRegistered && kernel.TryGetSystem(out WorldSystem worldSystem) &&
            worldSystem.WorldChunks != null)
        {
            worldSystem.WorldChunks.UnregisterObserver(this);
        }
    }

    /// <summary>
    /// Регистрирует юнита как наблюдателя чанков. Вызывается повторно из Update,
    /// пока не сработает: порядок Start у объектов недетерминирован, и раньше
    /// юнит мог молча не подписаться на генерацию чанков.
    /// </summary>
    private void TryRegisterObserver()
    {
        if (_observerRegistered) return;

        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (!kernel.TryGetSystem(out WorldSystem worldSystem) || worldSystem.WorldChunks == null) return;

        worldSystem.WorldChunks.RegisterObserver(this);
        _observerRegistered = true;
    }

    private void Update()
    {
        // AI, путь и атака — серверная логика. На клиентах она только жгла CPU
        // и пыталась писать в состояние, которым владеет сервер.
        if (!isServer) return;

        if (_jumpCooldownTimer > 0f)
        {
            _jumpCooldownTimer -= Time.deltaTime;
        }

        TryRegisterObserver();

        // Обновляем State Machine
        _stateMachine?.Update();
    }

    private void FixedUpdate()
    {
        if (!isServer) return;

        // Луч идёт из центра коллайдера вниз на его половину плюс запас: так он
        // достаёт до опоры и не «промахивается» на самой границе коллайдера,
        // из-за чего IsGrounded почти всегда был false и прыжок не срабатывал.
        // Запас берём не меньше значения по умолчанию: в префабе юнита
        // groundCheckDistance выставлен в 0, и луч упирался ровно в границу.
        float checkDistance = groundCheckDistance > 0.01f ? groundCheckDistance : DefaultGroundCheckDistance;

        int hitCount = Physics2D.Raycast(transform.position, Vector2.down, _groundFilter, _groundHits,
            UnitHalfHeight + checkDistance);

        _isGrounded = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _groundHits[i].collider;
            if (hit == null || hit.gameObject == gameObject) continue;   // своё попадание не считаем

            _isGrounded = true;
            break;
        }
    }

    public void Jump(Vector2 direction)
    {
        if (rb == null || !CanJump) return;

        // Скорость прыжка считаем из желаемой высоты в блоках: при жёстко заданной
        // силе (было 4) высота выходила 0.82 блока, и юнит не мог перелезть
        // ступеньку в один блок — упирался в неё и стоял.
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float jumpVelocity = gravity > 0f ? Mathf.Sqrt(2f * gravity * jumpHeight) : jumpHeight;

        // Горизонтальный импульс нужен для прыжка через яму: при ходьбе скорость
        // всего 1 блок/с, и без разгона юнит не перелетит провал.
        float horizontal = rb.linearVelocity.x;
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            horizontal = Mathf.Sign(direction.x) * jumpHorizontalSpeed;
        }

        // Спящее тело проигнорировало бы прыжок так же, как движение.
        rb.WakeUp();

        rb.linearVelocity = new Vector2(horizontal, jumpVelocity);
        _isGrounded = false;
        _jumpCooldownTimer = jumpCooldown;
    }

    /// <summary>Готов ли юнит прыгнуть: он на земле и пауза между прыжками прошла.</summary>
    public bool CanJump => _isGrounded && _jumpCooldownTimer <= 0f;

    /// <summary>
    /// Правила перемещения для поиска пути, согласованные с физикой юнита:
    /// подъём берётся из высоты прыжка, ширина перепрыгиваемой ямы — из дальности
    /// прыжка (горизонтальная скорость умножается на время полёта).
    /// </summary>
    public GridPathfinder.MoveRules MoveRules
    {
        get
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y);
            if (rb != null) gravity *= Mathf.Abs(rb.gravityScale);
            if (gravity <= 0f) gravity = 9.81f;

            float airTime = 2f * Mathf.Sqrt(2f * jumpHeight / gravity);   // вверх и вниз
            int maxGap = Mathf.Max(0, Mathf.FloorToInt(jumpHorizontalSpeed * airTime) - 1);
            int maxStepUp = Mathf.Max(0, Mathf.FloorToInt(jumpHeight));

            return new GridPathfinder.MoveRules(maxStepUp, 2, maxGap, 10, 8, 3, 35, 10);
        }
    }

    /// <summary>
    /// Задает горизонтальное направление движения (серверная логика).
    /// Движение идёт через Rigidbody2D
    /// </summary>
    public void SetMoveDirection(Vector2 direction)
    {
        if (rb == null) return;

        // WakeUp обязателен: Box2D не будит тело при присваивании скорости,
        // а уснувшее тело её игнорирует — юнит «полз» со скоростью 0.07 вместо 1.
        rb.WakeUp();

        rb.linearVelocity = new Vector2(direction.x * Speed * 2, rb.linearVelocity.y);
    }

    /// <summary>
    /// Останавливает горизонтальное движение, сохраняя вертикальную скорость
    /// (падение или прыжок продолжаются).
    /// </summary>
    public void StopMoving()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Начать последовательность: движение -> атака
    /// </summary>
    public void SetTarget(IDamagable target)
    {
        if (!isServer) { return; }

        _stateMachine.SetTarget(target);
    }

    /// <summary>
    /// Наносит урон юниту. Значение урона уже итоговое — множители
    /// (солдат бьёт сильнее) считает атакующий в своём состоянии.
    /// </summary>
    /// <param name="amount"> Количество урона </param>
    /// <param name="Damager"> Игрок, нанёсший урон. </param>
    public void TakeDamage(int amount, Player Damager)
    {
        if (!isServer || amount <= 0) return;
        if (CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (CurrentHealth == 0)
        {
            _stateMachine?.Kill();
        }
    }
}
