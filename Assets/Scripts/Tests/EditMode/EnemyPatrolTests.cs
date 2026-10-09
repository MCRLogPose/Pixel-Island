using NUnit.Framework;
using PixelIsland.Enemies;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Prueba la logica pura de la patrulla: velocidad, media vuelta en los
    /// extremos de la linea y salto ante obstaculo. Sin escena ni fisica.
    /// </summary>
    public sealed class EnemyPatrolTests
    {
        static EnemyPatrolConfig Config()
        {
            return new EnemyPatrolConfig
            {
                MoveSpeed = 2f,
                PatrolDistance = 3f,
                JumpHeight = 1.6f,
                Gravity = 30f,
                MaxFallSpeed = 20f
            };
        }

        [Test]
        public void Constructor_CentraLaLineaEnLaPosicion()
        {
            var motor = new PatrolMotor(Config(), 10f);

            Assert.AreEqual(7f, motor.MinX, 1e-4f);
            Assert.AreEqual(13f, motor.MaxX, 1e-4f);
            Assert.AreEqual(1, motor.Direction);
        }

        [Test]
        public void EnElSuelo_CaminaALaVelocidadConfigurada()
        {
            var motor = new PatrolMotor(Config(), 10f);

            Vector2 velocity = motor.Step(Vector2.zero, 10f, true, false, 0.02f);

            Assert.AreEqual(2f, velocity.x, 1e-4f);
            Assert.AreEqual(0f, velocity.y, 1e-4f);
        }

        [Test]
        public void AlLlegarAlExtremoDerecho_DaMediaVuelta()
        {
            var motor = new PatrolMotor(Config(), 10f);

            motor.Step(Vector2.zero, 13f, true, false, 0.02f);

            Assert.AreEqual(-1, motor.Direction);
        }

        [Test]
        public void AlLlegarAlExtremoIzquierdo_DaMediaVuelta()
        {
            var motor = new PatrolMotor(Config(), 10f);
            motor.Step(Vector2.zero, 13f, true, false, 0.02f);

            motor.Step(Vector2.zero, 7f, true, false, 0.02f);

            Assert.AreEqual(1, motor.Direction);
        }

        [Test]
        public void ConObstaculoDelanteYSuelo_SaltaConLaVelocidadDeLaAltura()
        {
            var config = Config();
            var motor = new PatrolMotor(config, 10f);
            float expected = Mathf.Sqrt(2f * config.JumpHeight * config.Gravity);

            Vector2 velocity = motor.Step(Vector2.zero, 10f, true, true, 0.02f);

            Assert.IsTrue(motor.JumpedThisStep);
            Assert.AreEqual(expected, velocity.y, 1e-3f);
        }

        [Test]
        public void ConObstaculoPeroSinSuelo_NoSalta()
        {
            var motor = new PatrolMotor(Config(), 10f);

            Vector2 velocity = motor.Step(new Vector2(0f, -5f), 10f, false, true, 0.02f);

            Assert.IsFalse(motor.JumpedThisStep);
            Assert.Less(velocity.y, -5f, "En el aire debe seguir cayendo, no saltar.");
        }

        [Test]
        public void EnElAire_AplicaGravedad()
        {
            var motor = new PatrolMotor(Config(), 10f);

            Vector2 velocity = motor.Step(Vector2.zero, 10f, false, false, 0.5f);

            Assert.AreEqual(-15f, velocity.y, 1e-3f);
        }

        [Test]
        public void EnElAire_LaCaidaSeLimitaAMaxFallSpeed()
        {
            var config = Config();
            var motor = new PatrolMotor(config, 10f);

            Vector2 velocity = motor.Step(new Vector2(0f, -19f), 10f, false, false, 1f);

            Assert.AreEqual(-config.MaxFallSpeed, velocity.y, 1e-3f);
        }

        [Test]
        public void EnElSuelo_AnulaLaVelocidadVerticalNegativa()
        {
            var motor = new PatrolMotor(Config(), 10f);

            Vector2 velocity = motor.Step(new Vector2(0f, -8f), 10f, true, false, 0.02f);

            Assert.AreEqual(0f, velocity.y, 1e-4f);
        }
    }
}
