using NUnit.Framework;
using UnityEngine;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки главного здания и здания-бонуса
    /// (перенос из features/7_logic_of_fractions).
    /// </summary>
    public class BuildingBonusTests
    {
        [Test]
        public void ComputeIncomePayouts_NothingBeforeFirstInterval()
        {
            Assert.AreEqual(0, MainBuilding.ComputeIncomePayouts(5f, 10f));
        }

        [Test]
        public void ComputeIncomePayouts_OnePayoutPerInterval()
        {
            Assert.AreEqual(1, MainBuilding.ComputeIncomePayouts(10f, 10f));
            Assert.AreEqual(3, MainBuilding.ComputeIncomePayouts(35f, 10f));
        }

        [Test]
        public void ComputeIncomePayouts_LongPause_KeepsIncome()
        {
            // При просадке кадров или паузе доход за пропущенное время не теряется.
            Assert.AreEqual(60, MainBuilding.ComputeIncomePayouts(600f, 10f));
        }

        [Test]
        public void ComputeIncomePayouts_InvalidInput_IsZero()
        {
            Assert.AreEqual(0, MainBuilding.ComputeIncomePayouts(100f, 0f), "нулевой интервал не должен делить на ноль");
            Assert.AreEqual(0, MainBuilding.ComputeIncomePayouts(100f, -5f));
            Assert.AreEqual(0, MainBuilding.ComputeIncomePayouts(0f, 10f));
            Assert.AreEqual(0, MainBuilding.ComputeIncomePayouts(-1f, 10f));
        }

        [Test]
        public void MainBuildingData_FitsWhereBuildingDataExpected()
        {
            var data = ScriptableObject.CreateInstance<MainBuildingData>();

            try
            {
                data.DisplayName = "Главное здание";
                data.MaxHealth = 500;
                data.IncomeInterval = 10f;
                data.InitialStorageCapacity = 200;
                data.IncomeResources = new[] { new ResourcePair(ResourceType.Rock, 5) };

                Assert.IsInstanceOf<BuildingData>(data, "наследник должен подходить в поля типа BuildingData");
                Assert.AreEqual(10f, data.IncomeInterval);
                Assert.AreEqual(200, data.InitialStorageCapacity);
                Assert.AreEqual(1, data.IncomeResources.Length);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ExtendsBuildingData_KeepsBonusSettings()
        {
            var data = ScriptableObject.CreateInstance<ExtendsBuildingData>();

            try
            {
                data.StoredResource = ResourceType.Minerals;
                data.BonusValue = 150;

                Assert.IsInstanceOf<BuildingData>(data);
                Assert.AreEqual(ResourceType.Minerals, data.StoredResource);
                Assert.AreEqual(150, data.BonusValue);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }
    }
}
