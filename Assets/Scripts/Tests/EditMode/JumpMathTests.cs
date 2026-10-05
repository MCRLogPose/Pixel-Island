using NUnit.Framework;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    public sealed class JumpMathTests
    {
        [Test]
        public void VelocityForHeight_CorrespondsToConfiguredHeight()
        {
            const float height = 2.75f;
            const float gravity = 34f;

            float velocity = JumpMath.VelocityForHeight(height, gravity);

            Assert.AreEqual(height, JumpMath.PeakHeight(velocity, gravity), 0.0005f);
        }

        [Test]
        public void VelocityForHeight_IsZeroForInvalidInput()
        {
            Assert.AreEqual(0f, JumpMath.VelocityForHeight(0f, 34f));
            Assert.AreEqual(0f, JumpMath.VelocityForHeight(-3f, 34f));
            Assert.AreEqual(0f, JumpMath.VelocityForHeight(2.75f, 0f));
            Assert.AreEqual(0f, JumpMath.VelocityForHeight(2.75f, -34f));
        }

        [Test]
        public void PeakHeight_IsSymmetricWithVelocityForHeight()
        {
            for (float height = 0.5f; height <= 4f; height += 0.5f)
            {
                float velocity = JumpMath.VelocityForHeight(height, 34f);
                Assert.AreEqual(height, JumpMath.PeakHeight(velocity, 34f), 0.0005f,
                    "Fallo la simetria para altura " + height);
            }
        }

        [Test]
        public void TimeToApex_MatchesVelocityOverGravity()
        {
            float velocity = JumpMath.VelocityForHeight(2.75f, 34f);

            Assert.AreEqual(velocity / 34f, JumpMath.TimeToApex(velocity, 34f), 0.0005f);
        }

        [Test]
        public void TimeToApex_IgnoresDownwardVelocity()
        {
            Assert.AreEqual(0f, JumpMath.TimeToApex(-12f, 34f));
        }
    }
}
