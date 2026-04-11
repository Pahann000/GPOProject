using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using static ResourceBundle;
/// <summary>
/// Хранит данные об исследовании.
/// </summary>

[CreateAssetMenu(fileName = "Talent", menuName = "Talents/NewTalent")]
public class TalentData : ScriptableObject
{
    /// <summary>
    /// Название
    /// </summary>
    public string TalentName;

    /// <summary>
    /// Описание исследования.
    /// </summary>
    [CanBeNull]
    [TextArea(3, 5)] public string Description;

    /// <summary>
    /// Иконка.
    /// </summary>
    [CanBeNull]
    public Sprite Icon;

    /// <summary>
    /// Стоимость исследования, может отсутствовать.
    /// </summary>
    [CanBeNull]
    public ResourceCost[] cost;

    /// <summary>
    /// Стоимость исследования
    /// </summary>
    public struct ResourceCost
    {
        public ResourceType type;
        public int amount;
    }

    /// <summary>
    /// Зависимости (предыдущие технологии), могут отсутствовать.
    /// </summary>
    [CanBeNull]
    public List<TalentData> Prerequisites;

    // Состояние в текущей игре
    [System.NonSerialized] public bool IsResearched;
    [System.NonSerialized] public bool IsUnlocked;

    public bool CanUnlock(ResourceManager rm)
    {
        if (IsUnlocked) return false;
        if (Prerequisites[0].Equals(null)) return true;
        foreach (var prereq in Prerequisites)
            if (!prereq.IsUnlocked)
            {
                return false;
            }
        foreach (var c in cost )
            if (rm.GetResource(c.type) < c.amount) return false;
        return true;
    }

    /// <summary>
    /// Позиция в дереве
    /// </summary
    public Vector2 NodePosition; 

}
