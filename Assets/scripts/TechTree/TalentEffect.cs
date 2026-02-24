using UnityEngine;

/// <summary>
/// Абстрактный класс, который хранит методы эффекта таланта.
/// </summary>
[System.Serializable]
public abstract class TalentEffect
{
    /// <summary>
    /// Применение эффекта
    /// </summary>
    public abstract void Apply();
    /// <summary>
    /// Получение описания
    /// </summary>
    /// <returns></returns>
    public abstract string GetDescription();
    /// <summary>
    /// Получение названия
    /// </summary>
    /// <returns></returns>
    public virtual string GetEffectName() => GetType().Name;
}


[System.Serializable]
public class UnlockHumanTechnology : TalentEffect
{

    public override void Apply()
    {
        // Применение 
    }

    public override string GetDescription()
    {
        return $"_______________";
    }
}

[System.Serializable]
public class UnlockMarsSkill : TalentEffect
{
    public UnitType unitType;

    public override void Apply()
    {
        // Применение 
    }

    public override string GetDescription()
    {
        return $"__________________";
    }
}