using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки древа исследований: требования, прогресс, скорость, открытия и бонусы.
    /// </summary>
    public class ResearchTests
    {
        /// <summary>Готовит маленькое тестовое дерево: база → продвинутое.</summary>
        private static ResearchAtlas CreateAtlas(out ResearchProject basic, out ResearchProject advanced)
        {
            basic = ScriptableObject.CreateInstance<ResearchProject>();
            basic.projectName = "База";
            basic.durationSeconds = 10f;
            basic.cost = new[] { new ResourcePair(ResourceType.Rock, 10) };
            basic.unlockBuildings = new[] { BuildingTypeNames.Mine };
            basic.productionBonusPercent = 10f;

            advanced = ScriptableObject.CreateInstance<ResearchProject>();
            advanced.projectName = "Продвинутое";
            advanced.durationSeconds = 20f;
            advanced.requiredPrerequisites = new List<ResearchProject> { basic };
            advanced.unlockBuildings = new[] { BuildingTypeNames.Turret };
            advanced.unlockUnits = new[] { UnitTypeName.Soldier };
            advanced.productionBonusPercent = 15f;

            var atlas = ScriptableObject.CreateInstance<ResearchAtlas>();
            atlas.projects = new List<ResearchProject> { basic, advanced };
            atlas.defaultBuildings = new[] { BuildingTypeNames.House };
            atlas.defaultUnits = new[] { UnitTypeName.Miner };

            return atlas;
        }

        [Test]
        public void CanStart_RequiresResources()
        {
            CreateAtlas(out ResearchProject basic, out _);

            try
            {
                Assert.IsTrue(ResearchRules.CanStart(basic, 0, new HashSet<int>(), null, true, out _));

                Assert.IsFalse(ResearchRules.CanStart(basic, 0, new HashSet<int>(), null, false, out string reason));
                StringAssert.Contains("ресурс", reason);
            }
            finally
            {
                Object.DestroyImmediate(basic);
            }
        }

        [Test]
        public void CanStart_RejectsCompletedAndMissingPrerequisites()
        {
            var atlas = CreateAtlas(out ResearchProject basic, out ResearchProject advanced);

            try
            {
                Assert.IsFalse(ResearchRules.CanStart(basic, 0, new HashSet<int> { 0 }, atlas, true, out _),
                    "уже изученное исследование повторно не начинается");

                Assert.IsFalse(ResearchRules.CanStart(advanced, 1, new HashSet<int>(), atlas, true, out _),
                    "без базового исследования продвинутое недоступно");

                Assert.IsTrue(ResearchRules.CanStart(advanced, 1, new HashSet<int> { 0 }, atlas, true, out _),
                    "после базового продвинутое доступно");
            }
            finally
            {
                Object.DestroyImmediate(basic);
                Object.DestroyImmediate(advanced);
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void PrerequisitesMet_IgnoresProjectsOutsideAtlas()
        {
            var atlas = CreateAtlas(out ResearchProject basic, out _);
            var stranger = ScriptableObject.CreateInstance<ResearchProject>();

            try
            {
                var project = ScriptableObject.CreateInstance<ResearchProject>();
                project.requiredPrerequisites = new List<ResearchProject> { basic, stranger };

                // stranger нет в атласе — это требование не должно блокировать.
                Assert.IsTrue(project.ArePrerequisitesMet(new HashSet<int> { 0 }, atlas));

                Assert.IsFalse(project.ArePrerequisitesMet(new HashSet<int>(), atlas));

                Object.DestroyImmediate(project);
            }
            finally
            {
                Object.DestroyImmediate(stranger);
                Object.DestroyImmediate(basic);
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void AdvanceProgress_StopsAtDuration()
        {
            Assert.AreEqual(5f, ResearchRules.AdvanceProgress(0f, 10f, 5f, 1f), 0.0001f);
            Assert.AreEqual(10f, ResearchRules.AdvanceProgress(9f, 10f, 5f, 1f), 0.0001f);
            Assert.AreEqual(0f, ResearchRules.AdvanceProgress(0f, 0f, 5f, 1f), 0.0001f);
            Assert.AreEqual(3f, ResearchRules.AdvanceProgress(3f, 10f, 5f, 0f), 0.0001f, "нулевая скорость не двигает прогресс");
            Assert.AreEqual(3f, ResearchRules.AdvanceProgress(3f, 10f, 0f, 1f), 0.0001f);
        }

        [Test]
        public void ComputeSpeed_AddsResearchAndLabBonuses()
        {
            Assert.AreEqual(1f, ResearchRules.ComputeSpeed(0f, 0, 25f), 0.0001f);
            Assert.AreEqual(1.25f, ResearchRules.ComputeSpeed(25f, 0, 25f), 0.0001f);
            Assert.AreEqual(1.5f, ResearchRules.ComputeSpeed(0f, 2, 25f), 0.0001f);
            Assert.AreEqual(1.75f, ResearchRules.ComputeSpeed(25f, 2, 25f), 0.0001f);
            Assert.GreaterOrEqual(ResearchRules.ComputeSpeed(0f, 0, 0f), 0.1f, "скорость не может обнулиться");
        }

        [Test]
        public void Progress01_ClampsToOne()
        {
            Assert.AreEqual(0.5f, ResearchRules.Progress01(5f, 10f), 0.0001f);
            Assert.AreEqual(1f, ResearchRules.Progress01(20f, 10f), 0.0001f);
            Assert.AreEqual(0f, ResearchRules.Progress01(5f, 0f), 0.0001f);
        }

        [Test]
        public void IsBuildingUnlocked_DefaultAndResearched()
        {
            var atlas = CreateAtlas(out ResearchProject basic, out ResearchProject advanced);

            try
            {
                var completed = new HashSet<int>();

                Assert.IsTrue(ResearchRules.IsBuildingUnlocked(atlas, completed, BuildingTypeNames.House),
                    "дом доступен изначально");
                Assert.IsFalse(ResearchRules.IsBuildingUnlocked(atlas, completed, BuildingTypeNames.Mine));

                completed.Add(0);
                Assert.IsTrue(ResearchRules.IsBuildingUnlocked(atlas, completed, BuildingTypeNames.Mine),
                    "шахта открывается базовым исследованием");
                Assert.IsFalse(ResearchRules.IsBuildingUnlocked(atlas, completed, BuildingTypeNames.Turret));

                completed.Add(1);
                Assert.IsTrue(ResearchRules.IsBuildingUnlocked(atlas, completed, BuildingTypeNames.Turret));
            }
            finally
            {
                Object.DestroyImmediate(basic);
                Object.DestroyImmediate(advanced);
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void IsUnitUnlocked_DefaultAndResearched()
        {
            var atlas = CreateAtlas(out ResearchProject basic, out ResearchProject advanced);

            try
            {
                Assert.IsTrue(ResearchRules.IsUnitUnlocked(atlas, new HashSet<int>(), UnitTypeName.Miner));
                Assert.IsFalse(ResearchRules.IsUnitUnlocked(atlas, new HashSet<int>(), UnitTypeName.Soldier));
                Assert.IsTrue(ResearchRules.IsUnitUnlocked(atlas, new HashSet<int> { 0, 1 }, UnitTypeName.Soldier));
            }
            finally
            {
                Object.DestroyImmediate(basic);
                Object.DestroyImmediate(advanced);
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void IsBuildingUnlocked_WithoutAtlas_DoesNotBlock()
        {
            Assert.IsTrue(ResearchRules.IsBuildingUnlocked(null, new HashSet<int>(), BuildingTypeNames.Turret),
                "без атласа игра не должна блокировать постройки");
            Assert.IsTrue(ResearchRules.IsUnitUnlocked(null, new HashSet<int>(), UnitTypeName.Soldier));
        }

        [Test]
        public void SumBonusPercent_AddsOnlyCompletedProjects()
        {
            var atlas = CreateAtlas(out ResearchProject basic, out ResearchProject advanced);

            try
            {
                float none = ResearchRules.SumBonusPercent(atlas, new HashSet<int>(), p => p.productionBonusPercent);
                float both = ResearchRules.SumBonusPercent(atlas, new HashSet<int> { 0, 1 }, p => p.productionBonusPercent);

                Assert.AreEqual(0f, none, 0.0001f);
                Assert.AreEqual(25f, both, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(basic);
                Object.DestroyImmediate(advanced);
                Object.DestroyImmediate(atlas);
            }
        }

        [Test]
        public void CostOf_NullSafe()
        {
            Assert.IsNull(ResearchRules.CostOf(null));

            var project = ScriptableObject.CreateInstance<ResearchProject>();

            try
            {
                Assert.IsNull(ResearchRules.CostOf(project), "пустая стоимость — бесплатное исследование");
            }
            finally
            {
                Object.DestroyImmediate(project);
            }
        }

        [Test]
        public void ScaleResources_AppliesProductionBonus()
        {
            var outputs = new[]
            {
                new ResourcePair(ResourceType.Minerals, 10),
                new ResourcePair(ResourceType.Ice, 1)
            };

            ResourcePair[] normal = Building.ScaleResources(outputs, 1f);
            ResourcePair[] boosted = Building.ScaleResources(outputs, 1.25f);

            Assert.AreEqual(10, normal[0].Amount);
            Assert.AreEqual(12, boosted[0].Amount, "10 * 1.25 = 12.5 → 12");
            Assert.AreEqual(1, boosted[1].Amount, "ненулевой ресурс не должен обнуляться округлением");
            Assert.AreEqual(ResourceType.Ice, boosted[1].Type);
        }

        [Test]
        public void ScaleResources_NullAndZeroSafe()
        {
            Assert.IsNull(Building.ScaleResources(null, 2f));

            var outputs = new[] { new ResourcePair(ResourceType.Rock, 5) };
            Assert.AreEqual(0, Building.ScaleResources(outputs, 0f)[0].Amount);
        }

        [Test]
        public void ResearchAtlasAsset_ContainsTree()
        {
            var atlas = UnityEditor.AssetDatabase.LoadAssetAtPath<ResearchAtlas>("Assets/Resources/ResearchAtlas.asset");

            if (atlas == null) Assert.Ignore("Ассет ResearchAtlas не найден — нужен полный проект.");

            Assert.GreaterOrEqual(atlas.Count, 5, "в древе должно быть несколько исследований");
            Assert.IsTrue(atlas.IsDefaultBuilding(BuildingTypeNames.House), "дом доступен сразу");
            Assert.IsTrue(atlas.IsDefaultBuilding(BuildingTypeNames.MainBuilding), "главное здание доступно сразу");
            Assert.IsFalse(atlas.IsDefaultBuilding(BuildingTypeNames.Turret), "турель открывается исследованием");
            Assert.IsTrue(atlas.IsDefaultUnit(UnitTypeName.Miner));
            Assert.IsFalse(atlas.IsDefaultUnit(UnitTypeName.Soldier), "солдат открывается исследованием");

            // Каждое исследование должно быть завершаемым: цена или время заданы.
            for (int i = 0; i < atlas.Count; i++)
            {
                ResearchProject project = atlas.Get(i);
                Assert.IsNotNull(project, "пустая запись " + i);
                Assert.Greater(project.Duration, 0f, project.name + ": длительность должна быть > 0");
            }

            // Требования ссылаются только на проекты из этого же атласа.
            for (int i = 0; i < atlas.Count; i++)
            {
                ResearchProject project = atlas.Get(i);
                if (project.requiredPrerequisites == null) continue;

                foreach (ResearchProject prerequisite in project.requiredPrerequisites)
                {
                    if (prerequisite == null) continue;
                    Assert.GreaterOrEqual(atlas.IndexOf(prerequisite), 0,
                        project.name + ": требование отсутствует в атласе");
                }
            }
        }
    }
}
