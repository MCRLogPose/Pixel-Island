using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Enemies
{
    /// <summary>
    /// Dano por contacto del enemigo. Va en el collider trigger (isTrigger) del
    /// enemigo: cuando el Explorador entra o permanece dentro, le resta vidas.
    ///
    /// La ventana de invulnerabilidad la aplica <see cref="LivesComponent"/>, de
    /// modo que varios enemigos compartidos no drenan la vida en un solo frame.
    /// Se detecta al objetivo por su <see cref="LivesComponent"/>, no por tag ni
    /// layer, para que funcione aunque el jugador cambie de capa.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Pixel Island/Enemies/Enemy Contact Damage")]
    public sealed class EnemyContactDamage : MonoBehaviour
    {
        [Tooltip("Vidas que quita por impacto.")]
        [SerializeField] int _damage = 1;

        public int Damage
        {
            get => _damage;
            set => _damage = Mathf.Max(0, value);
        }

        void OnTriggerEnter2D(Collider2D other) => TryDamage(other);

        void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        void TryDamage(Collider2D other)
        {
            if (_damage <= 0 || other == null)
            {
                return;
            }

            // GetComponentInParent cubre el caso de golpear un collider hijo.
            LivesComponent lives = other.GetComponentInParent<LivesComponent>();
            if (lives == null)
            {
                return;
            }

            lives.TakeDamage(_damage);
        }
    }
}
