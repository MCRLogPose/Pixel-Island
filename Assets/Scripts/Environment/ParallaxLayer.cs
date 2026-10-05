using UnityEngine;

namespace PixelIsland.Environment
{
    /// <summary>
    /// Capa de fondo con desplazamiento parallax.
    ///
    /// La capa sigue a la camara pero arrastra su propia posicion una fraccion de
    /// lo que la camara se desplaza, y al llegar al borde de la imagen reaparece por
    /// el lado opuesto. Asi el fondo es infinito sin depender de zoneId ni de
    /// scripting assembly: solo hace falta un factor por capa.
    ///
    /// Convencion de <see cref="Factor"/>:
    ///   0 -> la capa esta clavada a la camara, nunca se mueve (cielo, infinito lejos)
    ///   1 -> la capa se mueve a la velocidad del mundo (no se ve desplazarse)
    ///
    /// Solo se mueve en tiempo de ejecucion: en edicion las capas se colocan a mano
    /// y asi la escena no se marca como sucia en cada fotograma.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Pixel Island/Environment/Parallax Layer")]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = pegada a la camara (infinito lejos). " +
                 "1 = a la velocidad del mundo (no se desplaza respecto al terreno).")]
        [Range(0f, 1f)]
        [SerializeField] float _factor = 0.5f;

        [Tooltip("Factor vertical. 0 = la capa se queda fija al subir o bajar la camara.")]
        [Range(0f, 1f)]
        [SerializeField] float _verticalFactor;

        [Tooltip("Repite la imagen en horizontal para que el fondo sea infinito. " +
                 "Desactivalo si la imagen ya cubre toda la pantalla.")]
        [SerializeField] bool _wrapHorizontal = true;

        [Tooltip("Camara a la que sigue la capa. Vacia = Camera.main.")]
        [SerializeField] Camera _targetCamera;

        SpriteRenderer _renderer;
        Vector2 _scroll;
        Vector3 _lastCameraPosition;
        Vector2 _lastViewSize;
        bool _initialised;

        /// <summary>
        /// Renderer de la capa. Se resuelve de forma perezosa porque Awake no corre
        /// al abrir una escena en edicion, y sin esto la propiedad seria nula justo
        /// cuando mas hace falta: revisar y automatizar la escena guardada.
        /// </summary>
        public SpriteRenderer Renderer
        {
            get
            {
                if (_renderer == null)
                {
                    _renderer = GetComponent<SpriteRenderer>();
                }

                return _renderer;
            }
        }

        /// <summary>Fraccion del movimiento de camara que recibe la capa.</summary>
        public float Factor
        {
            get => _factor;
            set => _factor = Mathf.Clamp01(value);
        }

        public float VerticalFactor
        {
            get => _verticalFactor;
            set => _verticalFactor = Mathf.Clamp01(value);
        }

        public Camera TargetCamera
        {
            get => _targetCamera;
            set
            {
                _targetCamera = value;
                _initialised = false;
            }
        }

        /// <summary>Desplazamiento acumulado sin normalizar, util para depurar.</summary>
        public Vector2 Scroll => _scroll;

        /// <summary>
        /// Tamanio de una repeticion de la imagen en unidades de mundo, ya corregido
        /// por la escala del transform. Es el valor contra el que se envuelve el
        /// desplazamiento, asi que tiene que estar en unidades de mundo de verdad.
        /// </summary>
        public Vector2 TileSize
        {
            get
            {
                SpriteRenderer renderer = Renderer;
                if (renderer == null || renderer.sprite == null)
                {
                    return Vector2.zero;
                }

                Vector3 scale = transform.lossyScale;
                Vector2 local = renderer.sprite.bounds.size;
                return new Vector2(local.x * Mathf.Abs(scale.x), local.y * Mathf.Abs(scale.y));
            }
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            // El quad se dimensiona tambien en runtime: la escena se construye con la
            // relacion de aspecto de entonces y la ventana puede venir de otra, mas
            // ancha o mas alta. Sin esto se veria el borde del tile.
            Camera cam = ResolveCamera();
            if (cam != null)
            {
                RefreshQuad(cam);
                _lastViewSize = ViewSize(cam);
            }
        }

        void LateUpdate()
        {
            Camera cam = ResolveCamera();
            if (cam == null)
            {
                return;
            }

            Vector3 camPosition = cam.transform.position;

            // Redimensionar en cada cambio de vista: al redimensionar la ventana, al
            // cambiar la referencia de pixel perfect o al rotar el juego.
            Vector2 viewSize = ViewSize(cam);
            if (viewSize != _lastViewSize)
            {
                RefreshQuad(cam);
                _lastViewSize = viewSize;
            }

            // El primer fotograma solo ancla la referencia: sin delta acumulado, para
            // que entrar en juego no de un tirón que descuadre todo el fondo.
            if (!_initialised)
            {
                _initialised = true;
                _lastCameraPosition = camPosition;
                AlignTo(cam, camPosition);
                return;
            }

            Vector3 delta = camPosition - _lastCameraPosition;
            _lastCameraPosition = camPosition;

            _scroll.x = AccumulateScroll(_scroll.x, delta.x, _factor);
            _scroll.y = AccumulateScroll(_scroll.y, delta.y, _verticalFactor);

            AlignTo(cam, camPosition);
        }

        /// <summary>
        /// Recoloca la capa y dimensiona su quad para cubrir la vista de la camara
        /// mas el margen necesario para que el envoltado no deje huecos.
        /// </summary>
        public void AlignTo(Camera cam, Vector3 camPosition)
        {
            if (cam == null)
            {
                return;
            }

            Vector2 tile = TileSize;

            float x = camPosition.x;
            if (_wrapHorizontal && tile.x > 0f)
            {
                x += WrapScroll(_scroll.x, tile.x);
            }

            float y = camPosition.y;
            if (_verticalFactor > 0f && tile.y > 0f)
            {
                y += WrapScroll(_scroll.y, tile.y);
            }

            transform.position = new Vector3(x, y, transform.position.z);
        }

