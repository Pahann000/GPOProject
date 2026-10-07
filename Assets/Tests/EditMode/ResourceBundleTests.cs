using NUnit.Framework;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки хранилища ресурсов: лимиты, отсутствие падений на пустых данных.
    /// </summary>
    public class ResourceBundleTests
    {
        [Test]
        public void HasResources_NullOrEmptyCost_IsFree()
        {
            var bundle = new ResourceBundle();

            Assert.IsTrue(bundle.HasResources(null), "отсутствие цены — это бесплатно, а не исключение");
            Assert.IsTrue(bundle.HasResources(new ResourcePair[0]));
        }

        [Test]
        public void GetStorageLimit_WithoutEntry_ReturnsDefault()
        {
            var bundle = new ResourceBundle();

            // Раньше здесь возвращался 0, и Mathf.Min(old + income, 0) навсегда обнулял начисление.
            Assert.AreEqual(ResourceBundle.DefaultStorageLimit, bundle.GetStorageLimit(ResourceType.Minerals));
        }

        [Test]
        public void SetStorageLimit_CreatesAndClampsEntry()
        {
            var bundle = new ResourceBundle();

            bundle.SetStorageLimit(ResourceType.Ice, 250);
            Assert.AreEqual(250, bundle.GetStorageLimit(ResourceType.Ice));

            bundle.SetStorageLimit(ResourceType.Ice, -10);
            Assert.AreEqual(0, bundle.GetStorageLimit(ResourceType.Ice), "отрицательный лимит обрезается до нуля");
        }

        [Test]
        public void SetResource_ClampsNegativeAndAddsMissingKey()
        {
            var bundle = new ResourceBundle();

            bundle.SetResource(ResourceType.Root, 42);
            Assert.AreEqual(42, bundle.GetResource(ResourceType.Root));

            bundle.SetResource(ResourceType.Root, -5);
            Assert.AreEqual(0, bundle.GetResource(ResourceType.Root));
        }

        [Test]
        public void AddResources_Null_DoesNotThrow()
        {
            var bundle = new ResourceBundle();

            Assert.DoesNotThrow(() => bundle.AddResources(null));
        }

        [Test]
        public void AddResources_NewType_GetsDefaultLimit()
        {
            var bundle = new ResourceBundle();

            bundle.AddResources(new ResourcePair(ResourceType.Energy, 7));

            Assert.AreEqual(7, bundle.GetResource(ResourceType.Energy));
            Assert.AreEqual(ResourceBundle.DefaultStorageLimit, bundle.GetStorageLimit(ResourceType.Energy));
        }

        [Test]
        public void HasResource_RespectsStoredAmount()
        {
            var bundle = new ResourceBundle();
            bundle.AddResources(new ResourcePair(ResourceType.Rock, 3));

            Assert.IsTrue(bundle.HasResource(new ResourcePair(ResourceType.Rock, 3)));
            Assert.IsFalse(bundle.HasResource(new ResourcePair(ResourceType.Rock, 4)));
            Assert.IsFalse(bundle.HasResource(new ResourcePair(ResourceType.Ice, 1)), "неизвестный тип — ресурса нет");
        }
    }
}
