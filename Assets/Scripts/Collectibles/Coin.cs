using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Collectibles
{
    /// <summary>
    /// Moneda recogible. Es un trigger: cuando el Player entra, suma una al
    /// <see cref="CoinPurse"/> (se resuelve del propio Player, o del campo
    /// asignado) y se oculta. El total del nivel lo lleva el purse, no la moneda.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Pixel Island/Collectibles/Coin")]
    public sealed class Coin : MonoBehaviour
    {
        [Tooltip("Purse al que sumar. Vacio = se busca en el Player que entra.")]
        [SerializeField] CoinPurse _purse;

        [Tooltip("Oculta la moneda al recogerla (recomendado). Si no, se destruye.")]
        [SerializeField] bool _hideOnPickup = true;

        bool _collected;

        void Reset()
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || !other.CompareTag(GameLayers.TagPlayer))
            {
                return;
            }

            CoinPurse purse = ResolvePurse(other);
            if (purse == null)
            {
                return;
            }

            if (!purse.Collect())
            {
                return;
            }

            _collected = true;

            if (_hideOnPickup)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        CoinPurse ResolvePurse(Collider2D other)
        {
            if (_purse != null)
            {
                return _purse;
            }

            return other.GetComponentInParent<CoinPurse>();
        }
    }
}
