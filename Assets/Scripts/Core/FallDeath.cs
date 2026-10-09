using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Detecta una caida al vacio: la altura del objetivo cruzó el plano de muerte.
    /// Logica pura, sin escena: se testea en EditMode.
    /// Avisa una sola vez; hay que <see cref="Rearm"/> para volver a vigilar.
    /// </summary>
    public sealed class FallDeath
    {
        readonly float _killPlaneY;

        public bool Triggered { get; private set; }

        public float KillPlaneY => _killPlaneY;

        public FallDeath(float killPlaneY)
        {
            _killPlaneY = killPlaneY;
        }

        /// <summary>
        /// Devuelve true la primera vez que <paramref name="targetY"/> cae al plano
        /// o por debajo. Despues de disparar no vuelve a disparar hasta Rearm().
        /// </summary>
        public bool Check(float targetY)
        {
            if (Triggered)
            {
                return false;
            }

            if (targetY > _killPlaneY)
            {
                return false;
            }

            Triggered = true;
            return true;
        }

        /// <summary>Vuelve a armar el detector para una nueva caida.</summary>
        public void Rearm()
        {
            Triggered = false;
        }
    }
}
