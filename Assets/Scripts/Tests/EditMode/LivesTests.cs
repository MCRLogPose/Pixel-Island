using NUnit.Framework;
using PixelIsland.Core;

namespace PixelIsland.Tests.EditMode
{
    public sealed class LivesTests
    {
        [Test]
        public void NewLives_StartAtMax()
        {
            var lives = new Lives(3);

            Assert.AreEqual(3, lives.Max);
            Assert.AreEqual(3, lives.Current);
            Assert.IsFalse(lives.IsEmpty);
        }

        [Test]
        public void MaxLives_IsClampedToAtLeastOne()
        {
            var lives = new Lives(0);

            Assert.AreEqual(1, lives.Max);
            Assert.AreEqual(1, lives.Current);
        }

        [Test]
        public void LoseLife_DecrementsAndNotifies()
        {
            var lives = new Lives(3);
            int notifications = 0;
            lives.Changed += _ => notifications++;

            Assert.IsTrue(lives.LoseLife());
            Assert.AreEqual(2, lives.Current);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void LoseLife_StopsAtZero()
        {
            var lives = new Lives(2);

            Assert.IsTrue(lives.LoseLife());
            Assert.IsTrue(lives.LoseLife());
            Assert.IsFalse(lives.LoseLife(), "No se puede bajar de cero.");
            Assert.AreEqual(0, lives.Current);
            Assert.IsTrue(lives.IsEmpty);
        }

        [Test]
        public void Refill_RestoresMaxAndNotifies()
        {
            var lives = new Lives(3);
            lives.LoseLife();
            lives.LoseLife();

            int notifications = 0;
            lives.Changed += _ => notifications++;
            lives.Refill();

            Assert.AreEqual(3, lives.Current);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void Set_ClampsToRange()
        {
            var lives = new Lives(3);

            lives.Set(10);
            Assert.AreEqual(3, lives.Current);

            lives.Set(-5);
            Assert.AreEqual(0, lives.Current);
        }

        [Test]
        public void Set_ToSameValue_DoesNotNotify()
        {
            var lives = new Lives(3);
            int notifications = 0;
            lives.Changed += _ => notifications++;

            lives.Set(3);

            Assert.AreEqual(0, notifications);
        }
    }
}
