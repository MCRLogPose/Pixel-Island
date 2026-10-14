using System;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Enemies
{
    /// <summary>
    /// Vida de una criatura. Implementa <see cref="IDamageable"/> para que los
    /// proyectiles (que solo conocen la interfaz de Core) puedan herirla y, con la
    /// vida por defecto de 1, derribarla de un solo disparo.
    ///
    /// La vida se resuelve de forma perezosa porque Awake no corre al abrir una
    /// escena en edicion (ver AGENTS.md), y asi tambien responde a los tests de
    /// EditMode. Al morir se destruye el GameObject del enemigo.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/Enemies/Enemy Health")]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Tooltip("Disparos que aguanta el enemigo. 1 = cae al primer golpe.")]
        [SerializeField] int _maxHealth = 1;

        [Tooltip("Destruye el GameObject al quedarse sin vida.")]
        [SerializeField] bool _destroyOnDeath = true;

        int _health;
        bool _initialized;

        /// <summary>Se dispara al morir, antes de destruir el objeto.</summary>
        public event Action<EnemyHealth> Died;

        public int MaxHealth => Mathf.Max(1, _maxHealth);

        public int CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return _health;
            }
        }

        public bool IsAlive => CurrentHealth > 0;

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            _health = Mathf.Max(0, _health - amount);

            if (_health == 0)
            {
                Die();
            }
        }

        void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _health = MaxHealth;
            _initialized = true;
        }

        void Die()
        {
            Died?.Invoke(this);

            if (!_destroyOnDeath)
            {
                return;
            }

            // DestroyImmediate en edicion para que las herramientas y los tests de
            // EditMode vean el objeto fuera ya en el mismo fotograma.
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
