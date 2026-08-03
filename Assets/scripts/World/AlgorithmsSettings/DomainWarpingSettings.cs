using UnityEngine;

[System.Serializable]
public class DomainWarpingSettings
{
    public float frequency = 0.05f;
    [Range(0f, 1f)] public float threshold = 0.6f;
    public float warpAmplitude = 5f;
    public float warpFrequency = 0.02f;
    public int seedOffset = 0;
}