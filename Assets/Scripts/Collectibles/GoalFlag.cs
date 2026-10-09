using System;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Collectibles
{
    /// <summary>
    /// Bandera de meta (meta final tipo Mario). Es un trigger: al tocarla el Player
    /// se completa el nivel si ya recogio todas las monedas. Si no, avisa por
    /// <see cref="Blocked"/> y deja la bandera en pie.
    ///
    /// Solo decide; quien reacciona (HUD, cambios de escena) escucha sus eventos.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Pixel Island/Collectibles/Goal Flag")]
    public sealed class GoalFlag : MonoBehaviour
    {
        [Tooltip("Purse a consultar. Vacio = se busca en el Player que entra.")]
        [SerializeField] CoinPurse _purse;

        [Tooltip("Si esta activo, la bandera no completa el nivel hasta recoger todas las monedas.")]
        [SerializeField] bool _requireAllCoins = true;

        [Tooltip("Desactiva el collider al completar, para no re-disparar.")]
        [SerializeField] bool _disableOnReach = true;

        /// <summary>Se dispara al tocar la bandera sin tener todas las monedas.</summary>
        public event Action<GoalFlag> Blocked;

        /// <summary>Se dispara al completar el nivel.</summary>
        public event Action<GoalFlag> Reached;

        public bool Completed { get; private set; }

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
            if (Completed || !other.CompareTag(GameLayers.TagPlayer))
            {
                return;
            }

            CoinPurse purse = ResolvePurse(other);
            if (_requireAllCoins && purse != null && !purse.IsComplete)
            {
                Blocked?.Invoke(this);
                return;
            }

            Completed = true;
            Reached?.Invoke(this);

            if (_disableOnReach)
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
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
