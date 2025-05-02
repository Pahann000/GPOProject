using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// �����, �������������� ����������������� ������ � ������� ����������.
/// ����������� �� ScriptableObject ��� �������� ����� ���� ��������� Unity.
/// </summary>
[CreateAssetMenu(fileName = "ResearchProject", menuName = "Research/Project")]
public class ResearchProject : ScriptableObject
{
    /// <summary>
    /// �������� ������������������ �������.
    /// </summary>
    public string projectName;

    /// <summary>
    /// �������� ������� � ��� ��������.
    /// </summary>
    public string description;

    /// <summary>
    /// ���������� ������� �����, ����������� ��� ���������� ������������.
    /// </summary>
    public int requiredSciencePoints;

    /// <summary>
    /// ������ ������������, ������� ������ ���� ��������� ����� ������� ����� �������.
    /// </summary>
    public List<ResearchProject> requiredPrerequisites;

    /// <summary>
    /// ����, ����������� �� ������������� ������������.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// ��������� ����������������� ������ � ��������� ��� �������.
    /// </summary>
    /// <remarks>
    /// ������������� ���� IsCompleted � true � �������� ����� ApplyEffects().
    /// </remarks>
    public void Complete()
    {
        IsCompleted = true;
        ApplyEffects();
    }

    /// <summary>
    /// ��������� ������� ������������ ������������.
    /// </summary>
    /// <remarks>
    /// ����������� �����, ����� ���� ������������� � �������� ������� ��� ���������� ������������� ��������.
    /// �� ��������� ������� � ��� ��������� � ���������� ��������.
    /// </remarks>
    protected virtual void ApplyEffects()
    {
        // ��������� ������� ������������
        Debug.Log($"Applying effects for {projectName}");
    }
}