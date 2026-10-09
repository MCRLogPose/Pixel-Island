using System;
using NUnit.Framework;
using PixelIsland.Player;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Tests de la mecanica principal. El PlayerMotor es logica pura, asi que
    /// estos tests verifican el tuning real del salto y del desplazamiento sin
    /// escena, sin fisica de Unity y sin play mode.
    /// </summary>
    public sealed class PlayerMotorTests
    {
        const float Dt = 1f / 120f;

        /// <summary>Config de referencia: la que lleva el PlayerController por defecto.</summary>
        static MovementConfig Config(float jumpHeight = 2.75f)
        {
            return new MovementConfig
            {
                MoveSpeed = 6f,
                RunSpeedMultiplier = 1.45f,
                GroundAcceleration = 70f,
                GroundDeceleration = 85f,
                AirAcceleration = 38f,
                JumpHeight = jumpHeight,
                Gravity = 34f,
                MaxFallSpeed = 24f,
                CoyoteSeconds = 0.10f,
                JumpBufferSeconds = 0.12f,
                JumpCutMultiplier = 0.45f
            };
        }

        struct SimulationResult
        {
            public float PeakHeight;
            public int JumpFrames;
            public Vector2 FinalVelocity;
        }

        /// <summary>
        /// Simula pasos fisicos con el motor puro. Cada test decide cuando hay
        /// suelo y que entrada llega en cada paso.
        /// </summary>
        static SimulationResult Simulate(PlayerMotor motor, Func<int, bool> groundedAt,
            Func<int, MoveInput> inputAt, int steps = 1200)
        {
            Vector2 velocity = Vector2.zero;
            Vector2 position = Vector2.zero;
            SimulationResult result = default;

            for (int step = 0; step < steps; step++)
            {
                velocity = motor.Step(velocity, inputAt(step), groundedAt(step), Dt);
                position += velocity * Dt;

                result.PeakHeight = Mathf.Max(result.PeakHeight, position.y);
                if (motor.JumpedThisStep)
                {
                    result.JumpFrames++;
                }
            }

            result.FinalVelocity = velocity;
            return result;
        }

        /// <summary>Suelo solo en el frame del salto: el cuerpo se va al aire justo despues.</summary>
        static bool GroundedOnlyAtLaunch(int step)
        {
            return step == 0;
        }

        // ---------------------------------------------------------------
        // Horizontal
        // ---------------------------------------------------------------

        [Test]
        public void Step_OnGround_WithNoInput_KeepsVelocityAtZero()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;

            for (int i = 0; i < 240; i++)
            {
                velocity = motor.Step(velocity, MoveInput.None, true, Dt);
            }

            Assert.AreEqual(0f, velocity.x, 0.0001f);
            Assert.AreEqual(0f, velocity.y, 0.0001f, "En suelo no debe acumular gravedad.");
        }

        [Test]
        public void Step_AcceleratesTowardsWalkSpeed()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;

            MoveInput input = new MoveInput(1f, false, false, false);
            for (int i = 0; i < 240; i++)
            {
                velocity = motor.Step(velocity, input, true, Dt);
            }

            Assert.AreEqual(config.MoveSpeed, velocity.x, 0.001f,
                "Despues de un tiempo debe alcanzar la velocidad de caminar.");
        }

        [Test]
        public void Step_WalksBackwardsAtSameSpeed()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;

            MoveInput input = new MoveInput(-1f, false, false, false);
            for (int i = 0; i < 240; i++)
            {
                velocity = motor.Step(velocity, input, true, Dt);
            }

            Assert.AreEqual(-config.MoveSpeed, velocity.x, 0.001f);
        }

        [Test]
        public void Step_UsesGroundDecelerationWhenInputStops()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);

            Vector2 velocity = motor.Step(
                new Vector2(config.MoveSpeed, 0f), MoveInput.None, true, Dt);

            Assert.AreEqual(config.MoveSpeed - config.GroundDeceleration * Dt, velocity.x, 0.0001f,
                "Sin input en suelo debe frenar con GroundDeceleration.");
        }

        [Test]
        public void Step_DeceleratesToStopWhenInputReleased()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = new Vector2(config.MoveSpeed, 0f);

            for (int i = 0; i < 240; i++)
            {
                velocity = motor.Step(velocity, MoveInput.None, true, Dt);
            }

            Assert.AreEqual(0f, velocity.x, 0.0001f);
        }

        [Test]
        public void Step_RunningIsFasterThanWalking()
        {
            MovementConfig config = Config();

            Assert.Greater(config.TargetSpeed(1f, true), config.TargetSpeed(1f, false));
        }

        [Test]
        public void Step_TargetSpeedIsZeroWithoutHorizontalInput()
        {
            MovementConfig config = Config();

            Assert.AreEqual(0f, config.TargetSpeed(0f, true), 0.0001f);
        }

        [Test]
        public void Step_AirAccelerationIsWeakerThanGroundAcceleration()
        {
            MovementConfig config = Config();
            MoveInput input = new MoveInput(1f, false, false, false);

            Vector2 grounded = new PlayerMotor(config).Step(Vector2.zero, input, true, Dt);
            Vector2 airborne = new PlayerMotor(config).Step(Vector2.zero, input, false, Dt);

            Assert.AreEqual(config.GroundAcceleration * Dt, grounded.x, 0.0001f);
            Assert.AreEqual(config.AirAcceleration * Dt, airborne.x, 0.0001f);
            Assert.Less(airborne.x, grounded.x, "En el aire se tiene menos control horizontal.");
        }

        // ---------------------------------------------------------------
        // Salto: altura
        // ---------------------------------------------------------------

        [Test]
        public void Jump_ReachesConfiguredHeight()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);

            SimulationResult result = Simulate(motor, GroundedOnlyAtLaunch,
                step => new MoveInput(0f, step == 0, true, false));

            // Euler semi-implicito sobreestima el apice unos pocos por ciento a
            // 120 Hz, por eso la tolerancia no es exacta.
            Assert.AreEqual(1, result.JumpFrames);
            Assert.AreEqual(config.JumpHeight, result.PeakHeight, config.JumpHeight * 0.05f,
                "El salto a tope debe alcanzar la altura configurada.");
        }

        [Test]
        public void Jump_HigherSettingProducesHigherJump()
        {
            PlayerMotor lowMotor = new PlayerMotor(Config(1.5f));
            PlayerMotor highMotor = new PlayerMotor(Config(3.5f));

            SimulationResult low = Simulate(lowMotor, GroundedOnlyAtLaunch,
                step => new MoveInput(0f, step == 0, true, false));
            SimulationResult high = Simulate(highMotor, GroundedOnlyAtLaunch,
                step => new MoveInput(0f, step == 0, true, false));

            Assert.Greater(high.PeakHeight, low.PeakHeight + 1.5f);
        }

        [Test]
        public void Jump_LandsBackAtGroundLevel()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;
            Vector2 position = Vector2.zero;
            float peak = 0f;

            for (int step = 0; step < 1200; step++)
            {
                velocity = motor.Step(velocity, new MoveInput(0f, step == 0, true, false),
                    step == 0, Dt);
                position += velocity * Dt;
                peak = Mathf.Max(peak, position.y);

                // Tras el apice, el primer paso en el que vuelve a tocar la
                // altura de lanzamiento debe hacerlo muy cerca de ella. La
                // tolerancia de 0.1 unidades (~3 px a 32 PPU) absorbe el sesgo
                // del Euler semi-implicito sin dejar pasar un error real.
                if (step > 60 && position.y <= 0f)
                {
                    Assert.Less(Mathf.Abs(position.y), 0.1f,
                        "El jugador debe volver a la altura de partida tras el salto.");
                    Assert.Greater(peak, config.JumpHeight * 0.9f,
                        "Antes de caer debe haber alcanzado la altura configurada.");
                    Assert.Less(velocity.y, 0f, "Debe estar en descenso al volver al suelo.");
                    return;
                }
            }

            Assert.Fail("El jugador nunca volvio al suelo tras el salto.");
        }

        [Test]
        public void Jump_ReleasedEarly_ReachesLowerHeight()
        {
            MovementConfig config = Config();

            SimulationResult full = Simulate(new PlayerMotor(config), GroundedOnlyAtLaunch,
                step => new MoveInput(0f, step == 0, true, false));

            // El boton ya no esta pulsado ni en el frame del salto: toque breve.
            SimulationResult tap = Simulate(new PlayerMotor(config), GroundedOnlyAtLaunch,
                step => new MoveInput(0f, step == 0, false, false));

            Assert.Greater(tap.PeakHeight, 0f, "Un toque breve debe producir algo de altura.");
            Assert.Less(tap.PeakHeight, full.PeakHeight * 0.5f,
                "El salto corto debe ser claramente mas bajo que el salto a tope.");
        }

        [Test]
        public void Jump_ReportsJumpedThisStepOnlyOnTheJumpFrame()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;
            int jumpFrames = 0;

            for (int i = 0; i < 400; i++)
            {
                velocity = motor.Step(velocity, new MoveInput(0f, i == 1, true, false),
                    i < 3, Dt);

                if (motor.JumpedThisStep)
                {
                    jumpFrames++;
                }
            }

            Assert.AreEqual(1, jumpFrames, "Solo el frame del disparo debe reportar el salto.");
        }

        [Test]
        public void Jump_WhileGrounded_DoesNotFireTwiceInARow()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;
            int jumpFrames = 0;

            // El cuerpo sigue pegado al suelo 4 pasos y solo se pulsa en el primero.
            for (int i = 0; i < 4; i++)
            {
                velocity = motor.Step(velocity, new MoveInput(0f, i == 0, true, false), true, Dt);

                if (motor.JumpedThisStep)
                {
                    jumpFrames++;
                }
            }

            Assert.AreEqual(1, jumpFrames, "Un solo pulseo debe producir un solo salto.");
            Assert.Greater(velocity.y, 0f, "Tras el frame del salto el jugador esta en ascenso.");
        }

        // ---------------------------------------------------------------
        // Salto: coyote time y jump buffer
        // ---------------------------------------------------------------

        [Test]
        public void CoyoteTime_AllowsJumpShortlyAfterLeavingGround()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;
            int jumpFrame = -1;

            // El paso 4 son 0.033 s, dentro de la ventana de 0.10 s.
            for (int step = 0; step < 10; step++)
            {
                velocity = motor.Step(velocity, new MoveInput(0f, step == 4, false, false),
                    step == 0, Dt);

                if (motor.JumpedThisStep)
                {
                    jumpFrame = step;
                }
            }

            Assert.AreEqual(4, jumpFrame, "El coyote time debe permitir saltar ya en el aire.");
        }

        [Test]
        public void CoyoteTime_ExpiresAfterConfiguredWindow()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;
            int jumpFrames = 0;

            // Pulso de salto 0.15 s despues de perder el suelo: fuera de ventana.
            int lateStep = Mathf.CeilToInt(0.15f / Dt);
            for (int step = 0; step <= lateStep + 5; step++)
            {
                velocity = motor.Step(velocity, new MoveInput(0f, step == lateStep, false, false),
                    step == 0, Dt);

                if (motor.JumpedThisStep)
                {
                    jumpFrames++;
                }
            }

            Assert.AreEqual(0, jumpFrames, "Fuera del coyote time no debe haber salto.");
        }

        [Test]
        public void JumpBuffer_FiresOnLandingWhenPressedEarly()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;
            int jumpFrame = -1;

            int pressStep = 40;
            int landingStep = 50; // 10 pasos = 0.083 s despues, dentro de los 0.12 s.

            for (int step = 0; step <= 80; step++)
            {
                velocity = motor.Step(velocity, new MoveInput(0f, step == pressStep, true, false),
                    step == landingStep, Dt);

                if (motor.JumpedThisStep)
                {
                    jumpFrame = step;
                }
            }

            Assert.AreEqual(landingStep, jumpFrame,
                "Un salto pulsado antes de aterrizar debe salir al tocar suelo.");
        }

        [Test]
        public void JumpBuffer_ExpiresWhenLandingIsTooLate()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;
            int jumpFrames = 0;

            int pressStep = 40;
            int landingStep = 60; // 20 pasos = 0.167 s despues, fuera de los 0.12 s.

            for (int step = 0; step <= 90; step++)
            {
                velocity = motor.Step(velocity, new MoveInput(0f, step == pressStep, true, false),
                    step == landingStep, Dt);

                if (motor.JumpedThisStep)
                {
                    jumpFrames++;
                }
            }

            Assert.AreEqual(0, jumpFrames, "El jump buffer debe expirar si no se aterriza a tiempo.");
        }

        // ---------------------------------------------------------------
        // Caida
        // ---------------------------------------------------------------

        [Test]
        public void Fall_AcceleratesDownwardsWhileNotGrounded()
        {
            MovementConfig config = Config();

            Vector2 velocity = new PlayerMotor(config).Step(Vector2.zero, MoveInput.None, false, Dt);

            Assert.AreEqual(-config.Gravity * Dt, velocity.y, 0.0001f);
        }

        [Test]
        public void Fall_ClampsToMaxFallSpeed()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = Vector2.zero;

            for (int i = 0; i < 1200; i++)
            {
                velocity = motor.Step(velocity, MoveInput.None, false, Dt);
            }

            Assert.AreEqual(-config.MaxFallSpeed, velocity.y, 0.0001f,
                "La caida no debe superar la velocidad maxima.");
        }

        [Test]
        public void Reset_ClearsPendingJumpState()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            Vector2 velocity = motor.Step(Vector2.zero, new MoveInput(0f, true, true, false), true, Dt);
            Assert.IsTrue(motor.JumpedThisStep);

            motor.Reset();

            Assert.IsFalse(motor.JumpedThisStep);
            Assert.IsFalse(motor.Coyote.IsActive);
            Assert.IsFalse(motor.JumpBuffer.IsActive);
            Assert.IsFalse(motor.IsRising);
        }

        // ---------------------------------------------------------------
        // Rebote de dolor (impulso externo)
        // ---------------------------------------------------------------

        [Test]
        public void ApplyImpulse_OnFirstStep_OverridesHorizontalControl()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);

            motor.ApplyImpulse(new Vector2(-4f, 10f), 0.18f);

            Vector2 velocity = motor.Step(Vector2.zero, MoveInput.None, true, Dt);

            Assert.AreEqual(-4f, velocity.x, 0.001f,
                "El impulso debe mandar sobre la entrada horizontal.");
            Assert.Greater(velocity.y, 0f, "El brinco debe ir hacia arriba.");
        }

        [Test]
        public void ApplyImpulse_ExpiresAndReturnsControl()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            float lockSeconds = 0.05f;

            motor.ApplyImpulse(new Vector2(-4f, 10f), lockSeconds);

            int steps = Mathf.CeilToInt(lockSeconds / Dt) + 2;
            Vector2 velocity = Vector2.zero;
            for (int i = 0; i < steps; i++)
            {
                velocity = motor.Step(velocity, MoveInput.None, false, Dt);
            }

            Assert.IsFalse(motor.IsImpulsing, "El bloqueo de control debe expirar.");
        }

        [Test]
        public void ApplyImpulse_StillClampsFallSpeed()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            motor.ApplyImpulse(new Vector2(0f, 1f), 0.1f);

            Vector2 velocity = Vector2.zero;
            for (int i = 0; i < 2000; i++)
            {
                velocity = motor.Step(velocity, MoveInput.None, false, Dt);
            }

            Assert.AreEqual(-config.MaxFallSpeed, velocity.y, 0.0001f);
        }

        [Test]
        public void Reset_ClearsPendingImpulse()
        {
            MovementConfig config = Config();
            PlayerMotor motor = new PlayerMotor(config);
            motor.ApplyImpulse(new Vector2(4f, 10f), 1f);

            motor.Reset();

            Assert.IsFalse(motor.IsImpulsing);
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerMotor(null));
        }
    }
}
