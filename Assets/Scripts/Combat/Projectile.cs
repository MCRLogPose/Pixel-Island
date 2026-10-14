using System;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Combat
{
    /// <summary>
    /// Proyectil disparado por el Explorador. Se mueve en linea recta en la
    /// direccion de lanzamiento y se elimina (vuelve al pool de
    /// <see cref="ProjectileSingleton"/>) cuando:
    ///   - toca un objetivo con <see cref="IDamageable"/> (y le hace dano),
    ///   - toca el terreno (suelo o el layer que configure el campo de mascara),
    ///   - sale del encuadre de la camara, o
    ///   - agota su tiempo de vida de seguridad.
    ///
    /// Al ser un Rigidbody2D kinematico con collider trigger, el contacto con los
    /// enemigos llega por eventos (la matriz de fisica ya empareja Projectile con
    /// Enemy) mientras que el terreno se consulta con un Linecast contra una
    /// LayerMask, porque la matriz no empareja Projectile con Ground a proposito
    /// (ver ProjectSetupTests). Asi el proyectil nunca empuja al jugador ni al
    /// terreno y aun asi desaparece al chocar con ellos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Pixel Island/Combat/Projectile")]
    public sealed class Projectile : MonoBehaviour
    {
        [Header("Movimiento")]
        [Tooltip("Unidades por segundo. PlayerShooter lo puede sobrescribir al disparar.")]
        [SerializeField] float _speed = 14f;

        [Tooltip("Segundos maximos en vuelo antes de reciclarse, aunque no choque con nada.")]
        [SerializeField] float _maxLifetime = 6f;

        [Header("Deteccion")]
        [Tooltip("Capas que cuentan como terreno. Vacio = Ground + Obstacle.")]
        [SerializeField] LayerMask _terrainMask;

        [Tooltip("El proyectil se recicla al salir del encuadre de la camara.")]
        [SerializeField] bool _despawnOffCamera = true;

        [Tooltip("Margen del encuadre, en fraccion de pantalla, antes de considerarlo fuera.")]
        [SerializeField] float _cameraMargin = 0.05f;

        [Tooltip("Dano que aplica al objetivo golpeado.")]
        [SerializeField] int _damage = 1;

        [Header("Visual")]
        [Tooltip("Renderer del proyectil. Vacio = este GameObject o un hijo.")]
        [SerializeField] SpriteRenderer _spriteRenderer;

        [Tooltip("Voltea el sprite en el eje local Y al viajar hacia la izquierda.")]
        [SerializeField] bool _flipYWhenMovingLeft = true;

        Rigidbody2D _body;
        Collider2D _collider;
        Action<Projectile> _release;

        Vector2 _direction = Vector2.right;
        float _age;
        bool _spent;

        /// <summary>Sentido de vuelo actual (normalizado).</summary>
        public Vector2 Direction => _direction;

        /// <summary>True mientras el proyectil ya fue golpeado o reciclado.</summary>
        public bool IsSpent => _spent;

        /// <summary>Velocidad de vuelo configurada.</summary>
        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0f, value);
        }

        Rigidbody2D Body
        {
            get
            {
                if (_body == null)
                {
                    _body = GetComponent<Rigidbody2D>();
                }

                return _body;
            }
        }

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

        void Awake()
        {
            Cache();
        }

        void Cache()
        {
            Rigidbody2D body = Body;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (_terrainMask.value == 0)
            {
                _terrainMask = GameLayers.GroundMask;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        /// <summary>Conecta el proyectil con quien lo recicla (lo hace el singleton).</summary>
        public void Setup(Action<Projectile> release)
        {
            _release = release;
        }

        /// <summary>
        /// Prepara el proyectil para un nuevo disparo. La direccion se normaliza;
        /// una direccion nula cae a "derecha" para no dejar el proyectil sin rumbo.
        /// </summary>
        public void Launch(Vector2 direction, float speed = 0f, float lifetime = 0f)
        {
            Cache();

            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            if (speed > 0f)
            {
                _speed = speed;
            }

            if (lifetime > 0f)
            {
                _maxLifetime = lifetime;
            }

            _age = 0f;
            _spent = false;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipY = _flipYWhenMovingLeft && _direction.x < 0f;
            }

            gameObject.SetActive(true);
        }

        void FixedUpdate()
        {
            if (_spent)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            _age += dt;

            if (_age >= _maxLifetime)
            {
                Despawn();
                return;
            }

            Vector2 current = Body.position;
            Vector2 next = current + _direction * _speed * dt;

            // Se consulta el segmento recorrido en este paso y no solo el punto
            // final: a 14 u/s el proyectil avanza ~0.28 u por paso y un Linecast
            // evita que un suelo fino se cuele entre dos muestras.
            if (HitsTerrain(current, next))
            {
                Despawn();
                return;
            }

            Body.MovePosition(next);

            if (_despawnOffCamera && IsOffCamera(Camera.main, next, _cameraMargin))
            {
                Despawn();
            }
        }

        bool HitsTerrain(Vector2 from, Vector2 to)
        {
            if (_terrainMask.value == 0)
            {
                return false;
            }

            return Physics2D.Linecast(from, to, _terrainMask);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_spent || other == null)
            {
                return;
            }

            // GetComponentInParent cubre golpear el collider trigger o el solido
            // del enemigo indistintamente.
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(_damage);
            }

            // Un proyectil se gasta con cualquier impacto valido: si no habia
            // objetivo con vida, igualmente desaparece (nunca rebota).
            Despawn();
        }

        /// <summary>Apaga el proyectil y avisa al pool. Idempotente.</summary>
        public void Despawn()
        {
            if (_spent)
            {
                return;
            }

            _spent = true;
            gameObject.SetActive(false);
            _release?.Invoke(this);
        }

        /// <summary>
        /// True si el punto del mundo queda fuera del encuadre de la camara (con
        /// margen). Logica pura para poder testearla con una camara cualquiera.
        /// </summary>
        public static bool IsOffCamera(Camera camera, Vector3 worldPosition, float margin)
        {
            if (camera == null)
            {
                return false;
            }

            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            if (viewport.z < 0f)
            {
                return true;
            }

            return viewport.x < -margin || viewport.x > 1f + margin ||
                   viewport.y < -margin || viewport.y > 1f + margin;
        }
    }
}
