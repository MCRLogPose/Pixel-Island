using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Temporizador de gracia reutilizable. Implementa las dos ventanas de
    /// tolerancia del salto: coyote time (poder saltar un instante despues de
    /// haber salido del borde) y jump buffer (poder saltar un instante despues
    /// de haber pulsado, justo antes de aterrizar).
    /// Logica pura: se testea en EditMode.
    /// </summary>
    public sealed class GraceTimer
    {
        float _remaining;

        public float Duration { get; }
        public bool IsActive => _remaining > 0f;
        public float Remaining => _remaining;

        public GraceTimer(float duration)
        {
            Duration = Mathf.Max(0f, duration);
        }

        public void Arm()
        {
            _remaining = Duration;
        }

        public void Tick(float deltaTime)
        {
            if (_remaining > 0f)
            {
                _remaining -= deltaTime;
                if (_remaining < 0f)
                {
                    _remaining = 0f;
                }
            }
        }

        /// <summary>Devuelve true una sola vez si habia ventana activa, y la cierra.</summary>
        public bool Consume()
        {
            if (_remaining <= 0f)
            {
                return false;
            }

            _remaining = 0f;
            return true;
        }

        public void Clear()
        {
            _remaining = 0f;
        }
    }
}
