using NUnit.Framework;
using PixelIsland.Combat;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Tests de la logica pura de disparo: la separacion temporal es determinista
    /// y no depende de Time.time, de modo que se verifica con un reloj simulado.
    /// </summary>
    public sealed class FireControlTests
    {
        const float Cooldown = 2f;

        [Test]
        public void FirstShot_IsAllowedWhenEnabled()
        {
            var fire = new FireControl(Cooldown);

            Assert.IsTrue(fire.CanFire(0f));
            Assert.IsTrue(fire.TryConsume(0f));
        }

        [Test]
        public void SecondShot_IsBlockedUntilCooldownElapses()
        {
            var fire = new FireControl(Cooldown);
            Assert.IsTrue(fire.TryConsume(0f));

            Assert.IsFalse(fire.TryConsume(0.5f), "Antes de la recarga no se puede disparar.");
            Assert.IsFalse(fire.TryConsume(1.99f), "Un pelo antes tampoco.");
            Assert.IsTrue(fire.TryConsume(2f), "Al cumplirse la recarga vuelve a permitirse.");
        }

        [Test]
        public void Disabled_NeverFiresEvenAfterCooldown()
        {
            var fire = new FireControl(Cooldown, enabled: false);

            Assert.IsFalse(fire.CanFire(100f));
            Assert.IsFalse(fire.TryConsume(100f));
        }

        [Test]
        public void RemainingCooldown_CountsDownAndClampsAtZero()
        {
            var fire = new FireControl(Cooldown);
            fire.TryConsume(1f);

            Assert.AreEqual(2f, fire.RemainingCooldown(1f), 0.0001f);
            Assert.AreEqual(1f, fire.RemainingCooldown(2f), 0.0001f);
            Assert.AreEqual(0f, fire.RemainingCooldown(3f), 0.0001f);
        }

        [Test]
        public void UntrackedTaps_DoNotConsumeCooldown()
        {
            var fire = new FireControl(Cooldown);

            // Pulsar sin que la recarga este lista no debe adelantar el proximo tiro.
            Assert.IsFalse(fire.TryConsume(-1f));
            Assert.IsTrue(fire.TryConsume(0f));
            Assert.IsFalse(fire.TryConsume(1f));
            Assert.IsTrue(fire.TryConsume(2f));
        }
    }
}
