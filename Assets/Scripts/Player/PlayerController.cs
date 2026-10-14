using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Explorador: pieza principal del jugador. Es un cable thin entre cuatro
    /// piezas testeables por separado:
    ///   - <see cref="IPlayerInputSource"/> traduce la entrada a MoveInput
    ///   - <see cref="GroundSensor"/>        responde si hay suelo bajo los pies
    ///   - <see cref="PlayerMotor"/>         calcula la velocidad
    ///   - <see cref="StateMachine"/> + <see cref="PlayerState"/> el patron State
    ///
    /// El Rigidbody2D debe llevar gravityScale = 0, freezeRotation y CCD,
    /// porque la gravedad la aplica el motor y no Unity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Componente que implemente IPlayerInputSource (PlayerInputReader en el juego).")]
        [SerializeField] MonoBehaviour _inputSource;
        [SerializeField] GroundSensor _groundSensor;
        [SerializeField] SpriteRenderer _spriteRenderer;

        [Tooltip("Disparo del Explorador. Vacio = se resuelve en este GameObject.")]
        [SerializeField] PlayerShooter _shooter;

        [Header("Ajustes de movimiento")]
        [SerializeField] MovementConfig _movement = new MovementConfig();

        Rigidbody2D _body;
        PlayerMotor _motor;
        StateMachine _states;
        MoveInput _currentInput;

        PlayerIdleState _idle;
        PlayerRunState _run;
        PlayerJumpState _jump;
        PlayerFallState _fall;

        public PlayerIdleState IdleState => _idle;
        public PlayerRunState RunState => _run;
        public PlayerJumpState JumpState => _jump;
        public PlayerFallState FallState => _fall;

        public MovementConfig Movement => _movement;
        public Vector2 Velocity => _body != null ? _body.velocity : Vector2.zero;
        public bool IsGrounded => _groundSensor != null && _groundSensor.IsGrounded;

        /// <summary>Entrada cacheada del paso fisico en curso.</summary>
        public MoveInput MoveInput => _currentInput;

        /// <summary>True solo en el paso fisico en el que el motor disparo un salto.</summary>
        public bool JumpedThisStep => _motor != null && _motor.JumpedThisStep;

        public string CurrentStateName => _states != null ? _states.CurrentName : "<none>";
        public bool FacingLeft => _spriteRenderer != null && _spriteRenderer.flipX;

        IPlayerInputSource InputSource
        {
            get
            {
                if (_inputSource == null)
                {
                    _inputSource = GetComponent<IPlayerInputSource>() as MonoBehaviour;
                }

                return _inputSource as IPlayerInputSource;
            }
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (_groundSensor == null)
            {
                _groundSensor = GetComponent<GroundSensor>();
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_groundSensor != null)
            {
                _groundSensor.SetGroundMask(GameLayers.GroundMask);
            }

            if (InputSource == null)
            {
                Debug.LogError("[Player] " + name +
                               " necesita un componente que implemente IPlayerInputSource.");
            }

            _motor = new PlayerMotor(_movement);

            _states = new StateMachine();
            _idle = new PlayerIdleState(this);
            _run = new PlayerRunState(this);
            _jump = new PlayerJumpState(this);
            _fall = new PlayerFallState(this);
            _states.StateChanged += HandleStateChanged;
        }

        void OnEnable()
        {
            if (_states != null)
            {
                _states.ChangeState(_idle);
            }
        }

        void OnDisable()
        {
            _states?.Reset();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            _currentInput = InputSource != null ? InputSource.ReadInput() : MoveInput.None;

            if (_shooter == null)
            {
                _shooter = GetComponent<PlayerShooter>();
            }

            _shooter?.TryFire(_currentInput.FirePressed);

            bool grounded = IsGrounded;
            Vector2 velocity = _motor.Step(_body.velocity, _currentInput, grounded, dt);

            // El motor anula la velocidad vertical en cuanto el sensor ve suelo, asi
            // que sin este pegado el cuerpo se quedaria congelado a media caida. Se
            // apoya en la superficie real antes de integrar el movimiento.
            if (grounded && velocity.y <= 0f && !_motor.JumpedThisStep &&
                _groundSensor.TryGetSnapOffset(out Vector2 snap))
            {
                _body.position += snap;
            }

            _body.velocity = velocity;

            _states.Tick(dt);
        }

        /// <summary>Pide un cambio de estado. Lo invocan los propios estados.</summary>
        public void SetState(IState next)
        {
            _states.ChangeState(next);
        }

        /// <summary>
        /// Aplica un rebote externo (p.ej. el dolor de los pinchos): fija la
        /// velocidad, cede el control al impulso unos instantes y pasa al estado
        /// aereo correspondiente. Lo invoca la reaccion de dano del Explorador.
        /// </summary>
        public void ApplyImpulse(Vector2 impulse, float lockSeconds = 0f)
        {
            if (_body != null)
            {
                _body.velocity = impulse;
            }

            _motor?.ApplyImpulse(impulse, lockSeconds);

            if (_states == null)
            {
                return;
            }

            SetState(impulse.y > 0f ? (IState)_jump : (IState)_fall);
        }

        /// <summary>Orienta el sprite segun el sentido de avance.</summary>
        public void FaceTowards(float signedValue)
        {
            if (_spriteRenderer == null || Mathf.Approximately(signedValue, 0f))
            {
                return;
            }

            _spriteRenderer.flipX = signedValue < 0f;
        }

        void HandleStateChanged(IState from, IState to)
        {
            Debug.Log("[Player] Estado: " + (from != null ? from.Name : "<none>") + " -> " + to.Name);
        }
    }
}
