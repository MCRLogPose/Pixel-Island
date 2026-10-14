using UnityEngine;

namespace PixelIsland.Combat
{
    /// <summary>
    /// Control de disparo puro (no MonoBehaviour): decide si se puede disparar
    /// segun un interruptor (habilitado por nivel) y un tiempo de recarga. Igual
    /// que PlayerMotor, es logica determinista que se testea en EditMode sin
    /// escena, y el tiempo entra como parametro en vez de leer Time.time.
    /// </summary>
    public sealed class FireControl
    {
        float _nextReadyTime;

        public FireControl(float cooldownSeconds, bool enabled = true)
        {
            Cooldown = Mathf.Max(0f, cooldownSeconds);
            Enabled = enabled;
        }

        /// <summary>Segundos de separacion entre disparos.</summary>
        public float Cooldown { get; set; }

        /// <summary>Interruptor de nivel: en false no se dispara nunca.</summary>
        public bool Enabled { get; set; }

        /// <summary>True si en este instante se puede disparar.</summary>
        public bool CanFire(float now) => Enabled && now >= _nextReadyTime;

        /// <summary>Segundos que faltan para el proximo disparo (0 si ya esta listo).</summary>
        public float RemainingCooldown(float now) => Mathf.Max(0f, _nextReadyTime - now);

        /// <summary>
        /// Intenta disparar: si esta habilitado y la recarga ha terminado, arma el
        /// proximo disparo y devuelve true. En caso contrario no consume nada.
        /// </summary>
        public bool TryConsume(float now)
        {
            if (!CanFire(now))
            {
                return false;
            }

            _nextReadyTime = now + Cooldown;
            return true;
        }

        /// <summary>Reinicia la recarga (p.ej. al reaparecer).</summary>
        public void Reset()
        {
            _nextReadyTime = 0f;
        }
    }
}
