using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WorldConfig", menuName = "Config/World Config")]
public class WorldConfig : ScriptableObject
{
    [Header("Basic Settings")]
    public int WorldHeight = 100;
    public int WorldWidth = 100;
    public int HighAddition = 50;
    public int ChunkSize = 16;

    [Header("Visuals")]
    public BlockAtlas BlockAtlas;
    public Material AtlasMaterial;

    [Header("Noise Settings")]
    public float CaveSize = 0.3f;
    public float CaveFreq = 0.02f;
    public float TerrainFreq = 0.03f;
    public float HighMultiplier = 25f;
    public float GoldFrequency = 0.08f;
    public float GoldSize = 0.8f;
}