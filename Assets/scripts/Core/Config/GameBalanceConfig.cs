using UnityEngine;

/// <summary>
/// Конфигурация баланса игры (ScriptableObject).
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Mars Colony Builder/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Ресурсы")]
    [Tooltip("Стартовые значения ресурсов")]
    public ResourcePair[] StartingResources;
    
    [Tooltip("Максимальные лимиты хранилища для каждого ресурса")]
    public ResourcePair[] DefaultStorageLimits;

    [Header("Производство")]
    [Tooltip("Стандартное время производственного цикла в секундах")]
    public float DefaultProductionCycleSeconds = 5f;

    [Header("Здания")]
    [Tooltip("Процент стоимости при возврате ресурсов при уничтожении здания")]
    [Range(0f, 100f)]
    public float DestructionReturnPercentage = 50f;

    [Header("Юниты")]
    [Tooltip("Стартовое здоровье юнита")]
    public int DefaultUnitHealth = 100;

    [Tooltip("Скорость движения юнита")]
    public float DefaultUnitSpeed = 2f;

    [Tooltip("Сила прыжка юнита")]
    public float DefaultUnitJumpForce = 4f;

    [Header("Игрок")]
    [Tooltip("Время регистрации игрока в системе")]
    public float PlayerRegistrationTime = 1f;
}
