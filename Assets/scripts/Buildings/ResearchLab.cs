using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Специальное здание - научная лаборатория, занимающаяся исследованиями технологий.
/// Наследует функциональность базового здания и добавляет систему исследований.
/// </summary>
public class ResearchLab : Building
{
    [Header("Research Settings")]
    /// <summary>
    /// Список доступных для исследования проектов в этой лаборатории.
    /// </summary>
    public List<ResearchProject> availableProjects;

    /// <summary>
    /// Множитель скорости исследования (1 = нормальная скорость).
    /// </summary>
    public float researchSpeed = 1f;

    /// <summary>
    /// Количество научных очков, производимых лабораторией за день.
    /// </summary>
    public int sciencePointsPerDay = 10;

    private ResearchProject _currentProject;
    private float _researchProgress;
    private float _lastResearchTick;

    /// <summary>
    /// Обновление состояния лаборатории каждый кадр.
    /// </summary>
    /// <remarks>
    /// Если лаборатория функционирует и есть текущий проект исследования,
    /// вызывает обработку исследования раз в секунду.
    /// </remarks>
    private void Update()
    {
        if (State != BuildingState.Operational || _currentProject == null) return;

        if (Time.time - _lastResearchTick > 1f)
        {
            ResearchTick();
            _lastResearchTick = Time.time;
        }
    }

    /// <summary>
    /// Обрабатывает один "тик" исследования.
    /// </summary>
    /// <remarks>
    /// Увеличивает прогресс исследования и проверяет его завершение.
    /// Если проект завершен, вызывает CompleteResearch().
    /// </remarks>
    private void ResearchTick()
    {
        if (_currentProject.IsCompleted) return;

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
    /// <remarks>
    /// Проверяет, не завершен ли проект уже, перед началом исследования.
    /// Сбрасывает прогресс исследования при старте нового проекта.
    /// </remarks>
    public void StartResearch(ResearchProject project)
    {
        if (project.IsCompleted)
        {
            Debug.LogWarning("This research is already completed!");
            return;
        }

        _currentProject = project;
        _researchProgress = 0f;
    }

    /// <summary>
    /// Завершает текущее исследование и применяет его эффекты.
    /// </summary>
    /// <remarks>
    /// Вызывает метод Complete() у текущего проекта и сбрасывает текущий проект.
    /// </remarks>
    private void CompleteResearch()
    {
        _currentProject.Complete();
        Debug.Log($"Research completed: {_currentProject.projectName}");

        // Можно автоматически начать следующий проект или оставить выбор игроку
        _currentProject = null;
    }

    /// <summary>
    /// Улучшает лабораторию, увеличивая производство научных очков.
    /// </summary>
    /// <param name="additionalSciencePoints">Дополнительные очки науки в день</param>
    /// <remarks>
    /// Может быть расширен для добавления визуальных эффектов улучшения.
    /// </remarks>
    public void UpgradeLab(int additionalSciencePoints)
    {
        sciencePointsPerDay += additionalSciencePoints;
        // Можно добавить визуальные эффекты улучшения
    }
}