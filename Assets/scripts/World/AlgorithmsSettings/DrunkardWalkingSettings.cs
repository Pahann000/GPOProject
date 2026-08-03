using UnityEngine;

[System.Serializable]
public class DrunkardWalkSettings
{
    public int numberOfStartingPoints = 10;
    public int maxStepsPerTunnel = 100;
    public int tunnelRadius = 2;
    [Range(0f, 1f)] public float branchProbability = 0.1f;
    public int maxBranchDepth = 3;
    public int minY = 10;
    public int maxY = 90;
}