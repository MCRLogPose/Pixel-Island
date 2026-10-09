using NUnit.Framework;
using PixelIsland.Collectibles;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Tests de la logica pura de <see cref="CoinCounter"/> (sin escena).
    /// </summary>
    public sealed class CoinCounterTests
    {
        [Test]
        public void ArrancaSinMonedas()
        {
            var counter = new CoinCounter(4);

            Assert.AreEqual(0, counter.Collected);
            Assert.AreEqual(4, counter.Total);
            Assert.AreEqual(4, counter.Remaining);
            Assert.IsFalse(counter.IsComplete);
        }

        [Test]
        public void TotalNegativoSeRecortaACero()
        {
            var counter = new CoinCounter(-3);

            Assert.AreEqual(0, counter.Total);
            Assert.IsTrue(counter.IsComplete);
        }

        [Test]
        public void RecogerSumaYNotifica()
        {
            var counter = new CoinCounter(4);
            int notificaciones = 0;
            counter.Changed += _ => notificaciones++;

            Assert.IsTrue(counter.Collect());

            Assert.AreEqual(1, counter.Collected);
            Assert.AreEqual(3, counter.Remaining);
            Assert.AreEqual(1, notificaciones);
        }

        [Test]
        public void SeCompletaAlLlegarAlTotal()
        {
            var counter = new CoinCounter(4);

            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(counter.Collect());
            }

            Assert.AreEqual(4, counter.Collected);
            Assert.AreEqual(0, counter.Remaining);
            Assert.IsTrue(counter.IsComplete);
        }

        [Test]
        public void NoSumaMasAllaDelTotal()
        {
            var counter = new CoinCounter(2);
            counter.Collect();
            counter.Collect();
            int notificaciones = 0;
            counter.Changed += _ => notificaciones++;

            Assert.IsFalse(counter.Collect());
            Assert.AreEqual(2, counter.Collected);
            Assert.AreEqual(0, notificaciones);
        }
    }
}
