using NUnit.Framework;
using PixelIsland.Player;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Verifica que el disparo sale en el sentido correcto y desde el cañon, sin
    /// montar la escena: son funciones estaticas puras de PlayerShooter.
    /// </summary>
    public sealed class PlayerShooterMathTests
    {
        [Test]
        public void Direction_IsRightWhenFacingRight()
        {
            Vector2 direction = PlayerShooter.ComputeDirection(Vector2.right, facingLeft: false);

            Assert.AreEqual(Vector2.right, direction);
        }

        [Test]
        public void Direction_IsMirroredWhenFacingLeft()
        {
            Vector2 direction = PlayerShooter.ComputeDirection(Vector2.right, facingLeft: true);

            Assert.AreEqual(Vector2.left, direction);
        }

        [Test]
        public void Direction_KeepsVerticalAimRegardlessOfFacing()
        {
            Vector2 direction = PlayerShooter.ComputeDirection(Vector2.up, facingLeft: true);

            Assert.AreEqual(Vector2.up, direction);
        }

        [Test]
        public void Direction_FallsBackToRightWhenAimIsZero()
        {
            Vector2 direction = PlayerShooter.ComputeDirection(Vector2.zero, facingLeft: false);

            Assert.AreEqual(Vector2.right, direction);
        }

        [Test]
        public void MuzzleOffset_IsMirroredWhenFacingLeft()
        {
            var muzzle = new Vector2(0.7f, 1f);

            Assert.AreEqual(new Vector2(0.7f, 1f),
                PlayerShooter.ComputeMuzzleOffset(muzzle, facingLeft: false));
            Assert.AreEqual(new Vector2(-0.7f, 1f),
                PlayerShooter.ComputeMuzzleOffset(muzzle, facingLeft: true));
        }
    }
}
