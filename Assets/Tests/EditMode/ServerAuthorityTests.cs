using System.Reflection;
using Mirror;
using NUnit.Framework;

namespace GPOProject.Tests
{
    /// <summary>
    /// Инварианты сетевой модели. Их легко случайно ослабить при рефакторинге
    /// (именно так в проекте появились клиент-авторитетные урон и здоровье),
    /// поэтому фиксируем их тестами.
    /// </summary>
    public class ServerAuthorityTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Building_HealthAndState_AreSyncVars()
        {
            Assert.IsNotNull(
                typeof(Building).GetField("_currentHealth", Private)?.GetCustomAttribute<SyncVarAttribute>(),
                "здоровье здания должно синхронизироваться через [SyncVar]");

            Assert.IsNotNull(
                typeof(Building).GetField("_state", Private)?.GetCustomAttribute<SyncVarAttribute>(),
                "состояние здания должно синхронизироваться через [SyncVar]");
        }

        [Test]
        public void Unit_Health_IsSyncVar()
        {
            Assert.IsNotNull(
                typeof(Unit).GetField("_currentHealth", Private)?.GetCustomAttribute<SyncVarAttribute>(),
                "здоровье юнита должно синхронизироваться через [SyncVar]");
        }

        [Test]
        public void Building_DestroyBuilding_IsServerOnly()
        {
            MethodInfo destroy = typeof(Building).GetMethod("DestroyBuilding", Private);

            Assert.IsNotNull(destroy, "DestroyBuilding должен оставаться protected-методом здания");
            Assert.IsFalse(destroy.IsPublic, "уничтожение здания не должно быть публичной точкой входа");
        }

        [Test]
        public void Idamagable_TakesFinalDamageWithoutAttackerType()
        {
            ParameterInfo[] parameters = typeof(IDamagable).GetMethod("TakeDamage").GetParameters();

            Assert.AreEqual(2, parameters.Length,
                "множитель урона считает атакующий, получателю приходит итоговое значение");
            Assert.AreEqual(typeof(int), parameters[0].ParameterType);
            Assert.AreEqual(typeof(Player), parameters[1].ParameterType);
        }

        [Test]
        public void BuilderSystem_PlaceBuilding_IsCommand()
        {
            MethodInfo place = typeof(BuilderSystem).GetMethod("CmdPlaceBuilding", Private);

            Assert.IsNotNull(place, "постройка должна идти через [Command]");
            Assert.IsNotNull(place.GetCustomAttribute<CommandAttribute>(),
                "без [Command] запрос постройки с клиента не дойдёт до сервера");
        }

        [Test]
        public void BuilderSystem_StartPlacement_IsNotServerCommand()
        {
            // Режим стройки — клиентское состояние (превью, ввод), серверная часть — только CmdPlaceBuilding.
            Assert.IsNotNull(typeof(BuilderSystem).GetMethod("StartPlacement"));
            Assert.IsNull(typeof(BuilderSystem).GetMethod("StartPlacement").GetCustomAttribute<CommandAttribute>());
        }

        [Test]
        public void ClientSystems_ImplementGameSystemContract()
        {
            Assert.IsTrue(typeof(IGameSystem).IsAssignableFrom(typeof(UISystem)), "UISystem должен быть системой ядра");
            Assert.IsTrue(typeof(IGameSystem).IsAssignableFrom(typeof(SelectionSystem)), "SelectionSystem должен быть системой ядра");
            Assert.IsTrue(typeof(IGameSystem).IsAssignableFrom(typeof(BuilderSystem)), "BuilderSystem должен быть системой ядра");
            Assert.IsTrue(typeof(IGameSystem).IsAssignableFrom(typeof(ResourceChangedClientNotifier)),
                "ResourceChangedClientNotifier должен регистрироваться в ядре");
        }
    }
}
