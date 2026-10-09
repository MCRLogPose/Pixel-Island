using System;
using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Contador de vidas. Logica pura, sin escena: se testea en EditMode.
    /// Arranca al maximo; <see cref="LoseLife"/> baja una y avisa por <see cref="Changed"/>.
    /// </summary>
    public sealed class Lives
    {
        int _current;
        int _max;

        /// <summary>Se dispara cada vez que cambia el numero de vidas.</summary>
        public event Action<Lives> Changed;

        public Lives(int maxLives)
        {
            _max = Mathf.Max(1, maxLives);
            _current = _max;
        }

        public int Max => _max;

        public int Current => _current;

        public bool IsEmpty => _current <= 0;

        /// <summary>Resta una vida. Devuelve false si ya estaba a cero.</summary>
        public bool LoseLife()
        {
            if (_current <= 0)
            {
                return false;
            }

            _current--;
            Changed?.Invoke(this);
            return true;
        }

        /// <summary>Deja las vidas al maximo y avisa si cambio algo.</summary>
        public void Refill()
        {
            Set(_max);
        }

        /// <summary>Fija las vidas actuales, recortadas a [0, Max].</summary>
        public void Set(int lives)
        {
            int clamped = Mathf.Clamp(lives, 0, _max);
            if (clamped == _current)
            {
                return;
            }

            _current = clamped;
            Changed?.Invoke(this);
        }
    }
}
