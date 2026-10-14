using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки выбора исполнителя для задачи и самой модели задачи
    /// (перенос из features/9_unit_work_spread_system).
    /// </summary>
    public class UnitTaskDistributionTests
    {
        private static UnitSystem.UnitCandidate Candidate(UnitTypeName type, UnitWork work, float x, float y)
        {
            return new UnitSystem.UnitCandidate(type, work, new Vector2(x, y));
        }

        /// <summary>Заглушка цели: настоящие Block и Unit требуют сцены.</summary>
        private class FakeDamagable : IDamagable
        {
            public int Health = 10;

            public void TakeDamage(int amount, Player damager) => Health -= amount;

            public int CurrentHealth => Health;
            public int X => 3;
            public int Y => 7;
        }

        [Test]
        public void SelectNearestFreeUnit_PicksClosest()
        {
            var candidates = new List<UnitSystem.UnitCandidate>
            {
                Candidate(UnitTypeName.Miner, UnitWork.Idle, 10f, 0f),
                Candidate(UnitTypeName.Miner, UnitWork.Idle, 2f, 0f),
                Candidate(UnitTypeName.Miner, UnitWork.Idle, 5f, 0f),
            };

            Assert.AreEqual(1, UnitSystem.SelectNearestFreeUnit(candidates, UnitTypeName.Miner, Vector2.zero));
        }

        [Test]
        public void SelectNearestFreeUnit_SkipsBusyUnits()
        {
            var candidates = new List<UnitSystem.UnitCandidate>
            {
                Candidate(UnitTypeName.Miner, UnitWork.Moving, 1f, 0f),
                Candidate(UnitTypeName.Miner, UnitWork.Attacking, 2f, 0f),
                Candidate(UnitTypeName.Miner, UnitWork.Idle, 9f, 0f),
            };

            Assert.AreEqual(2, UnitSystem.SelectNearestFreeUnit(candidates, UnitTypeName.Miner, Vector2.zero),
                "занятые юниты не должны получать новую задачу");
        }

        [Test]
        public void SelectNearestFreeUnit_SkipsWrongType()
        {
            var candidates = new List<UnitSystem.UnitCandidate>
            {
                Candidate(UnitTypeName.Soldier, UnitWork.Idle, 1f, 0f),
                Candidate(UnitTypeName.Miner, UnitWork.Idle, 8f, 0f),
            };

            Assert.AreEqual(1, UnitSystem.SelectNearestFreeUnit(candidates, UnitTypeName.Miner, Vector2.zero),
                "блок должен добывать шахтёр, а не солдат");
        }

        [Test]
        public void SelectNearestFreeUnit_NoSuitableUnit_ReturnsMinusOne()
        {
            var candidates = new List<UnitSystem.UnitCandidate>
            {
                Candidate(UnitTypeName.Soldier, UnitWork.Idle, 1f, 0f),
                Candidate(UnitTypeName.Miner, UnitWork.Dead, 2f, 0f),
            };

            Assert.AreEqual(-1, UnitSystem.SelectNearestFreeUnit(candidates, UnitTypeName.Miner, Vector2.zero));
        }

        [Test]
        public void SelectNearestFreeUnit_EmptyAndNullListsAreSafe()
        {
            Assert.AreEqual(-1, UnitSystem.SelectNearestFreeUnit(new List<UnitSystem.UnitCandidate>(),
                UnitTypeName.Miner, Vector2.zero));
            Assert.AreEqual(-1, UnitSystem.SelectNearestFreeUnit(null, UnitTypeName.Miner, Vector2.zero));
        }

        [Test]
        public void SelectNearestFreeUnit_ComparesStraightLineDistance()
        {
            // Первый ближе по одной оси, но дальше по прямой.
            var candidates = new List<UnitSystem.UnitCandidate>
            {
                Candidate(UnitTypeName.Miner, UnitWork.Idle, 3f, 4f),   // расстояние 5
                Candidate(UnitTypeName.Miner, UnitWork.Idle, 4f, 0f),   // расстояние 4
            };

            Assert.AreEqual(1, UnitSystem.SelectNearestFreeUnit(candidates, UnitTypeName.Miner, Vector2.zero));
        }

        [Test]
        public void Task_ForNonComponentTarget_UsesCellCoordinates()
        {
            var target = new FakeDamagable();

            var task = new Task(null, target, UnitTypeName.Miner);

            Assert.AreEqual(new Vector2(3f, 7f), task.TargetPosition, "у блока позиция берётся из координат клетки");
            Assert.AreEqual(UnitTypeName.Miner, task.RequiredUnitType);
            Assert.IsTrue(task.IsTargetValid());
        }

        [Test]
        public void Task_DeadTarget_IsNotValid()
        {
            var target = new FakeDamagable { Health = 0 };

            var task = new Task(null, target, UnitTypeName.Soldier);

            Assert.IsFalse(task.IsTargetValid(), "цель без здоровья больше не задача");
        }
    }
}
