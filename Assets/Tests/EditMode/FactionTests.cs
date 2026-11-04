using NUnit.Framework;
using UnityEngine;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки системы фракций: кому какая фракция достаётся, где стоит база
    /// и какой участок мира расчищается.
    /// </summary>
    public class FactionTests
    {
        [Test]
        public void SelectFaction_FirstPlayer_GoesToHumans()
        {
            Assert.AreEqual(FactionType.Humans, FactionSpawnMath.SelectFaction(0, 0),
                "первый подключившийся игрок должен попасть к людям");
        }

        [Test]
        public void SelectFaction_SecondPlayer_GoesToMartians()
        {
            Assert.AreEqual(FactionType.Martians, FactionSpawnMath.SelectFaction(1, 0),
                "второй подключившийся игрок должен попасть к марсианам");
        }

        [Test]
        public void SelectFaction_AlternatesAfterBothSidesFilled()
        {
            // 1 человек и 1 марсианин — третий игрок снова к людям.
            Assert.AreEqual(FactionType.Humans, FactionSpawnMath.SelectFaction(1, 1));
            // 2 человека и 1 марсианин — четвёртый к марсианам.
            Assert.AreEqual(FactionType.Martians, FactionSpawnMath.SelectFaction(2, 1));
            Assert.AreEqual(FactionType.Humans, FactionSpawnMath.SelectFaction(2, 2));
        }

        [Test]
        public void SelectFaction_FreesSlotOfDisconnectedPlayer()
        {
            // Марсианин вышел: людей двое, марсиан ноль — следующий игрок идёт к марсианам.
            Assert.AreEqual(FactionType.Martians, FactionSpawnMath.SelectFaction(2, 0));

            // Человек вышел: марсиан двое, людей ноль — следующий игрок идёт к людям.
            Assert.AreEqual(FactionType.Humans, FactionSpawnMath.SelectFaction(0, 2));
        }

        [Test]
        public void SelectFaction_NegativeCounts_AreTreatedAsZero()
        {
            Assert.AreEqual(FactionType.Humans, FactionSpawnMath.SelectFaction(-5, -3));
        }

        [Test]
        public void GetBaseColumn_StaysInsideMap()
        {
            var definition = ScriptableObject.CreateInstance<FactionDefinition>();

            try
            {
                definition.BaseMapFraction = 0.25f;
                definition.ClearHalfWidth = 6;

                int column = FactionSpawnMath.GetBaseColumn(definition, 80);
                Assert.AreEqual(20, column);

                // Крайние значения доли не должны выводить площадку за границу карты.
                definition.BaseMapFraction = 0f;
                int left = FactionSpawnMath.GetBaseColumn(definition, 80);
                Assert.Greater(left, definition.ClearHalfWidth, "площадка не должна упираться в край карты");

                definition.BaseMapFraction = 1f;
                int right = FactionSpawnMath.GetBaseColumn(definition, 80);
                Assert.Less(right, 80 - definition.ClearHalfWidth, "площадка не должна выходить за карту");
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void GetBaseColumn_KeepsFractionsApart()
        {
            var humans = ScriptableObject.CreateInstance<FactionDefinition>();
            var martians = ScriptableObject.CreateInstance<FactionDefinition>();

            try
            {
                humans.BaseMapFraction = 0.25f;
                humans.ClearHalfWidth = 6;
                martians.BaseMapFraction = 0.72f;
                martians.ClearHalfWidth = 6;

                int humansColumn = FactionSpawnMath.GetBaseColumn(humans, 80);
                int martiansColumn = FactionSpawnMath.GetBaseColumn(martians, 80);

                Assert.Less(humansColumn, martiansColumn, "базы фракций не должны стоять на одном месте");
                Assert.Greater(martiansColumn - humansColumn, humans.ClearHalfWidth + martians.ClearHalfWidth,
                    "расчищенные площадки фракций не должны пересекаться");
            }
            finally
            {
                Object.DestroyImmediate(humans);
                Object.DestroyImmediate(martians);
            }
        }

        [Test]
        public void ComputeFloorY_SurfaceFaction_StaysOnSurface()
        {
            Assert.AreEqual(40, FactionSpawnMath.ComputeFloorY(40, true, 18));
        }

        [Test]
        public void ComputeFloorY_UndergroundFaction_GoesBelowSurface()
        {
            Assert.AreEqual(22, FactionSpawnMath.ComputeFloorY(40, false, 18));
        }

        [Test]
        public void ComputeFloorY_NeverGoesBelowWorld()
        {
            // Глубина больше самой поверхности — пол прижимается к нижней границе мира.
            Assert.AreEqual(1, FactionSpawnMath.ComputeFloorY(5, false, 18));

            // Нулевая глубина трактуется как «минимум один блок вниз», а не «на поверхности».
            Assert.AreEqual(4, FactionSpawnMath.ComputeFloorY(5, false, 0));
        }

        [Test]
        public void GetClearRect_CoversWholePlatform()
        {
            RectInt rect = FactionSpawnMath.GetClearRect(20, 30, 6, 4);

            Assert.AreEqual(14, rect.xMin);
            Assert.AreEqual(27, rect.xMax);   // 13 блоков: 20±6
            Assert.AreEqual(30, rect.yMin);
            Assert.AreEqual(34, rect.yMax);   // 4 блока высоты
            Assert.AreEqual(13, rect.width);
            Assert.AreEqual(4, rect.height);
        }

        [Test]
        public void GetClearRect_RejectsZeroSizes()
        {
            RectInt rect = FactionSpawnMath.GetClearRect(10, 10, 0, 0);

            Assert.GreaterOrEqual(rect.width, 1);
            Assert.GreaterOrEqual(rect.height, 1);
        }

        [Test]
        public void ClampSurfaceFloorY_KeepsHeadroomBelowWorldTop()
        {
            // Карта целиком из породы сверху: «поверхность» на верхней границе,
            // но пол наземной базы должен опуститься, чтобы над ней было место.
            int floor = FactionSpawnMath.ClampSurfaceFloorY(80, 80, 5);

            Assert.AreEqual(74, floor);
            Assert.LessOrEqual(floor + 5, 79, "над полом должно остаться место до верха карты");
        }

        [Test]
        public void ClampSurfaceFloorY_KeepsNaturalSurfaceBelowTheTop()
        {
            Assert.AreEqual(40, FactionSpawnMath.ClampSurfaceFloorY(80, 40, 5));
        }

        [Test]
        public void ClampSurfaceFloorY_NeverNegative()
        {
            Assert.GreaterOrEqual(FactionSpawnMath.ClampSurfaceFloorY(4, 2, 5), 1);
            Assert.GreaterOrEqual(FactionSpawnMath.ClampSurfaceFloorY(80, -10, 5), 1);
        }

        [Test]
        public void GetSpawnPosition_StandsOnTopOfFloor()
        {
            Vector2 spawn = FactionSpawnMath.GetSpawnPosition(20, 30);

            Assert.AreEqual(20.5f, spawn.x, 0.0001f, "центр колонки");
            Assert.AreEqual(30.5f, spawn.y, 0.0001f, "полблока над полом — высота коллайдера юнита");
        }

        [Test]
        public void HumansFaction_DefinitionMatchesRules()
        {
            FactionDefinition humans = FactionAtlas.GetDefinition(FactionType.Humans);

            if (humans == null)
            {
                Assert.Ignore("Ассет FactionAtlas отсутствует в Resources — проверка требует проекта.");
            }

            Assert.IsTrue(humans.SpawnsOnSurface, "люди появляются на поверхности");
            Assert.AreEqual(FactionType.Humans, humans.Faction);
            Assert.Greater(humans.ClearHalfWidth, 0, "площадку фракции нужно расчищать");
            Assert.Greater(humans.UnitVisuals.Length, 0, "у фракции должна быть своя графика юнитов");
            Assert.Greater(humans.BuildingVisuals.Length, 0, "у фракции должна быть своя графика зданий");
        }

        [Test]
        public void MartiansFaction_DefinitionMatchesRules()
        {
            FactionDefinition martians = FactionAtlas.GetDefinition(FactionType.Martians);

            if (martians == null)
            {
                Assert.Ignore("Ассет FactionAtlas отсутствует в Resources — проверка требует проекта.");
            }

            Assert.IsFalse(martians.SpawnsOnSurface, "марсиане появляются под землёй");
            Assert.Greater(martians.UndergroundDepth, 0, "глубина пещеры должна быть положительной");
            Assert.AreEqual(FactionType.Martians, martians.Faction);
        }

        [Test]
        public void FactionDefinitions_HaveDifferentGraphics()
        {
            FactionDefinition humans = FactionAtlas.GetDefinition(FactionType.Humans);
            FactionDefinition martians = FactionAtlas.GetDefinition(FactionType.Martians);

            if (humans == null || martians == null)
            {
                Assert.Ignore("Ассет FactionAtlas отсутствует в Resources — проверка требует проекта.");
            }

            Sprite humanSprite = humans.GetBuildingSprite(BuildingTypeNames.House);
            Sprite martianSprite = martians.GetBuildingSprite(BuildingTypeNames.House);

            Assert.IsNotNull(humanSprite);
            Assert.IsNotNull(martianSprite);
            Assert.AreNotSame(humanSprite, martianSprite, "у фракций должны быть разные текстуры одного здания");

            Sprite[] humanFrames = humans.GetUnitFrames(UnitTypeName.Worker);
            Sprite[] martianFrames = martians.GetUnitFrames(UnitTypeName.Worker);

            Assert.IsNotNull(humanFrames);
            Assert.IsNotNull(martianFrames);
            Assert.AreNotSame(humanFrames[0], martianFrames[0], "у фракций должны быть разные спрайты юнитов");
        }

        [Test]
        public void FactionDefinition_UnknownType_ReturnsNull()
        {
            var definition = ScriptableObject.CreateInstance<FactionDefinition>();

            try
            {
                definition.UnitVisuals = null;
                definition.BuildingVisuals = null;

                Assert.IsNull(definition.GetUnitFrames(UnitTypeName.Soldier));
                Assert.IsNull(definition.GetUnitIcon(UnitTypeName.Soldier));
                Assert.IsNull(definition.GetBuildingSprite(BuildingTypeNames.Turret));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
