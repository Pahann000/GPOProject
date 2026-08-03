using UnityEngine;

[System.Serializable]
public class ErosionSettings
{
    public int numberOfCanyons = 15;
    public int maxDepth = 30;
    public int canyonRadius = 2;
    [Range(0f, 1f)] public float turnProbability = 0.3f;
    public int iterations = 10;
}