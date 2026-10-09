using PixelIsland.Collectibles;
using UnityEngine;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Muestra las monedas recogidas como "n/total" (0/4, 1/4, ..., 4/4).
    /// Escucha al <see cref="CoinPurse"/> del Explorador.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Coin HUD")]
    public sealed class CoinHud : MonoBehaviour
    {
        [Header("Origen de datos")]
        [Tooltip("Purse a escuchar. Vacio = se busca en la escena.")]
        [SerializeField] CoinPurse _purse;

        [Header("Iconos")]
        [Tooltip("Etiqueta que muestra \"n/total\".")]
        [SerializeField] Text _label;

        [Tooltip("Formato con {0}=recogidas y {1}=total.")]
        [SerializeField] string _format = "{0}/{1}";

        void OnEnable()
        {
            if (_purse == null)
            {
                _purse = FindObjectOfType<CoinPurse>();
            }

            if (_purse != null)
            {
                _purse.Changed += HandleChanged;
            }

            Refresh();
        }

        void OnDisable()
        {
            if (_purse != null)
            {
                _purse.Changed -= HandleChanged;
            }
        }

        /// <summary>Repinta la etiqueta con el estado actual del purse.</summary>
        public void Refresh()
        {
            if (_label == null || _purse == null)
            {
                return;
            }

            _label.text = string.Format(_format, _purse.Collected, _purse.Total);
        }

        void HandleChanged(CoinCounter counter) => Refresh();
    }
}
