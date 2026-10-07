using UnityEngine;
using Mirror;

/// <summary>
/// Взаимодействие локального игрока с миром: клик по блоку или юниту отдаёт приказ юниту.
/// Преобразование экранных координат в мировые делает клиент (у него есть камера),
/// а сервер работает с уже мировыми координатами — иначе на выделенном сервере
/// выбор попадал не туда, куда кликнул игрок.
/// </summary>
public class PlayerUI : NetworkBehaviour
{
    private Player _player;

    private void Awake()
    {
        // Раньше _player приходил только из OnStartClient (клиентский путь),
        // поэтому на выделенном сервере он оставался null и приказы юнитам
        // отбрасывались: UnitCommandEvent уходил с пустым владельцем.
        _player = GetComponent<Player>();
    }

    public void Shutdown()
    {
        _player = null;
    }

    public void Initialize(Player player)
    {
        _player = player;
    }

    void Update()
    {
        if (!isLocalPlayer) { return; }
        if (!Input.GetMouseButtonDown(0)) { return; }

        Camera playerCamera = Camera.main;
        if (playerCamera == null) { return; }

        // z задаёт плоскость мира: с нулём перспективная камера дала бы точку у самой камеры.
        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = Mathf.Abs(playerCamera.transform.position.z);
        Vector2 worldPoint = playerCamera.ScreenToWorldPoint(screenPoint);

        CmdSelectTarget(worldPoint);
    }

    [Command]
    private void CmdSelectTarget(Vector2 worldPoint)
    {
        if (!isServer) return;

        SelectTarget(worldPoint);
    }

    /// <summary>
    /// Выбирает цель в мировой точке и отдаёт приказ свободному юниту.
    /// </summary>
    /// <param name="worldPoint">Мировая точка клика, уже в координатах мира.</param>
    [Server]
    private void SelectTarget(Vector2 worldPoint)
    {
        if (_player == null) return;

        var kernel = GameKernel.Instance;
        if (kernel == null || !kernel.TryGetSystem(out WorldSystem worldSystem)) return;

        Map map = worldSystem.WorldMap;
        if (map == null) return;

        int x = Mathf.FloorToInt(worldPoint.x);
        int y = Mathf.FloorToInt(worldPoint.y);

        IDamagable target = null;

        // GetBlockInfo, а не GetBlockObj: у второго есть побочный эффект —
        // он навсегда закрепляет запрошенный блок в кэше карты.
        Block selectedBlock = map.GetBlockInfo(x, y);

        if (selectedBlock != null && selectedBlock.tileData.Type != BlockType.Air)
        {
            target = selectedBlock;
        }
        else
        {
            Collider2D targetCollider = Physics2D.OverlapPoint(worldPoint);
            if (targetCollider != null)
            {
                Unit unit = targetCollider.GetComponentInParent<Unit>();
                if (unit != null && unit.Owner != _player)
                {
                    target = unit;
                }
            }
        }

        if (target == null) return;

        kernel.EventBus.Raise(new UnitCommandEvent(target, worldPoint, _player));
    }
}
