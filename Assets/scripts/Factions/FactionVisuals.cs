using UnityEngine;

/// <summary>
/// Применяет к объекту графику его фракции: спрайты, цвет и анимацию.
/// </summary>
/// <remarks>
/// Один и тот же компонент используется юнитами, зданиями и базой фракции.
/// Анимация задаётся данными фракции (<see cref="FactionDefinition"/>):
/// у людей и марсиан свои кадры, частота кадров, покачивание и пульсация цвета.
///
/// Позиция объекта в мире анимируется только у зданий и базы: у юнитов
/// transform двигает сервер и NetworkTransform, поэтому покачивание
/// ограничено сменой кадров и цветом.
/// </remarks>
[DisallowMultipleComponent]
public class FactionVisuals : MonoBehaviour
{
    private SpriteRenderer _renderer;

    private Sprite[] _frames;
    private int _frameIndex;
    private float _frameTimer;
    private float _framesPerSecond = 4f;

    private Color _primary = Color.white;
    private Color _secondary = Color.white;
    private float _pulseSpeed;
    private float _pulseAmount;
    private float _bobAmplitude;
    private float _bobFrequency;
    private bool _animatePosition;

    private Vector3 _baseLocalPosition;
    private float _time;
    private bool _running;

    /// <summary>Рендерер, которым управляет компонент.</summary>
    public SpriteRenderer Renderer
    {
        get
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            }

            return _renderer;
        }
    }

    private void Awake()
    {
        // Базовая позиция нужна для покачивания: запоминаем её до первой анимации.
        _baseLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (!_running) return;

        Animate(Time.deltaTime);
    }

    /// <summary>
    /// Включает анимацию заново с текущей базовой позицией (после телепорта объекта).
    /// </summary>
    public void RebaseLocalPosition()
    {
        _baseLocalPosition = transform.localPosition;
    }

    /// <summary>
    /// Применяет графику фракции.
    /// </summary>
    /// <param name="definition">Описание фракции (может быть null).</param>
    /// <param name="frames">Кадры анимации: один кадр — статичный спрайт.</param>
    /// <param name="fallbackSprite">Спрайт, если у фракции графика не задана.</param>
    /// <param name="animatePosition">Анимировать смещение по Y (для статичных объектов).</param>
    public void Apply(FactionDefinition definition, Sprite[] frames, Sprite fallbackSprite, bool animatePosition)
    {
        _primary = definition != null ? definition.PrimaryColor : Color.white;
        _secondary = definition != null ? definition.SecondaryColor : Color.white;

        _pulseSpeed = definition != null ? Mathf.Max(0f, definition.PulseSpeed) : 0f;
        _pulseAmount = definition != null ? Mathf.Clamp01(definition.PulseAmount) : 0f;

        _bobAmplitude = definition != null ? Mathf.Max(0f, definition.BobAmplitude) : 0f;
        _bobFrequency = definition != null ? Mathf.Max(0f, definition.BobFrequency) : 0f;

        _framesPerSecond = definition != null && definition.FramesPerSecond > 0.01f
            ? definition.FramesPerSecond
            : 4f;

        _frames = frames != null && frames.Length > 0
            ? frames
            : (fallbackSprite != null ? new[] { fallbackSprite } : null);

        _animatePosition = animatePosition && _bobAmplitude > 0f && _bobFrequency > 0f;

        _frameIndex = 0;
        _frameTimer = 0f;
        _time = 0f;
        _baseLocalPosition = transform.localPosition;

        SpriteRenderer target = Renderer;
        if (target != null)
        {
            if (_frames != null && _frames.Length > 0) target.sprite = _frames[0];
            target.color = _primary;
        }

        _running = true;
    }

    /// <summary>
    /// Отключает анимацию и возвращает объект на базовую позицию.
    /// </summary>
    public void Stop()
    {
        _running = false;

        if (_animatePosition) transform.localPosition = _baseLocalPosition;

        SpriteRenderer target = Renderer;
        if (target != null) target.color = Color.white;
    }

    private void Animate(float deltaTime)
    {
        _time += deltaTime;

        AnimateFrames(deltaTime);
        AnimateColor();
        AnimatePosition();
    }

    private void AnimateFrames(float deltaTime)
    {
        if (_frames == null || _frames.Length < 2) return;

        SpriteRenderer target = Renderer;
        if (target == null) return;

        float frameTime = 1f / Mathf.Max(0.01f, _framesPerSecond);
        _frameTimer += deltaTime;

        while (_frameTimer >= frameTime)
        {
            _frameTimer -= frameTime;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            target.sprite = _frames[_frameIndex];
        }
    }

    private void AnimateColor()
    {
        if (_pulseAmount <= 0f) return;

        SpriteRenderer target = Renderer;
        if (target == null) return;

        float phase = Mathf.Sin(_time * _pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
        target.color = Color.Lerp(_primary, _secondary, phase * _pulseAmount);
    }

    private void AnimatePosition()
    {
        if (!_animatePosition) return;

        float bob = Mathf.Sin(_time * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;
        transform.localPosition = _baseLocalPosition + new Vector3(0f, bob, 0f);
    }

    /// <summary>Возвращает компонент на объекте, создавая его при необходимости.</summary>
    public static FactionVisuals Ensure(GameObject target)
    {
        if (target == null) return null;

        FactionVisuals visuals = target.GetComponent<FactionVisuals>();
        if (visuals == null) visuals = target.AddComponent<FactionVisuals>();

        return visuals;
    }

    /// <summary>Графика юнита фракции.</summary>
    public static void ApplyUnit(GameObject target, FactionDefinition definition, UnitTypeName type)
    {
        FactionVisuals visuals = Ensure(target);
        if (visuals == null) return;

        Sprite[] frames = definition != null ? definition.GetUnitFrames(type) : null;
        Sprite fallback = definition != null ? definition.GetUnitIcon(type) : null;

        // Позицию юнита ведёт NetworkTransform, поэтому смещение не анимируем.
        visuals.Apply(definition, frames, fallback, false);
    }

    /// <summary>Графика здания фракции.</summary>
    public static void ApplyBuilding(GameObject target, FactionDefinition definition, BuildingTypeNames type)
    {
        FactionVisuals visuals = Ensure(target);
        if (visuals == null) return;

        Sprite sprite = definition != null ? definition.GetBuildingSprite(type) : null;

        // Здания статичны: покачивание даёт им «живость» и не ломает синхронизацию.
        visuals.Apply(definition, null, sprite, true);
    }

    /// <summary>Графика базы/флага фракции.</summary>
    public static void ApplyBase(GameObject target, FactionDefinition definition)
    {
        FactionVisuals visuals = Ensure(target);
        if (visuals == null) return;

        Sprite sprite = definition != null ? definition.BaseSprite : null;
        visuals.Apply(definition, null, sprite, true);
    }
}
