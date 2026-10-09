using System;
using UnityEngine;

namespace PixelIsland.Collectibles
{
    /// <summary>
    /// Cuenta de monedas recogidas sobre un total fijo. Logica pura, sin escena
    /// (igual que <c>Lives</c>), de modo que se testea en EditMode.
    ///
    /// El total lo fija el nivel (las monedas que coloca); <see cref="Collect"/>
    /// suma una y avisa por <see cref="Changed"/>.
    /// </summary>
    public sealed class CoinCounter
    {
        readonly int _total;
        int _collected;

        /// <summary>Se dispara cada vez que cambia el numero de monedas.</summary>
        public event Action<CoinCounter> Changed;

        public CoinCounter(int total)
        {
            _total = Mathf.Max(0, total);
        }

        public int Total => _total;

        public int Collected => _collected;

        public int Remaining => _total - _collected;

        /// <summary>True cuando ya se han recogido todas las monedas del nivel.</summary>
        public bool IsComplete => _collected >= _total;

        /// <summary>Suma una moneda. Devuelve false si ya estaban todas.</summary>
        public bool Collect()
        {
            if (_collected >= _total)
            {
                return false;
            }

            _collected++;
            Changed?.Invoke(this);
            return true;
        }
    }
}
