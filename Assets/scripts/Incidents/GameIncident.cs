using UnityEditor;
using UnityEngine;

/// <summary>
/// Базовый класс для любого инцидента.
/// Наследуйтесь от него, чтобы создать Метеорит, Пожар и т.д.
/// </summary>
public abstract class GameIncident : ScriptableObject
{
    [Header("Base Settings")]
    public string IncidentID;
    public string DisplayName;
    [TextArea] public string Desription;

    [Tooltip("Вес инцидента. Чем выше, тем чаще оно выпадает.")]
    public float Weight = 10f;

    /// <summary>
    /// Проверка условия: может ли произойти сечас.
    /// То есть пожара не будет если нет зданий.
    /// </summary>
    public virtual bool CanTrigger(GameKernel kernel) => true;

    /// <summary>
    /// Логика инцидента.
    /// </summary>
    public abstract void Execute(GameKernel kernel);
}