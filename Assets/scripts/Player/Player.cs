using UnityEngine;
using Mirror;

/// <summary>
/// Класс, обеспечивающий взаимодействие игрока с игровым миром.
/// </summary>
public class Player : NetworkBehaviour, IChunkObserver
{
    private bool _isRegistered = false;

    private void OnDestroy()
    {
        if (_isRegistered && GameKernel.Instance != null)
        {
            GameKernel.Instance.GetSystem<WorldSystem>()?.WorldChunks?.UnregisterObserver(this);
        }
    }

    // TODO: Необходимо доделать инвентарь игрока.
    /// <summary>
    /// временная реализация инвентаря игрока.
    /// </summary>
    public ObservableDictionary<string, int> Resources { get; } = new ObservableDictionary<string, int>();

    /// <summary>
    /// Положение игрока по X.
    /// </summary>
    public int X => (int)this.transform.position.x;

    /// <summary>
    /// Положение игрока по X.
    /// </summary>
    public int Y => (int)this.transform.position.y;

    /// <inheritdoc/>
    void Update()
    {
        if (!_isRegistered && GameKernel.Instance != null)
        {
            var worldSys = GameKernel.Instance.GetSystem<WorldSystem>();
            if (worldSys != null && worldSys.WorldChunks != null)
            {
                worldSys.WorldChunks.RegisterObserver(this);
                if (isLocalPlayer)
                {
                    worldSys.CmdSpawnUnit(this);
                }
                _isRegistered = true;
                Debug.Log("[Player] Успешно зарегистрирован в системе чанков.");
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
            OnPlayerClicked(new Vector2(mousePos.origin.x, mousePos.origin.y));
        }
    }

    [Command]
    private void OnPlayerClicked(Vector2 position)
    {
        if (isServer)
        {
            SelectTarget(position);
        }
    }

    //TODO: сделать нормальное выделение, и вообще работу игрока с блоками
    /// <summary>
    /// выделяет блок(заглушка заставляющая юнита уничтожить блок).
    /// </summary>
    /// <param name="position">Позиция выделяемого блока.</param>
    [Server]
    private void SelectTarget(Vector2 position)
    {
        IDamagable target = null;
        Block selectedBlock = GameKernel.Instance.GetSystem<WorldSystem>().WorldMap.GetBlockObj(((int)position.x), ((int)position.y));

        if (selectedBlock.tileData.type != BlockType.Air)
        {
            target = selectedBlock;
        }
        else
        {
            Vector3 vector3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var targetCollider = Physics2D.OverlapPoint(new Vector2(vector3.x, vector3.y));
            if (targetCollider)
            {
                Unit unit = targetCollider.gameObject.GetComponent<Unit>();
                if (unit != null && !(unit.Owner == this))
                {
                    target = unit;
                }
                else return;
            }
        }

        if (target != null && GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Raise(new UnitCommandEvent(target, position));
        }
    }
}
