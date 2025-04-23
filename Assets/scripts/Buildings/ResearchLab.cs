using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ����������� ������ - ������� �����������, ������������ �������������� ����������.
/// ��������� ���������������� �������� ������ � ��������� ������� ������������.
/// </summary>
public class ResearchLab : Building
{
    [Header("Research Settings")]
    /// <summary>
    /// ������ ��������� ��� ������������ �������� � ���� �����������.
    /// </summary>
    public List<ResearchProject> availableProjects;

    /// <summary>
    /// ��������� �������� ������������ (1 = ���������� ��������).
    /// </summary>
    public float researchSpeed = 1f;

    /// <summary>
    /// ���������� ������� �����, ������������ ������������ �� ����.
    /// </summary>
    public int sciencePointsPerDay = 10;

    private ResearchProject _currentProject;
    private float _researchProgress;
    private float _lastResearchTick;

    /// <summary>
    /// ���������� ��������� ����������� ������ ����.
    /// </summary>
    /// <remarks>
    /// ���� ����������� ������������� � ���� ������� ������ ������������,
    /// �������� ��������� ������������ ��� � �������.
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
    /// ������������ ���� "���" ������������.
    /// </summary>
    /// <remarks>
    /// ����������� �������� ������������ � ��������� ��� ����������.
    /// ���� ������ ��������, �������� CompleteResearch().
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
    /// �������� ����� ����������������� ������.
    /// </summary>
    /// <param name="project">������ ��� ������������</param>
    /// <remarks>
    /// ���������, �� �������� �� ������ ���, ����� ������� ������������.
    /// ���������� �������� ������������ ��� ������ ������ �������.
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
    /// ��������� ������� ������������ � ��������� ��� �������.
    /// </summary>
    /// <remarks>
    /// �������� ����� Complete() � �������� ������� � ���������� ������� ������.
    /// </remarks>
    private void CompleteResearch()
    {
        _currentProject.Complete();
        Debug.Log($"Research completed: {_currentProject.projectName}");

        // ����� ������������� ������ ��������� ������ ��� �������� ����� ������
        _currentProject = null;
    }

    /// <summary>
    /// �������� �����������, ���������� ������������ ������� �����.
    /// </summary>
    /// <param name="additionalSciencePoints">�������������� ���� ����� � ����</param>
    /// <remarks>
    /// ����� ���� �������� ��� ���������� ���������� �������� ���������.
    /// </remarks>
    public void UpgradeLab(int additionalSciencePoints)
    {
        sciencePointsPerDay += additionalSciencePoints;
        // ����� �������� ���������� ������� ���������
    }
}