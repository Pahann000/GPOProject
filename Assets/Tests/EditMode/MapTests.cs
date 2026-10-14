using System.Collections.Generic;
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

        // --- Сохранение: какие блоки попадают в файл и как они восстанавливаются ---

        [Test]
        public void ChangedBlocks_ContainOnlyPlayerEdits()
        {
            int x = 1, y = 1;
            BlockType generated = _map.GetGeneratedBlockType(x, y);
            BlockType placed = generated == BlockType.Air ? BlockType.Stone : BlockType.Air;

            _map.PlaceBlock(x, y, placed);
            var changed = _map.GetChangedBlocks();

            Assert.AreEqual(1, changed.Count, "в сохранение попадает только изменённый блок");
            Assert.AreEqual(x, changed[0].x);
            Assert.AreEqual(y, changed[0].y);
            Assert.AreEqual(placed, changed[0].type);
        }

        [Test]
        public void ChangedBlocks_IgnoreQueriedBlocks()
        {
            // GetBlockInfo и GetBlockObj кладут блок в кэш, но карту не меняют:
            // раньше такие блоки могли уехать в сохранение как «изменения».
            _map.GetBlockInfo(2, 3);
            _map.GetBlockObj(4, 5);

            Assert.AreEqual(0, _map.GetChangedBlocks().Count, "простые запросы блоков не являются правками карты");
        }

        [Test]
        public void ApplyBlockChanges_RestoresMapOnSameSeed()
        {
            int x = 2, y = 2;
            BlockType generated = _map.GetGeneratedBlockType(x, y);
            BlockType placed = generated == BlockType.Air ? BlockType.Gold : BlockType.Air;

            _map.PlaceBlock(x, y, placed);
            var changed = _map.GetChangedBlocks();

            var freshObject = new GameObject("TestMapRestored");
            var fresh = freshObject.AddComponent<Map>();
            fresh.Initialize(_config, 12345f);

            try
            {
                Assert.AreEqual(0, fresh.GetChangedBlocks().Count, "свежая карта правок не имеет");

                fresh.ApplyBlockChanges(changed);

                Assert.AreEqual(placed, fresh.GetBlockType(x, y), "правка должна примениться поверх генерации");
                Assert.AreEqual(changed.Count, fresh.GetChangedBlocks().Count, "и снова считаться правкой");
            }
            finally
            {
                Object.DestroyImmediate(freshObject);
            }
        }

        [Test]
        public void ClearBlockChanges_RevertsMapToGeneration()
        {
            int x = 3, y = 3;
            BlockType generated = _map.GetGeneratedBlockType(x, y);
            BlockType placed = generated == BlockType.Air ? BlockType.Stone : BlockType.Air;

            _map.PlaceBlock(x, y, placed);
            Assert.AreEqual(1, _map.GetChangedBlocks().Count);

            int reverted = _map.ClearBlockChanges();

            Assert.AreEqual(1, reverted, "должна откатиться одна правка");
            Assert.AreEqual(generated, _map.GetBlockType(x, y), "блок возвращается к сгенерированному типу");
            Assert.AreEqual(0, _map.GetChangedBlocks().Count, "правок больше нет");
        }

        [Test]
        public void ClearBlockChanges_KeepsQueriedBlocksAsCache()
        {
            // Запрошенный блок лежит в кэше, но правкой не является: откатывать нечего.
            var cached = _map.GetBlockObj(4, 2);

            int reverted = _map.ClearBlockChanges();

            Assert.AreEqual(0, reverted);
            Assert.AreEqual(cached.tileData.Type, _map.GetBlockType(4, 2));
        }

        [Test]
        public void SaveData_JsonRoundTrip_KeepsSeedAndBlocks()
        {
            var data = new SaveData
            {
                seed = 12345f,
                worldWidth = 64,
                worldHeight = 64,
                chunkSize = 16,
                changedBlocks = new List<BlockChangeData>
                {
                    new BlockChangeData { x = 3, y = 4, type = BlockType.Air },
                    new BlockChangeData { x = 7, y = 8, type = BlockType.Gold }
                }
            };

            var restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data, true));

            Assert.IsNotNull(restored);
            Assert.AreEqual(data.seed, restored.seed);
            Assert.AreEqual(64, restored.worldWidth);
            Assert.AreEqual(16, restored.chunkSize);
            Assert.AreEqual(2, restored.changedBlocks.Count);
            Assert.AreEqual(BlockType.Gold, restored.changedBlocks[1].type);
            Assert.AreEqual(7, restored.changedBlocks[1].x);
        }
    }
}
