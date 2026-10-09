using System;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Collectibles
{
    /// <summary>
    /// Puente MonoBehaviour para <see cref="CoinCounter"/>. Va en el Explorador y da
    /// a las monedas y al HUD un sitio unico donde sumar y leer.
    ///
    /// El modelo se resuelve de forma perezosa porque Awake no corre al abrir una
    /// escena en edicion (ver AGENTS.md).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/Collectibles/Coin Purse")]
    public sealed class CoinPurse : MonoBehaviour
    {
        [Tooltip("Total de monedas del nivel. Debe coincidir con las monedas colocadas.")]
        [SerializeField] int _totalCoins = 4;

        /// <summary>Se reemite el cambio del modelo para que el HUD solo escuche aqui.</summary>
        public event Action<CoinCounter> Changed;

        CoinCounter _counter;

        public CoinCounter Value
        {
            get
            {
                if (_counter == null)
                {
                    _counter = new CoinCounter(Mathf.Max(0, _totalCoins));
                }

                return _counter;
            }
        }

        public int Collected => Value.Collected;

        public int Total => Value.Total;

        public bool IsComplete => Value.IsComplete;

        /// <summary>Suma una moneda. Devuelve false si ya estaban todas.</summary>
        public bool Collect() => Value.Collect();

        void OnEnable()
        {
            Value.Changed += HandleChanged;
        }

        void OnDisable()
        {
            Value.Changed -= HandleChanged;
        }

        void HandleChanged(CoinCounter counter) => Changed?.Invoke(counter);
    }
}
