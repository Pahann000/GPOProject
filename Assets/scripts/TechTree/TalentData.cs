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
    public ResourceBundle Cost;

    /// <summary>
    /// Возвращает значение стоимости в виде строки.
    /// </summary>
    /// <returns></returns>
    public string GetCostString()
    {
        if (Cost.Resources == null || Cost.Resources.Count == 0)
            return "";

        string result = "";
        foreach (var resource in Cost.Resources)
        {
            result += $"{resource.Type}: {resource.Amount}\n";
        }
        return result;
    }

    // Проверка, есть ли у технологии стоимость
    public bool HasCost => Cost.Resources != null && Cost.Resources.Count > 0;

    /// <summary>
    /// Зависимости (предыдущие технологии), могут отсутствовать.
    /// </summary>
    [CanBeNull]
    public List<TalentData> Prerequisites;

    // Состояние в текущей игре
    [System.NonSerialized] public bool IsResearched;
    [System.NonSerialized] public bool IsUnlocked;

    /// <summary>
    /// Позиция в дереве
    /// </summary
    public Vector2 NodePosition; 

}
