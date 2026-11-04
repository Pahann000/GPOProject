using NUnit.Framework;
using UnityEngine;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки завершённой системы строительства: атлас префабов, правила
    /// размещения и стоимость ремонта.
    /// </summary>
    public class BuildingSystemTests
    {
        /// <summary>Правило-заглушка: результат задаётся тестом.</summary>
        private class StubPlacementRule : PlacementRule
        {
            public bool Result;
            public int Calls;

            public override bool IsSatisfied(Vector2 position)
            {
                Calls++;
                return Result;
            }
        }

        [Test]
        public void BuildingAtlas_ReturnsPrefabByType()
        {
            var atlas = ScriptableObject.CreateInstance<BuildingAtlas>();
            var house = new GameObject("HousePrefab");
            var mine = new GameObject("MinePrefab");

            try
            {
                atlas.SetBuildings(new[]
                {
                    new BuildingAtlas.BuildingPrefab { Type = BuildingTypeNames.House, Prefab = house },
                    new BuildingAtlas.BuildingPrefab { Type = BuildingTypeNames.Mine, Prefab = mine }
                });

                Assert.AreSame(house, atlas.GetPrefab(BuildingTypeNames.House));
                Assert.AreSame(mine, atlas.GetPrefab(BuildingTypeNames.Mine));
            }
            finally
            {
                Object.DestroyImmediate(house);
                Object.DestroyImmediate(mine);
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void BuildingAtlas_UnknownType_ReturnsNull()
        {
            var atlas = ScriptableObject.CreateInstance<BuildingAtlas>();

            try
            {
                atlas.SetBuildings(null);
                Assert.IsNull(atlas.GetPrefab(BuildingTypeNames.Turret));
            }
            finally
            {
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void BuildingAtlas_SkipsEntriesWithoutPrefab()
        {
            var atlas = ScriptableObject.CreateInstance<BuildingAtlas>();
            var turret = new GameObject("TurretPrefab");

            try
            {
                atlas.SetBuildings(new[]
                {
                    new BuildingAtlas.BuildingPrefab { Type = BuildingTypeNames.Turret, Prefab = null },
                    new BuildingAtlas.BuildingPrefab { Type = BuildingTypeNames.Turret, Prefab = turret }
                });

                Assert.AreSame(turret, atlas.GetPrefab(BuildingTypeNames.Turret),
                    "пустая запись не должна перекрывать настоящий префаб");
            }
            finally
            {
                Object.DestroyImmediate(turret);
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void BuildingData_WithoutRules_AllowsPlacement()
        {
            var data = ScriptableObject.CreateInstance<BuildingData>();

            try
            {
                data.PlacementRules = null;
                Assert.IsTrue(data.SatisfiesPlacementRules(Vector2.zero));

                data.PlacementRules = new PlacementRule[0];
                Assert.IsTrue(data.SatisfiesPlacementRules(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void BuildingData_FailedRule_BlocksPlacement()
        {
            var data = ScriptableObject.CreateInstance<BuildingData>();
            var rule = ScriptableObject.CreateInstance<StubPlacementRule>();

            try
            {
                rule.Result = false;
                data.PlacementRules = new[] { rule };

                Assert.IsFalse(data.SatisfiesPlacementRules(Vector2.zero));
                Assert.AreEqual(1, rule.Calls);
            }
            finally
            {
                Object.DestroyImmediate(rule);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void BuildingData_PassedRules_AllowPlacement()
        {
            var data = ScriptableObject.CreateInstance<BuildingData>();
            var first = ScriptableObject.CreateInstance<StubPlacementRule>();
            var second = ScriptableObject.CreateInstance<StubPlacementRule>();

            try
            {
                first.Result = true;
                second.Result = true;
                data.PlacementRules = new[] { first, second };

                Assert.IsTrue(data.SatisfiesPlacementRules(new Vector2(3f, 5f)));
                Assert.AreEqual(1, first.Calls, "каждое правило проверяется один раз");
                Assert.AreEqual(1, second.Calls);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void RepairCost_FullHealth_IsNull()
        {
            var cost = new[] { new ResourcePair(ResourceType.Rock, 20) };

            Assert.IsNull(Building.ComputeRepairCost(cost, 100, 100));
            Assert.IsNull(Building.ComputeRepairCost(null, 10, 100));
            Assert.IsNull(Building.ComputeRepairCost(cost, 10, 0));
        }

        [Test]
        public void RepairCost_IsProportionalToMissingHealth()
        {
            var cost = new[] { new ResourcePair(ResourceType.Rock, 100) };

            ResourcePair[] half = Building.ComputeRepairCost(cost, 50, 100);
            ResourcePair[] quarter = Building.ComputeRepairCost(cost, 75, 100);

            Assert.AreEqual(50, half[0].Amount);
            Assert.AreEqual(25, quarter[0].Amount);
        }

        [Test]
        public void RepairCost_AlwaysCostsAtLeastOne()
        {
            var cost = new[] { new ResourcePair(ResourceType.Rock, 10) };

            ResourcePair[] almostDead = Building.ComputeRepairCost(cost, 99, 100);

            Assert.AreEqual(1, almostDead[0].Amount, "почти целое здание всё равно стоит хотя бы один ресурс");
        }

        [Test]
        public void RepairCost_KeepsResourceTypes()
        {
            var cost = new[]
            {
                new ResourcePair(ResourceType.Rock, 20),
                new ResourcePair(ResourceType.Minerals, 10)
            };

            ResourcePair[] result = Building.ComputeRepairCost(cost, 0, 100);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(ResourceType.Rock, result[0].Type);
            Assert.AreEqual(ResourceType.Minerals, result[1].Type);
            Assert.AreEqual(20, result[0].Amount);
            Assert.AreEqual(10, result[1].Amount);
        }

        [Test]
        public void HouseData_MatchesItsPrefabType()
        {
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>(
                "Assets/ScriptableObjects/BuildingData/HouseData.asset");

            if (data == null)
            {
                Assert.Ignore("Ассет HouseData не найден — нужен полный проект.");
            }

            Assert.AreEqual(BuildingTypeNames.House, data.BuildingTypeName);
            Assert.Greater(data.MaxHealth, 0);
            Assert.GreaterOrEqual(data.Width, 1);
            Assert.GreaterOrEqual(data.Height, 1);
        }
    }
}
