using UnityEngine;

/// <summary>
/// ����� ��� ��������� ����� ����.
/// </summary>
public class WorldManager : MonoBehaviour
{
    /// <summary>
    /// ������ �������� ���� ����.
    /// </summary>
    private Texture2D _worldNoise;
    /// <summary>
    /// ������ �������� ���� ������.
    /// </summary>
    private Texture2D _goldNoise;

    public Map map;

    [Header("Noise settings")]
    /// <summary>
    /// ������ ����� � ���� (������ �������� ������ �����).
    /// </summary>
    public float CaveSize = 0.25f;
    /// <summary>
    /// ������� ��������� �����.
    /// </summary>
    public float CaveFreq = 0.05f;
    /// <summary>
    /// ������� ��������� �����������.
    /// ������ �������� - ������ �����������.
    /// </summary>
    public float TerrainFreq = 0.04f;
    /// <summary>
    /// ��������� ������ �����������.
    /// ������ �������� - ������ �����������.
    /// </summary>
    public float HighMultiplier = 25f;

    [Header("World generation")]
    /// <summary>
    /// ��� ����.
    /// </summary>
    public float Seed;
    /// <summary>
    /// ������ ������.
    /// </summary>
    public int chunkSize = 16;
    /// <summary>
    /// ������ ���� � ������.
    /// </summary>
    public int WorldWidth = 10;
    /// <summary>
    /// ������ ���� � ������.
    /// </summary>
    public int WorldHigh = 100;
    /// <summary>
    /// ������ ����������� � �������. 
    /// </summary>
    public int HighAddition = 50;

    [Header("Ores")]
    /// <summary>
    /// ������� ��������� ������.
    /// <summary/>
    public float GoldFrequency;
    /// <summary>
    /// ������ ��������� ������ (������ �������� ������ ����).
    /// </summary>
    public float GoldSize;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Seed = Random.Range(-100000, 100000);
        _worldNoise = GenerateNoiseTexture(CaveFreq, CaveSize);
        _goldNoise = GenerateNoiseTexture(GoldFrequency, GoldSize);
        GenerateTerrain();
    }

    /// <summary>
    /// ���������� ����������� � ����������� ����� ���� � ����.
    /// </summary>
    private void GenerateTerrain()
    {
        for (int x = 0; x < _worldNoise.width; x++)
        {
            float height = Mathf.PerlinNoise((x + Seed) * TerrainFreq, Seed * TerrainFreq) * HighMultiplier + HighAddition;
            for (int y = 0; y < height; y++)
            {
                TileType type = TileType.Air;
                if (_goldNoise.GetPixel(x, y).r > 0.5f)
                {
                    type = TileType.Gold;
                }
                else if (_worldNoise.GetPixel(x, y).r > 0.5f)
                {
                    type = TileType.Stone;
                }

                map.SetTile(x, y, type);
            }
        }
    }

    /// <summary>
    /// ���������� ����� ���� �������.
    /// </summary>
    /// <param name="frequency">������� ��������� �������� �������</param>
    /// <returns>Texture2D</returns>
    private Texture2D GenerateNoiseTexture(float frequency, float limit)
    {
        Texture2D noise = new Texture2D(WorldWidth * chunkSize, WorldHigh);
        for (int x = 0; x < noise.width; x++)
        {
            for (int y = 0; y < noise.height; y++)
            {
                float v = Mathf.PerlinNoise((x + Seed) * frequency, (y + Seed) * frequency);

                if (v > limit)
                {
                    noise.SetPixel(x, y, Color.white);
                }
                else
                {
                    noise.SetPixel(x, y, Color.black);
                }
            }
        }

        noise.Apply();

        return noise;
    }
}
