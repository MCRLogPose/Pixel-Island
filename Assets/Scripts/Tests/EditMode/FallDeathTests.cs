using NUnit.Framework;
using PixelIsland.Core;

namespace PixelIsland.Tests.EditMode
{
    public sealed class FallDeathTests
    {
        [Test]
        public void AbovePlane_DoesNotTrigger()
        {
            var death = new FallDeath(-16f);

            Assert.IsFalse(death.Check(-5f));
            Assert.IsFalse(death.Triggered);
        }

        [Test]
        public void AtOrBelowPlane_Triggers()
        {
            var death = new FallDeath(-16f);

            Assert.IsTrue(death.Check(-16f), "Justo en el plano tambien cuenta como caida.");
            Assert.IsTrue(death.Triggered);
        }

        [Test]
        public void AfterTrigger_DoesNotTriggerAgainUntilRearm()
        {
            var death = new FallDeath(-16f);
            death.Check(-20f);

            Assert.IsFalse(death.Check(-30f), "No debe volver a disparar hasta el Rearm.");

            death.Rearm();
            Assert.IsTrue(death.Check(-20f), "Tras Rearm vuelve a vigilar.");
        }

        [Test]
        public void Rearm_ClearsTriggeredFlag()
        {
            var death = new FallDeath(-16f);
            death.Check(-16f);

            death.Rearm();

            Assert.IsFalse(death.Triggered);
        }
    }
}
