using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Enemies
{
    /// <summary>
    /// Enemigo que patrulla. Es un cable thin entre piezas testeables:
    ///   - <see cref="EnemyPatrolConfig"/> datos de tuning
    ///   - <see cref="PatrolMotor"/>          calcula la velocidad (logica pura)
    ///   - sensores de suelo y de obstaculo   OverlapBox contra las capas solidas
    ///
    /// Patrulla sobre una linea horizontal centrada en su punto de aparicion
    /// (extremo a <c>_patrol.PatrolDistance</c> a cada lado) y salta cuando el
    /// sensor frontal detecta un obstaculo. El Animator recibe el parametro int
    /// "State" con los indices de <see cref="EnemyAnimState"/>.
    ///
    /// El Rigidbody2D debe llevar gravityScale = 0 y freezeRotation, porque la
    /// gravedad la aplica el motor y no Unity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Pixel Island/Enemies/Enemy Patrol Controller")]
    public sealed class EnemyPatrolController : MonoBehaviour
    {
        [Header("Patrulla")]
        [SerializeField] EnemyPatrolConfig _patrol = new EnemyPatrolConfig();

        [Header("Referencias")]
        [Tooltip("Animator del enemigo. Vacio = se resuelve en este GameObject.")]
        [SerializeField] Animator _animator;

        [Tooltip("SpriteRenderer que se voltea segun el sentido. Vacio = este GameObject.")]
        [SerializeField] SpriteRenderer _spriteRenderer;

        [Tooltip("Capas que cuentan como suelo y como obstaculo. Vacio = Ground + Obstacle.")]
        [SerializeField] LayerMask _solidMask;

        [Header("Sensores")]
        [Tooltip("Centro del box de suelo, relativo al pivote (centro del sprite). " +
                 "Debe quedar a ras del fondo del collider solido (que acaba en y = -0.4).")]
        [SerializeField] Vector2 _feetOffset = new Vector2(0f, -0.5f);

        [Tooltip("Tamano del box de suelo. Se extiende por debajo de los pies para " +
                 "detectar el suelo aunque el cuerpo descanse un pixel por encima.")]
        [SerializeField] Vector2 _feetProbe = new Vector2(1.2f, 0.25f);

        [Tooltip("Distancia del sensor de obstaculo hacia el frente, relativa al centro.")]
        [SerializeField] float _obstacleDistance = 0.85f;

        [Tooltip("Tamano del box de obstaculo. Debe quedar por encima del suelo para " +
                 "no confundir el suelo plano con un obstaculo.")]
        [SerializeField] Vector2 _obstacleProbe = new Vector2(0.25f, 0.5f);

        [Tooltip("Altura del sensor de obstaculo relativa al pivote.")]
        [SerializeField] float _obstacleOffsetY = -0.1f;

        readonly Collider2D[] _hits = new Collider2D[4];

        Rigidbody2D _body;
        PatrolMotor _motor;

        public EnemyPatrolConfig Patrol => _patrol;

        /// <summary>Sentido de avance actual: +1 derecha, -1 izquierda.</summary>
        public int Direction => _motor != null ? _motor.Direction : 1;

        public bool IsGrounded { get; private set; }

        /// <summary>Ultimo estado de animacion calculado (para el Animator y tests).</summary>
        public EnemyAnimState AnimState { get; private set; } = EnemyAnimState.Idle;

        public bool FacingLeft => _spriteRenderer != null && _spriteRenderer.flipX;

        /// <summary>Parametro int que comparten el controller del enemigo y su driver.</summary>
        public const string StateParameter = "State";

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_solidMask.value == 0)
            {
                _solidMask = GameLayers.GroundMask;
            }

            _motor = new PatrolMotor(_patrol, transform.position.x);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 position = _body.position;

            bool grounded = CheckGrounded(position);
            bool obstacleAhead = grounded && ObstacleAhead(position, _motor.Direction);

            Vector2 velocity = _motor.Step(_body.velocity, position.x, grounded, obstacleAhead, dt);
            _body.velocity = velocity;

            IsGrounded = grounded;
            FaceMovement();
            UpdateAnimState(grounded, velocity.y);
        }

        bool CheckGrounded(Vector2 position)
        {
            Vector2 center = position + _feetOffset;
            int count = Physics2D.OverlapBoxNonAlloc(center, _feetProbe, 0f, _hits, _solidMask);
            return HasForeignHit(count);
        }

        bool ObstacleAhead(Vector2 position, int direction)
        {
            Vector2 center = position + new Vector2(direction * _obstacleDistance, _obstacleOffsetY);
            int count = Physics2D.OverlapBoxNonAlloc(center, _obstacleProbe, 0f, _hits, _solidMask);
            return HasForeignHit(count);
        }

        bool HasForeignHit(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hits[i];
                if (hit == null)
                {
                    continue;
                }

                if (hit.attachedRigidbody == _body)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        void FaceMovement()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.flipX = _motor.Direction < 0;
        }

        void UpdateAnimState(bool grounded, float velocityY)
        {
            EnemyAnimState state;
            if (grounded)
            {
                state = EnemyAnimState.Idle;
            }
            else
            {
                state = velocityY > 0.01f ? EnemyAnimState.Jump : EnemyAnimState.Fall;
            }

            AnimState = state;

            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetInteger(StateParameter, (int)state);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Application.isPlaying ? (IsGrounded ? Color.green : Color.red) : Color.cyan;
            Gizmos.DrawWireCube(transform.position + (Vector3)_feetOffset, _feetProbe);

            Gizmos.color = Color.yellow;
            int direction = Application.isPlaying ? Direction : 1;
            Gizmos.DrawWireCube(
                transform.position + new Vector3(direction * _obstacleDistance, _obstacleOffsetY, 0f),
                _obstacleProbe);
        }
    }
}
