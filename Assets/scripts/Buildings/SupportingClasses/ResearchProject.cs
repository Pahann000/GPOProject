using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Класс, представляющий исследовательский проект в системе технологий.
/// Наследуется от ScriptableObject для создания через меню редактора Unity.
/// </summary>
[CreateAssetMenu(fileName = "ResearchProject", menuName = "Research/Project")]
public class ResearchProject : ScriptableObject
{
    /// <summary>
    /// Название исследовательского проекта.
    /// </summary>
    public string projectName;

    /// <summary>
    /// Описание проекта и его эффектов.
    /// </summary>
    public string description;

    /// <summary>
    /// Количество научных очков, необходимых для завершения исследования.
    /// </summary>
    public int requiredSciencePoints;

    /// <summary>
    /// Список исследований, которые должны быть завершены перед началом этого проекта.
    /// </summary>
    public List<ResearchProject> requiredPrerequisites;

    /// <summary>
    /// Флаг, указывающий на завершенность исследования.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Завершает исследовательский проект и применяет его эффекты.
    /// </summary>
    /// <remarks>
    /// Устанавливает флаг IsCompleted в true и вызывает метод ApplyEffects().
    /// </remarks>
    public void Complete()
    {
        IsCompleted = true;
        ApplyEffects();
    }

    /// <summary>
    /// Применяет эффекты завершенного исследования.
    /// </summary>
    /// <remarks>
    /// Виртуальный метод, может быть переопределен в дочерних классах для реализации специфических эффектов.
    /// По умолчанию выводит в лог сообщение о применении эффектов.
    /// </remarks>
    protected virtual void ApplyEffects()
    {
        // Применяем эффекты исследования
        Debug.Log($"Applying effects for {projectName}");
    }
}