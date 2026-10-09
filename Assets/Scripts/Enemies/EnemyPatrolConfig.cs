using System;
using UnityEngine;

namespace PixelIsland.Enemies
{
    /// <summary>
    /// Datos de tuning de la patrulla de un enemigo. Objeto serializable para
    /// editarlo en el Inspector del EnemyPatrolController, de modo que cada
    /// enemigo lleva SU propia linea de patrulla y no hace falta crear objetos
    /// punto A / punto B en la jerarquia (ver AGENTS.md).
    /// Es SOLO datos: la logica vive en <see cref="PatrolMotor"/>.
    /// </summary>
    [Serializable]
    public sealed class EnemyPatrolConfig
    {
        [Tooltip("Velocidad de patrulla en unidades por segundo (1 unidad = 32 px).")]
        public float MoveSpeed = 3.5f;

        [Tooltip("Distancia desde el punto de aparicion hacia CADA lado. El enemigo " +
                 "recorre la linea [inicio - distancia, inicio + distancia].")]
        public float PatrolDistance = 3f;

        [Tooltip("Altura maxima del salto en unidades. Se convierte a velocidad con JumpMath. " +
                 "Debe dar tiempo en el aire para librar un obstaculo de un tile.")]
        public float JumpHeight = 3f;

        [Tooltip("Gravedad aplicada por el motor. El Rigidbody2D usa gravityScale = 0.")]
        public float Gravity = 30f;

        [Tooltip("Velocidad maxima de caida, para que la caida sea controlable.")]
        public float MaxFallSpeed = 20f;
    }
}
