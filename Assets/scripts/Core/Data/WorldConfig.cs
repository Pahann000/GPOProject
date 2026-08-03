using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WorldConfig", menuName = "Config/World Config")]
public class WorldConfig : ScriptableObject
{
    [Header("Basic Settings")]
    public int WorldHeight = 100;
    public int WorldWidth = 100;
    public int ChunkSize = 16;
    public string SeedString = "";

    [Header("Visuals")]
    public BlockAtlas BlockAtlas;
    public Material AtlasMaterial;

    [Header("Cave Settings")]
    public float CaveSize = 0.25f;
    public float CaveFreq = 0.05f;

    [Header("Drunkard's Walk (Tunnels)")]
    public DrunkardWalkSettings TunnelSettings = new DrunkardWalkSettings();

    [Header("Domain Warping (Resources)")]
    public DomainWarpingSettings MineralWarpSettings = new DomainWarpingSettings 
    { 
        frequency = 0.05f, 
        threshold = 0.6f, 
        warpAmplitude = 5f, 
        warpFrequency = 0.02f, 
        seedOffset = 0 
    };
    public DomainWarpingSettings RootWarpSettings = new DomainWarpingSettings 
    { 
        frequency = 0.04f, 
        threshold = 0.5f, 
        warpAmplitude = 5f, 
        warpFrequency = 0.02f, 
        seedOffset = 1000 
    };
    public DomainWarpingSettings IceWarpSettings = new DomainWarpingSettings 
    { 
        frequency = 0.03f, 
        threshold = 0.7f, 
        warpAmplitude = 5f, 
        warpFrequency = 0.02f, 
        seedOffset = 2000 
    };

    [Header("Erosion (Canyons)")]
    public ErosionSettings CanyonErosionSettings = new ErosionSettings();
}