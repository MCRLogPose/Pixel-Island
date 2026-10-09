using UnityEngine;

namespace PixelIsland.Environment
{
    /// <summary>
    /// Seguimiento de camara en 2D con suavizado y zona muerta vertical.
    ///
    /// Es deliberadamente pequeno: solo existe para poder validar el parallax
    /// caminando por la escena. Cuando llegue el LevelManager de la etapa 3 este
    /// script es el sitio a sustituir por la camara con limites de nivel.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Pixel Island/Environment/Camera Follow 2D")]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [Tooltip("Objeto a seguir. Vacio = el objeto con el tag Player.")]
        [SerializeField] Transform _target;

        [Header("Suavizado")]
        [Tooltip("Segundos que tarda la camara en alcanzar al objetivo. 0 = sin suavizado.")]
        [SerializeField] float _smoothTime = 0.12f;

        [Tooltip("Margen vertical en unidades. El objetivo vertical solo se persigue " +
                 "cuando se sale de esta franja, para que saltar no maree.")]
        [SerializeField] float _verticalDeadZone = 1.25f;

        [Tooltip("Desplazamiento fijo respecto al objetivo, en unidades de mundo.")]
        [SerializeField] Vector2 _offset;

        Camera _camera;
        float _velocityX;
        float _velocityY;
        float _zOffset;
        bool _zOffsetReady;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        public float SmoothTime
        {
            get => _smoothTime;
            set => _smoothTime = Mathf.Max(0f, value);
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_target == null)
            {
                GameObject tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null)
                {
                    _target = tagged.transform;
                }
            }
        }

        void Start()
        {
            SnapToTarget();
        }

        void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 self = transform.position;

            // El eje x siempre persigue. El vertical solo cuando el objetivo se sale de
            // la franja, y siempre hacia dentro de ella, para que saltar no mueva el
            // encuadre de arriba abajo.
            self.x = Mathf.SmoothDamp(self.x, _target.position.x + _offset.x,
                ref _velocityX, _smoothTime);

            float centre = self.y;
            float delta = _target.position.y + _offset.y - centre;
            float limit = Mathf.Max(0f, _verticalDeadZone);

            if (Mathf.Abs(delta) > limit)
            {
                float targetY = _target.position.y + _offset.y - Mathf.Sign(delta) * limit;
                self.y = Mathf.SmoothDamp(centre, targetY, ref _velocityY, _smoothTime);
            }

            self.z = _target.position.z + ResolveZOffset();
            transform.position = self;
        }

        /// <summary>Coloca la camara sin interpolacion, util al entrar en juego.</summary>
        public void SnapToTarget()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 self = transform.position;
            self.x = _target.position.x + _offset.x;
            self.y = _target.position.y + _offset.y;
            self.z = _target.position.z + ResolveZOffset();
            transform.position = self;
            _velocityY = 0f;
        }

        /// <summary>
        /// Distancia en z que separa la camara de su objetivo. Se fija una sola vez,
        /// con la posicion con la que se autoro la camara (tipicamente -10), para que
        /// seguir en x/y no la arrastre a la z de los sprites, donde el near clip la
        /// dejaria sin dibujar nada.
        /// </summary>
        float ResolveZOffset()
        {
            if (!_zOffsetReady)
            {
                _zOffset = transform.position.z - (_target != null ? _target.position.z : 0f);
                _zOffsetReady = true;
            }

            return _zOffset;
        }
    }
}