using System;
using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Datos de tuning del movimiento del Explorador. Objeto serializable para
    /// poder editarlo en el Inspector del PlayerController.
    /// Es SOLO datos: la logica vive en <see cref="PlayerMotor"/>.
    /// </summary>
    [Serializable]
    public sealed class MovementConfig
    {
        [Header("Horizontal")]
        [Tooltip("Velocidad maxima caminando, en unidades por segundo (1 unidad = 32 px).")]
        public float MoveSpeed = 6f;

        [Tooltip("Multiplicador de velocidad mientras se corre (Shift).")]
        public float RunSpeedMultiplier = 1.45f;

        [Tooltip("Rapidez con la que se alcanza la velocidad objetivo en el suelo.")]
        public float GroundAcceleration = 70f;

        [Tooltip("Rapidez con la que se frena en el suelo (sin input horizontal).")]
        public float GroundDeceleration = 85f;

        [Tooltip("Rapidez con la que se corrige la horizontal en el aire (menos control).")]
        public float AirAcceleration = 38f;

        [Header("Salto")]
        [Tooltip("Altura maxima del salto en unidades. Se convierte a velocidad con JumpMath.")]
        public float JumpHeight = 2.75f;

        [Tooltip("Gravedad aplicada por el motor. El Rigidbody2D usa gravityScale = 0.")]
        public float Gravity = 34f;

        [Tooltip("Velocidad maxima de caida, para que la caida sea controlable.")]
        public float MaxFallSpeed = 24f;

        [Tooltip("Segundos de tolerancia para saltar tras abandonar el suelo (coyote time).")]
        public float CoyoteSeconds = 0.10f;

        [Tooltip("Segundos que un pulseo de salto queda guardado esperando aterrizaje (jump buffer).")]
        public float JumpBufferSeconds = 0.12f;

        [Tooltip("Factor al soltar el boton de salto en ascenso: menos altura = salto corto.")]
        [Range(0.1f, 1f)]
        public float JumpCutMultiplier = 0.45f;

        public float TargetSpeed(float moveX, bool runHeld)
        {
            float speed = MoveSpeed * (runHeld ? Mathf.Max(1f, RunSpeedMultiplier) : 1f);
            return moveX * speed;
        }
    }
}
