using System;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Enemies
{
    /// <summary>
    /// Mecanica principal de un enemigo que patrulla: decide hacia donde camina,
    /// cuando da media vuelta y cuando salta. CLASE PURA (no MonoBehaviour) a
    /// proposito: todo el tuning se testea en EditMode con dt fijo, sin escena.
    ///
    /// La linea de patrulla se define respecto al punto de aparicion (centerX),
    /// no con objetos en la jerarquia. El Rigidbody2D lleva gravityScale = 0 y
    /// freezeRotation; este motor aplica la gravedad y las velocidades.
    /// </summary>
    public sealed class PatrolMotor
    {
        public EnemyPatrolConfig Config { get; }

        /// <summary>Sentido de avance actual: +1 derecha, -1 izquierda.</summary>
        public int Direction { get; private set; }

        /// <summary>Extremo izquierdo de la linea de patrulla.</summary>
        public float MinX { get; }

        /// <summary>Extremo derecho de la linea de patrulla.</summary>
        public float MaxX { get; }

        /// <summary>True solo en el paso fisico en el que arranco un salto.</summary>
        public bool JumpedThisStep { get; private set; }

        public PatrolMotor(EnemyPatrolConfig config, float centerX)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));

            float half = Mathf.Max(0.01f, config.PatrolDistance);
            MinX = centerX - half;
            MaxX = centerX + half;
            Direction = 1;
        }

        /// <summary>Velocidad vertical inicial derivada de la altura de salto configurada.</summary>
        public float JumpVelocity => JumpMath.VelocityForHeight(Config.JumpHeight, Config.Gravity);

        /// <summary>
        /// Avanza la simulacion un paso fisico y devuelve la nueva velocidad.
        /// <paramref name="x"/> es la posicion horizontal actual y
        /// <paramref name="obstacleAhead"/> lo marca el sensor del enemigo.
        /// </summary>
        public Vector2 Step(Vector2 velocity, float x, bool grounded, bool obstacleAhead, float deltaTime)
        {
            JumpedThisStep = false;

            // Media vuelta en los extremos de la linea. Se comprueba antes de
            // mover para no salirse; si ya apunta hacia dentro, no hace nada.
            if (x <= MinX)
            {
                Direction = 1;
            }
            else if (x >= MaxX)
            {
                Direction = -1;
            }

            // El salto se aplica DESPUES de la gravedad para que la velocidad de
            // salida sea exactamente la derivada de la altura configurada.
            ApplyGravity(ref velocity, grounded, deltaTime);

            if (grounded && obstacleAhead)
            {
                velocity.y = JumpVelocity;
                JumpedThisStep = true;
            }

            velocity.x = Direction * Mathf.Max(0f, Config.MoveSpeed);
            return velocity;
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
    }
}
