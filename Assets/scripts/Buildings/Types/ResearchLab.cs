using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Специальное здание — научная лаборатория, занимающаяся исследованиями технологий.
/// Наследует функциональность базового здания и добавляет систему исследований.
/// </summary>
public class ResearchLab : Building
{
    [Header("Research Settings")]
    [Tooltip("Список доступных для исследования проектов в этой лаборатории.")]
    [SerializeField] private List<ResearchProject> availableProjects = new List<ResearchProject>();

    [Tooltip("Множитель скорости исследования (1 = нормальная скорость).")]
    [SerializeField] private float researchSpeed = 1f;

    [Tooltip("Количество научных очков, производимых лабораторией за день.")]
    [SerializeField] private int sciencePointsPerDay = 10;

    private ResearchProject _currentProject;
    private float _researchProgress = 0f;

    /// <summary>Доступные проекты (только чтение).</summary>
    public IReadOnlyList<ResearchProject> AvailableProjects => availableProjects;

    /// <summary>Текущий исследуемый проект или null.</summary>
    public ResearchProject CurrentProject => _currentProject;

    /// <summary>
    /// Обновление состояния лаборатории каждый кадр.
    /// </summary>
    public override void Update()
    {
        if (!isServer) return;

        // Производство базового класса не теряем.
        base.Update();

        if (State != BuildingState.Operational || _currentProject == null) return;

        // Прогресс идёт непрерывно. Раньше ResearchTick() вызывался раз в секунду,
        // а внутри умножался на Time.deltaTime одного кадра — фактическая скорость
        // исследования зависела от FPS.
        _researchProgress += sciencePointsPerDay * researchSpeed * Time.deltaTime;

        if (_researchProgress >= _currentProject.requiredSciencePoints)
        {
            CompleteResearch();
        }
    }

    /// <summary>
    /// Начинает новый исследовательский проект.
    /// </summary>
    /// <param name="project">Проект для исследования</param>
    public void StartResearch(ResearchProject project)
    {
        if (project == null)
        {
            Debug.LogWarning("[ResearchLab] Проект не передан.");
            return;
        }

        if (project.IsCompleted)
        {
            Debug.LogWarning($"[ResearchLab] Проект {project.projectName} уже завершён.");
            return;
        }

        _currentProject = project;
        _researchProgress = 0f;
    }

    /// <summary>
    /// Завершает текущее исследование и применяет его эффекты.
    /// </summary>
    private void CompleteResearch()
    {
        ResearchProject completed = _currentProject;

        _currentProject = null;
        _researchProgress = 0f;

        completed.Complete();
        Debug.Log($"[ResearchLab] Исследование завершено: {completed.projectName}");
    }

    /// <summary>
    /// Улучшает лабораторию, увеличивая производство научных очков.
    /// </summary>
    /// <param name="additionalSciencePoints">Дополнительные очки науки в день</param>
    public void UpgradeLab(int additionalSciencePoints)
    {
        sciencePointsPerDay += additionalSciencePoints;
    }
}
