using NUnit.Framework;
using UnityEngine;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки карты: границы, координаты чанков, кэш блоков, детерминированность шума.
    /// </summary>
    public class MapTests
    {
        private GameObject _mapObject;
        private WorldConfig _config;
        private Map _map;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<WorldConfig>();
            _config.WorldWidth = 4;
            _config.WorldHeight = 4;
            _config.ChunkSize = 16;

            _mapObject = new GameObject("TestMap");
            _map = _mapObject.AddComponent<Map>();
            _map.Initialize(_config, 12345f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_mapObject != null) Object.DestroyImmediate(_mapObject);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void GetChunkPosition_HandlesNegativeCoordinates()
        {
            Assert.AreEqual(new Vector2Int(0, 0), _map.GetChunkPosition(0, 0));
            Assert.AreEqual(new Vector2Int(0, 0), _map.GetChunkPosition(15, 15));
            Assert.AreEqual(new Vector2Int(1, 1), _map.GetChunkPosition(16, 16));
            Assert.AreEqual(new Vector2Int(-1, -1), _map.GetChunkPosition(-1, -1), "нужно FloorToInt, а не усечение к нулю");
            Assert.AreEqual(new Vector2Int(-1, -2), _map.GetChunkPosition(-16, -17));
        }

        [Test]
        public void GetBlockType_OutOfBounds_ReturnsAir()
        {
            // Раньше проверка границ была x > Width, из-за чего чтение уходило
            // за пределы текстуры шума.
            Assert.AreEqual(BlockType.Air, _map.GetBlockType(-1, 0));
            Assert.AreEqual(BlockType.Air, _map.GetBlockType(0, -1));
            Assert.AreEqual(BlockType.Air, _map.GetBlockType(_map.Width, 0));
            Assert.AreEqual(BlockType.Air, _map.GetBlockType(0, _map.Height));
        }

        [Test]
        public void PlaceBlock_OverridesGeneratedNoise()
        {
            _map.PlaceBlock(5, 5, BlockType.Stone);

            Assert.AreEqual(BlockType.Stone, _map.GetBlockType(5, 5));
            Assert.AreEqual(BlockType.Stone, _map.GetBlockInfo(5, 5).tileData.Type);
        }

        [Test]
        public void GetBlockType_IsDeterministic_ForSameSeed()
        {
            var otherConfig = ScriptableObject.CreateInstance<WorldConfig>();
            otherConfig.WorldWidth = 4;
            otherConfig.WorldHeight = 4;
            otherConfig.ChunkSize = 16;

            var otherObject = new GameObject("TestMap2");
            var other = otherObject.AddComponent<Map>();
            other.Initialize(otherConfig, 12345f);

            for (int x = 0; x < 32; x++)
            {
                Assert.AreEqual(_map.GetBlockType(x, 7), other.GetBlockType(x, 7),
                    $"тип блока ({x},7) должен совпадать при одинаковом сиде");
            }

            Object.DestroyImmediate(otherObject);
            Object.DestroyImmediate(otherConfig);
        }
    }
}
