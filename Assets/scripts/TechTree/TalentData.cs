using JetBrains.Annotations;
using UnityEngine;
using static ResourceBundle;
/// <summary>
/// Хранит данные об исследовании.
/// </summary>

[CreateAssetMenu(fileName = "Talent", menuName = "Scriptable Objects/NewTalent")]
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
    public ResourceBundle ResearchCost;

    /// <summary>
    /// Зависимости (предыдущие технологии), могут отсутствовать.
    /// </summary>
    [CanBeNull]
    public TalentData[] Prerequisites;

    // Эффекты при разблокировке
    public TalentEffect[] UnlockEffects;

    // Состояние в текущей игре
    [System.NonSerialized] public bool IsResearched;
    [System.NonSerialized] public bool IsAvailable;

    // На будущее
    /// <summary>
    /// Позиция в дереве
    /// </summary
    public Vector2 NodePosition; 

}
