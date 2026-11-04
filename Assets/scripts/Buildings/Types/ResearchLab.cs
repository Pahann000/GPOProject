using UnityEngine;

/// <summary>
/// Научная лаборатория: ускоряет исследования своего владельца.
/// </summary>
/// <remarks>
/// Раньше лаборатория сама хранила текущий проект и прогресс, но у здания
/// (сетевого объекта) нет «игрока», а состояние исследований должно быть
/// серверным и общим для всех лабораторий игрока. Теперь лаборатория только
/// регистрируется в <see cref="ResearchSystem"/> как ускоритель.
/// </remarks>
public class ResearchLab : Building
{
    [Header("Исследования")]
    [Tooltip("Прибавка к скорости исследований владельца, %.")]
    [SerializeField] private float researchSpeedPercent = 25f;

    /// <summary>Прибавка к скорости исследований, которую даёт эта лаборатория.</summary>
    public float ResearchSpeedPercent => Mathf.Max(0f, researchSpeedPercent);

    private ResearchSystem _research;
    private bool _registered;

    public override void Update()
    {
        base.Update();

        if (!isServer) return;
        if (_registered) return;

        // Владелец назначается после спавна, поэтому регистрируемся при первой возможности.
        if (Owner == null) return;
        if (GameKernel.Instance == null) return;
        if (!GameKernel.Instance.TryGetSystem(out _research)) return;

        _research.RegisterLab(Owner, this);
        _registered = true;

        Debug.Log($"[ResearchLab] {name}: ускоряет исследования владельца на {ResearchSpeedPercent:F0}%.");
    }

    protected override void DestroyBuilding()
    {
        Unregister();

        base.DestroyBuilding();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Unregister()
    {
        if (!_registered) return;

        if (_research != null) _research.UnregisterLab(Owner, this);

        _registered = false;
    }
}
