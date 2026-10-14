using PixelIsland.Combat;
using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Disparo del Explorador. Decide el origen y el sentido del proyectil a
    /// partir de la orientacion del sprite, aplica la separacion entre disparos
    /// (<see cref="FireControl"/>) y se lo pide a <see cref="ProjectileSingleton"/>,
    /// que controla las instancias.
    ///
    /// El interruptor <see cref="CanShoot"/> es configurable por escena desde el
    /// Inspector: la primera zona lo lleva apagado y las siguientes encendido.
    /// Se puede disparar en cualquier estado (quieto, andando o saltando) porque
    /// no depende de la maquina de estados; el disparo lo invoca PlayerController
    /// con el flanco de entrada ya muestreado.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/Player/Player Shooter")]
    public sealed class PlayerShooter : MonoBehaviour
    {
        [Header("Habilitacion (configurable por nivel)")]
        [Tooltip("Si esta apagado, este nivel no permite disparar.")]
        [SerializeField] bool _canShoot = true;

        [Header("Disparo")]
        [Tooltip("Segundos de separacion entre disparos.")]
        [SerializeField] float _cooldownSeconds = 2f;

        [Tooltip("Velocidad del proyectil, en unidades por segundo.")]
        [SerializeField] float _projectileSpeed = 14f;

        [Tooltip("Sentido de punteria base. El eje X se refleja segun hacia donde mira el Explorador.")]
        [SerializeField] Vector2 _aimDirection = Vector2.right;

        [Tooltip("Desplazamiento del cañon respecto a los pies del Explorador. La X se refleja al mirar a la izquierda.")]
        [SerializeField] Vector2 _muzzleLocal = new Vector2(0.7f, 1f);

        [Tooltip("Singleton de proyectiles. Vacio = se resuelve por la instancia global.")]
        [SerializeField] ProjectileSingleton _projectiles;

        PlayerController _controller;
        FireControl _fire;

        /// <summary>Control de disparo (recarga + habilitacion). Util para HUD y tests.</summary>
        public FireControl Fire
        {
            get
            {
                if (_fire == null)
                {
                    _fire = new FireControl(_cooldownSeconds, _canShoot);
                }

                return _fire;
            }
        }

        /// <summary>Interruptor de nivel. Se puede cambiar en caliente.</summary>
        public bool CanShoot
        {
            get => Fire.Enabled;
            set => Fire.Enabled = value;
        }

        /// <summary>Segundos que faltan para poder volver a disparar.</summary>
        public float CooldownRemaining => Fire.RemainingCooldown(Time.time);

        public bool IsReadyToFire => Fire.CanFire(Time.time);

        ProjectileSingleton Projectiles =>
            _projectiles != null ? _projectiles : ProjectileSingleton.Instance;

        PlayerController Controller
        {
            get
            {
                if (_controller == null)
                {
                    _controller = GetComponent<PlayerController>();
                }

                return _controller;
            }
        }

        /// <summary>
        /// Intenta disparar si en este paso se pulso el boton. Lo invoca
        /// PlayerController justo despues de leer la entrada.
        /// </summary>
        public bool TryFire(bool firePressed) => firePressed && TryFire();

        /// <summary>Intenta disparar ahora mismo (recarga e interruptor aparte).</summary>
        public bool TryFire()
        {
            if (!Fire.TryConsume(Time.time))
            {
                return false;
            }

            ProjectileSingleton singleton = Projectiles;
            if (singleton == null)
            {
                Debug.LogError("[PlayerShooter] No hay ProjectileSingleton en la escena.");
                return false;
            }

            bool facingLeft = Controller != null && Controller.FacingLeft;
            Vector2 direction = ComputeDirection(_aimDirection, facingLeft);
            Vector2 offset = ComputeMuzzleOffset(_muzzleLocal, facingLeft);

            singleton.Fire((Vector2)transform.position + offset, direction, _projectileSpeed);
            return true;
        }

        /// <summary>
        /// Refleja la punteria base segun la orientacion. Logica pura para tests:
        /// un vector nulo cae a "derecha" para no dejar el disparo sin rumbo.
        /// </summary>
        public static Vector2 ComputeDirection(Vector2 aim, bool facingLeft)
        {
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = Vector2.right;
            }

            return new Vector2(facingLeft ? -Mathf.Abs(aim.x) : Mathf.Abs(aim.x), aim.y).normalized;
        }

        /// <summary>Desplazamiento del cañon, con la X reflejada al mirar a la izquierda.</summary>
        public static Vector2 ComputeMuzzleOffset(Vector2 muzzle, bool facingLeft)
        {
            return new Vector2(facingLeft ? -muzzle.x : muzzle.x, muzzle.y);
        }
    }
}