/// <summary>
        /// Tamano de la vista de la camara en unidades de mundo.
        /// </summary>
        public static Vector2 ViewSize(Camera cam)
        {
            if (cam == null)
            {
                return Vector2.zero;
            }

            float height = cam.orthographicSize * 2f;
            return new Vector2(height * cam.aspect, height);
        }

        /// <summary>
        /// Recoloca la capa y dimensiona su quad para cubrir la vista de la camara
        /// mas el margen necesario para que el envoltado no deje huecos.
        /// </summary>
        public void RefreshQuad(Camera cam)
        {
            SpriteRenderer renderer = Renderer;
            if (cam == null || renderer == null || renderer.sprite == null)
            {
                return;
            }

            Vector2 tile = TileSize;
            Vector2 view = ViewSize(cam);
            float viewHeight = view.y;
            float viewWidth = view.x;

            float worldWidth;
            if (_wrapHorizontal && tile.x > 0f)
            {
                worldWidth = Mathf.Ceil((viewWidth + tile.x) / tile.x) * tile.x;
            }
            else
            {
                worldWidth = Mathf.Max(tile.x, viewWidth);
            }

            float worldHeight = Mathf.Max(tile.y, viewHeight);

            if (viewHeight > tile.y && _verticalFactor <= 0f)
            {
                Debug.LogWarning("[Parallax] La vista de la camara (" + viewHeight.ToString("0.##") +
                                 "u) es mas alta que la imagen (" + tile.y.ToString("0.##") +
                                 "u) en '" + name + "'. Habra que repetirla en vertical, " +
                                 "y eso duplica el motivo en bandas horizontales.", this);
            }

            // SpriteRenderer.size va en unidades locales, asi que hay que deshacer la
            // escala del transform: el quad se dimensiona en mundo y luego se pasa a local.
            Vector3 scale = transform.lossyScale;
            float sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
            float sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));

            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(worldWidth / sx, worldHeight / sy);
        }

        /// <summary>Reparte desde cero el desplazamiento acumulado.</summary>
        public void ResetScroll()
        {
            _scroll = Vector2.zero;
            _initialised = false;
        }

        public Camera ResolveCamera()
        {
            if (_targetCamera != null)
            {
                return _targetCamera;
            }

            _targetCamera = Camera.main;
            return _targetCamera;
        }

        /// <summary>
        /// Anade el desplazamiento de la camara al acumulado de la capa.
        /// Estatico y sin estado para poder verificarlo en tests de EditMode.
        /// </summary>
        public static float AccumulateScroll(float scroll, float cameraDelta, float factor)
        {
            return scroll + cameraDelta * Mathf.Clamp01(factor);
        }

        /// <summary>
        /// Envolve el desplazamiento al intervalo [-tile/2, +tile/2) para que la
        /// imagen reaparezca por el lado opuesto en vez de escaparse.
        /// Estatico y sin estado para poder verificarlo en tests de EditMode.
        /// </summary>
        public static float WrapScroll(float scroll, float tileWidth)
        {
            if (tileWidth <= 0f)
            {
                return 0f;
            }

            return Mathf.Repeat(scroll + tileWidth * 0.5f, tileWidth) - tileWidth * 0.5f;
        }
    }
}