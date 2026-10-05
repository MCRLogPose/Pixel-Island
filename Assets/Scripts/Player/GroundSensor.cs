using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Detecta suelo con un OverlapBox bajo los pies. Se consulta en FixedUpdate
    /// y su ultimo resultado queda disponible para el PlayerController y el
    /// patron State.
    ///
    /// Se usa OverlapBox y no IsTouchingLayers porque el box cae dentro del
    /// propio collider del jugador: los hits se filtran descartando el rigidbody
    /// propio, y se usa NonAlloc para no generar basura por paso fisico.
    /// </summary>
    public sealed class GroundSensor : MonoBehaviour
    {
        [Tooltip("Centro del box relativo al pivote del jugador. El arte se importa " +
                 "con pivote abajo-centro, asi que los pies estan en y = 0.")]
        [SerializeField] Vector2 _boxOffset = new Vector2(0f, -0.03f);

        [Tooltip("El box se extiende por DEBAJO de la linea de los pies a proposito: " +
                 "cuanto mas sobresale, mas rapido se detecta el suelo al caer y menos " +
                 "riesgo hay de perderse un fotograma al rozar una superficie. La " +
                 "distancia real hasta la superficie la resuelve el pegado (snap), no " +
                 "este box, asi que el ancho no produce flotacion visible.")]
        [SerializeField] Vector2 _boxSize = new Vector2(0.56f, 0.10f);

        [Tooltip("Distancia maxima a la que se pega el jugador a la superficie cuando " +
                 "el box detecta suelo. Cubre el hueco que queda entre el box y el " +
                 "suelo sin permitir teletransportes.")]
        [SerializeField] float _maxSnapDistance = 0.14f;

        [Tooltip("Ancho del box usado para el pegado. algo mas estrecho que el de " +
                 "deteccion para no engancharse con el borde de una plataforma.")]
        [SerializeField] Vector2 _snapSize = new Vector2(0.44f, 0.06f);

        [Tooltip("Capas que cuentan como suelo. Lo asigna el PlayerController.")]
        [SerializeField] LayerMask _groundMask = ~0;

        readonly Collider2D[] _hits = new Collider2D[4];
        readonly RaycastHit2D[] _castHits = new RaycastHit2D[4];
        Rigidbody2D _selfBody;

        /// <summary>Resultado del ultimo paso fisico.</summary>
        public bool IsGrounded { get; private set; }

        void Awake()
        {
            _selfBody = GetComponent<Rigidbody2D>();
        }

        public void SetGroundMask(LayerMask mask)
        {
            _groundMask = mask;
        }

        void FixedUpdate()
        {
            Vector2 center = (Vector2)transform.position + _boxOffset;
            int count = Physics2D.OverlapBoxNonAlloc(center, _boxSize, 0f, _hits, _groundMask);

            IsGrounded = false;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i] == null)
                {
                    continue;
                }

                if (_selfBody != null && _hits[i].attachedRigidbody == _selfBody)
                {
                    continue;
                }

                IsGrounded = true;
                return;
            }
        }

        /// <summary>
        /// Calcula cuanto hay que bajar para apoyar los pies exactamente en la
        /// superficie mas cercana.
        ///
        /// Hace falta porque el motor anula la velocidad vertical en cuanto el sensor
        /// dice que hay suelo: sin este pegado el jugador se quedaria congelado a
        /// media caida, hasta dos pixeles por encima del suelo. Devuelve false si no
        /// hay superficie dentro de la ventana, en cuyo caso el jugador sigue cayendo
        /// con normalidad (por ejemplo al salir por un borde).
        /// </summary>
        public bool TryGetSnapOffset(out Vector2 offset)
        {
            offset = Vector2.zero;

            Vector2 feet = transform.position;
            int count = Physics2D.BoxCastNonAlloc(feet, _snapSize, 0f, Vector2.down,
                _castHits, _maxSnapDistance, _groundMask);

            float closest = float.PositiveInfinity;
            float surfaceY = 0f;

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _castHits[i].collider;
                if (hit == null)
                {
                    continue;
                }

                // El propio collider del jugador arranca dentro del box del cast.
                if (_selfBody != null && hit.attachedRigidbody == _selfBody)
                {
                    continue;
                }

                if (_castHits[i].distance < closest)
                {
                    closest = _castHits[i].distance;
                    surfaceY = _castHits[i].point.y;
                }
            }

            if (closest > _maxSnapDistance)
            {
                return false;
            }

            // Se usa el punto de contacto y no la distancia recorrida: la distancia
            // se mide desde el borde inferior de la caja del cast, no desde los pies,
            // y mezclar ambos deja al jugador flotando media caja por encima.
            offset = new Vector2(0f, surfaceY - feet.y);
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Vector2 center = (Vector2)transform.position + _boxOffset;
            Gizmos.color = Application.isPlaying ? (IsGrounded ? Color.green : Color.red) : Color.cyan;
            Gizmos.DrawWireCube(center, _boxSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, _snapSize);
            Gizmos.DrawLine(transform.position,
                transform.position + Vector3.down * _maxSnapDistance);
        }
    }
}
