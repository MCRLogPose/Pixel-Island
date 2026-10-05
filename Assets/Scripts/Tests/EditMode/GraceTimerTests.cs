using NUnit.Framework;
using PixelIsland.Core;

namespace PixelIsland.Tests.EditMode
{
    public sealed class GraceTimerTests
    {
        [Test]
        public void NewTimer_IsNotActive()
        {
            Assert.IsFalse(new GraceTimer(0.1f).IsActive);
        }

        [Test]
        public void Arm_ActivatesWindow()
        {
            GraceTimer timer = new GraceTimer(0.1f);

            timer.Arm();

            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(0.1f, timer.Remaining, 0.0001f);
        }

        [Test]
        public void Tick_CountsDownAndExpires()
        {
            GraceTimer timer = new GraceTimer(0.1f);
            timer.Arm();

            timer.Tick(0.04f);
            Assert.IsTrue(timer.IsActive, "La ventana sigue activa a los 0.04s.");

            timer.Tick(0.04f);
            timer.Tick(0.04f);
            Assert.IsFalse(timer.IsActive, "La ventana expiro tras pasar los 0.1s.");
        }

        [Test]
        public void Tick_DoesNotAccumulateNegativeTime()
        {
            GraceTimer timer = new GraceTimer(0.1f);
            timer.Arm();

            timer.Tick(5f);
            timer.Tick(5f);

            Assert.AreEqual(0f, timer.Remaining);
        }

        [Test]
        public void Consume_SucceedsOnlyOnce()
        {
            GraceTimer timer = new GraceTimer(0.1f);
            timer.Arm();

            Assert.IsTrue(timer.Consume(), "El primer Consume debe tener exito.");
            Assert.IsFalse(timer.Consume(), "El segundo Consume no debe tener exito.");
            Assert.IsFalse(timer.IsActive);
        }

        [Test]
        public void Consume_OnExpiredTimer_ReturnsFalse()
        {
            GraceTimer timer = new GraceTimer(0.1f);
            timer.Arm();
            timer.Tick(0.2f);

            Assert.IsFalse(timer.Consume());
        }

        [Test]
        public void Clear_ClosesWindowImmediately()
        {
            GraceTimer timer = new GraceTimer(0.1f);
            timer.Arm();

            timer.Clear();

            Assert.IsFalse(timer.IsActive);
        }

        [Test]
        public void NegativeDuration_ClampsToZero()
        {
            GraceTimer timer = new GraceTimer(-5f);
            timer.Arm();

            Assert.AreEqual(0f, timer.Duration);
            Assert.IsFalse(timer.IsActive);
        }
    }
}
