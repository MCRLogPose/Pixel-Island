using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Hazards
{
    /// <summary>
    /// Pinchos: solidos de costado (el Explorador choca y no pasa) pero solo restan
    /// vidas si cae sobre la punta. Va en el collider solido de los pinchos.
    ///
    /// La ventana de invulnerabilidad la aplica <see cref="LivesComponent"/>, igual
    /// que con los enemigos, para que estar sobre la punta no drene la vida en un
    /// solo frame. Se detecta al objetivo por su <see cref="LivesComponent"/>, no por
    /// tag ni layer, para que funcione aunque el jugador cambie de capa.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Pixel Island/Hazards/Spike Hazard")]
    public sealed class SpikeHazard : MonoBehaviour
    {
        [Tooltip("Vidas que quita un pisotazo sobre la punta.")]
        [SerializeField] int _damage = 1;

        [Tooltip("Cuanto mas vertical que lateral debe ser el contacto para contar como punta.")]
        [SerializeField] float _minVerticalRatio = SpikeHit.DefaultVerticalRatio;

        [Tooltip("Margen por debajo de la punta que aun cuenta como pisotazo.")]
        [SerializeField] float _feetTolerance = SpikeHit.DefaultFeetTolerance;

        Collider2D _collider;

        Collider2D Collider
        {
            get
            {
                if (_collider == null)
                {
                    _collider = GetComponent<Collider2D>();
                }

                return _collider;
            }
        }

        public int Damage
        {
            get => _damage;
            set => _damage = Mathf.Max(0, value);
        }

        void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision);

        void OnCollisionStay2D(Collision2D collision) => TryDamage(collision);

        void TryDamage(Collision2D collision)
        {
            if (_damage <= 0 || collision == null)
            {
                return;
            }

            // GetComponentInParent cubre el caso de golpear un collider hijo.
            LivesComponent lives = collision.collider.GetComponentInParent<LivesComponent>();
            if (lives == null)
            {
                return;
            }

            float spikeTopY = Collider.bounds.max.y;
            float targetFeetY = collision.collider.bounds.min.y;

            ContactPoint2D[] contacts = collision.contacts;
            for (int i = 0; i < contacts.Length; i++)
            {
                if (SpikeHit.IsTopHit(contacts[i].normal, spikeTopY, targetFeetY, _minVerticalRatio, _feetTolerance))
                {
                    Hurt(lives, collision.collider);
                    return;
                }
            }
        }

        void Hurt(LivesComponent lives, Collider2D target)
        {
            int applied = lives.TakeDamage(_damage);
            if (applied <= 0)
            {
                return;
            }

            // Solo reacciona si el dano entro de verdad (no en la invulnerabilidad).
            IDamageReaction reaction = target.GetComponentInParent<IDamageReaction>();
            if (reaction != null)
            {
                reaction.OnDamaged(transform.position);
            }
        }
    }
}
