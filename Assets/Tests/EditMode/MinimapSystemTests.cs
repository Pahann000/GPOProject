using NUnit.Framework;
using UnityEngine;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки чистой математики миникарты: масштаб текстуры, перевод координат
    /// и цвета блоков (перенос из features/12_minimap).
    /// </summary>
    public class MinimapSystemTests
    {
        [Test]
        public void GetPixelScale_SmallWorld_IsOne()
        {
            Assert.AreEqual(1, MinimapSystem.GetPixelScale(160, 160));
        }

        [Test]
        public void GetPixelScale_BigWorld_ShrinksToLimit()
        {
            int scale = MinimapSystem.GetPixelScale(1600, 1600);

            Assert.AreEqual(4, scale, "1600 блоков при лимите 512 дают 4 блока на пиксель");
            Assert.LessOrEqual(1600 / scale, 512, "текстура не должна превышать лимит");
        }

        [Test]
        public void GetPixelScale_DegenerateSize_IsOne()
        {
            Assert.AreEqual(1, MinimapSystem.GetPixelScale(0, 0));
        }

        [Test]
        public void WorldToPixel_DividesByScale()
        {
            Assert.AreEqual(new Vector2Int(0, 0), MinimapSystem.WorldToPixel(0, 3, 4));
            Assert.AreEqual(new Vector2Int(1, 2), MinimapSystem.WorldToPixel(4, 8, 4));
        }

        [Test]
        public void WorldToPixel_ZeroScale_DoesNotDivideByZero()
        {
            Assert.AreEqual(new Vector2Int(5, 5), MinimapSystem.WorldToPixel(5, 5, 0));
        }

        [Test]
        public void GetColor_KnownAndUnknownTypes()
        {
            Assert.AreEqual(new Color(0.5f, 0.5f, 0.52f, 1f), MinimapSystem.GetColor(BlockType.Stone));
            Assert.AreEqual(Color.magenta, MinimapSystem.GetColor((BlockType)999),
                "неизвестный тип должен быть заметным, а не сливаться с фоном");
        }

        [Test]
        public void GetColor_AirAndStone_Differ()
        {
            Assert.AreNotEqual(MinimapSystem.GetColor(BlockType.Air), MinimapSystem.GetColor(BlockType.Stone));
        }
    }
}
