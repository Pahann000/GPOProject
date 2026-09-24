using UnityEngine;

/// <summary>
/// Описывает атлас, содержащий типы зданий.
/// </summary>
[CreateAssetMenu(fileName = "BuildingAtlas", menuName = "Building atlas")]
public class BuildingAtlas : ScriptableObject
{
    /// <summary>
    /// Дом.
    /// </summary>
    public GameObject House;
}
