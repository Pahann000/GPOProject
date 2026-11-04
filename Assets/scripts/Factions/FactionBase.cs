using Mirror;
using UnityEngine;

/// <summary>
/// Маркер базы фракции в мире: ставится сервером в расчищенной зоне и
/// показывает клиентам, где живёт фракция. Спрайт и цвет берутся из
/// <see cref="FactionAtlas"/> по синхронизированной фракции.
/// </summary>
public class FactionBase : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnFactionChanged))]
    private FactionType _faction = FactionType.None;

    /// <summary>Фракция, которой принадлежит база.</summary>
    public FactionType Faction => _faction;

    /// <summary>
    /// Назначает фракцию базы. Вызывается только сервером и обязательно до
    /// <see cref="NetworkServer.Spawn"/>: тогда значение приходит клиентам
    /// в стартовом пакете. Проверяем <see cref="NetworkServer.active"/>,
    /// потому что у не заспавненного объекта isServer ещё false.
    /// </summary>
    public void SetFactionServer(FactionType faction)
    {
        if (!NetworkServer.active) return;

        _faction = faction;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        ApplyVisuals();
    }

    private void OnFactionChanged(FactionType oldFaction, FactionType newFaction)
    {
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        FactionDefinition definition = FactionAtlas.GetDefinition(_faction);
        if (definition == null) return;

        FactionVisuals.ApplyBase(gameObject, definition);
    }
}
