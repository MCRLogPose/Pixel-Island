using System;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Mecanica principal: calculo de la velocidad del Explorador.
    /// CLASE PURA (no MonoBehaviour) a proposito: todo el tuning del salto y del
    /// desplazamiento se testea en EditMode con dt fijo, sin escena ni fisica.
    ///
    /// El Rigidbody2D lleva gravityScale = 0 y freezeRotation; este motor es
    /// quien aplica la gravedad y las velocidades, de modo que el resultado
    /// es determinista y el altura de salto coincide con el tuning.
    /// </summary>
    public sealed class PlayerMotor
    {
        public MovementConfig Config { get; }

        /// <summary>Ventana para saltar poco despues de perder el contacto con el suelo.</summary>
        public GraceTimer Coyote { get; }

        /// <summary>Ventana que recuerda un pulseo de salto hecho antes de aterrizar.</summary>
        public GraceTimer JumpBuffer { get; }

        /// <summary>True solo en el paso fisico en el que se disparo un salto.</summary>
        public bool JumpedThisStep { get; private set; }

        /// <summary>True mientras el salto sigue en ascension y se puede cortar.</summary>
        public bool IsRising { get; private set; }

        /// <summary>True mientras un rebote externo (dolor) manda sobre la entrada.</summary>
        public bool IsImpulsing => _impulseTimer > 0f;

        Vector2 _impulseVelocity;
        float _impulseTimer;

        public PlayerMotor(MovementConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Coyote = new GraceTimer(config.CoyoteSeconds);
            JumpBuffer = new GraceTimer(config.JumpBufferSeconds);
        }

        /// <summary>Velocidad vertical inicial derivada de la altura de salto configurada.</summary>
        public float JumpVelocity => JumpMath.VelocityForHeight(Config.JumpHeight, Config.Gravity);

        /// <summary>
        /// Avanza la simulacion un paso fisico y devuelve la nueva velocidad.
        /// </summary>
        public Vector2 Step(Vector2 velocity, in MoveInput input, bool grounded, float deltaTime)
        {
            JumpedThisStep = false;

            Coyote.Tick(deltaTime);
            JumpBuffer.Tick(deltaTime);

            if (grounded)
            {
                Coyote.Arm();
            }

            // Un rebote externo (p.ej. el dolor de los pinchos) manda sobre la
            // entrada mientras dura: se aplica la gravedad al propio impulso.
            if (_impulseTimer > 0f)
            {
                _impulseTimer -= deltaTime;
                _impulseVelocity.y = Mathf.Max(
                    -Config.MaxFallSpeed, _impulseVelocity.y - Config.Gravity * deltaTime);
                return _impulseVelocity;
            }

            ApplyGravity(ref velocity, grounded, deltaTime);
            ApplyJump(ref velocity, input);
            ApplyHorizontal(ref velocity, input, grounded, deltaTime);

            return velocity;
        }

        /// <summary>
        /// Impone una velocidad externa (rebote de dolor) que manda sobre la entrada
        /// durante <paramref name="lockSeconds"/>; despues la gravedad y el control
        /// normales retoman desde esa velocidad. Logica pura: se testea en EditMode.
        /// </summary>
        public void ApplyImpulse(Vector2 velocity, float lockSeconds)
        {
            _impulseVelocity = velocity;
            _impulseTimer = Mathf.Max(0f, lockSeconds);
            IsRising = false;
            JumpedThisStep = false;
        }

        public void Reset()
        {
            Coyote.Clear();
            JumpBuffer.Clear();
            JumpedThisStep = false;
            IsRising = false;
            _impulseVelocity = Vector2.zero;
            _impulseTimer = 0f;
        }

        void ApplyGravity(ref Vector2 velocity, bool grounded, float deltaTime)
        {
            if (grounded && velocity.y <= 0f)
            {
                velocity.y = 0f;
                return;
            }

            velocity.y = Mathf.Max(-Config.MaxFallSpeed, velocity.y - Config.Gravity * deltaTime);
        }

        void ApplyJump(ref Vector2 velocity, in MoveInput input)
        {
            if (input.JumpPressed)
            {
                JumpBuffer.Arm();
            }

            // El orden importa: si aun no hay coyote time disponible NO se consume el
            // buffer, para que el pulseo siga esperando al aterrizaje.
            if (Coyote.IsActive && JumpBuffer.Consume())
            {
                velocity.y = JumpVelocity;
                Coyote.Clear();
                IsRising = true;
                JumpedThisStep = true;
            }

            if (IsRising && velocity.y <= 0f)
            {
                IsRising = false;
                return;
            }

            // Salto variable: soltar el boton en ascenso recorta el impulso. Se
            // evalua tambien en el frame del salto, para que un toque muy breve
            // de boton produzca un salto corto y no un salto a tope.
            if (IsRising && !input.JumpHeld)
            {
                velocity.y *= Config.JumpCutMultiplier;
                IsRising = false;
            }
        }

        void ApplyHorizontal(ref Vector2 velocity, in MoveInput input, bool grounded, float deltaTime)
        {
            float target = Config.TargetSpeed(input.MoveX, input.RunHeld);

            float rate;
            if (!grounded)
            {
                rate = Config.AirAcceleration;
            }
            else
            {
                rate = Mathf.Approximately(target, 0f)
                    ? Config.GroundDeceleration
                    : Config.GroundAcceleration;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, target, rate * deltaTime);
        }
    }
}
