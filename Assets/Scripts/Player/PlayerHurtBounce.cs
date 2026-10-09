using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Rebote de dolor: cuando un hazard resta vidas de verdad, el Explorador sale
    /// despedido hacia arriba y en sentido contrario al golpe, perdiendo el control
    /// unos instantes para simular que le ha dolido. Implementa
    /// <see cref="IDamageReaction"/> para que el hazard solo tenga que avisar "te he
    /// hecho dano desde aqui" y no conocer nada del controlador del jugador.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [AddComponentMenu("Pixel Island/Player/Player Hurt Bounce")]
    public sealed class PlayerHurtBounce : MonoBehaviour, IDamageReaction
    {
        [Tooltip("Altura del brinco de dolor, en unidades.")]
        [SerializeField] float _bounceHeight = 1.5f;

        [Tooltip("Empuje horizontal al salir despedido, en unidades por segundo.")]
        [SerializeField] float _horizontalSpeed = 4f;

        [Tooltip("Segundos sin control tras el golpe.")]
        [SerializeField] float _lockSeconds = 0.18f;

        [Tooltip("Distancia lateral por debajo de la cual el golpe cuenta como centrado.")]
        [SerializeField] float _lateralEpsilon = 0.05f;

        PlayerController _controller;

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

        public void OnDamaged(Vector2 sourcePosition)
        {
            PlayerController controller = Controller;
            if (controller == null)
            {
                return;
            }

            float vertical = JumpMath.VelocityForHeight(_bounceHeight, controller.Movement.Gravity);
            Vector2 impulse = ComputeImpulse(
                controller.Velocity,
                transform.position.x,
                sourcePosition.x,
                controller.FacingLeft,
                _horizontalSpeed,
                vertical,
                _lateralEpsilon);

            controller.ApplyImpulse(impulse, _lockSeconds);
        }

        /// <summary>
        /// Decide el vector del rebote. Logica pura para testearla en EditMode: el
        /// lateral sale del golpe; si fue centrado, del sentido del movimiento
        /// (rebote hacia atras) o, en reposo, de la orientacion del sprite.
        /// </summary>
        public static Vector2 ComputeImpulse(
            Vector2 playerVelocity,
            float playerX,
            float sourceX,
            bool facingLeft,
            float horizontalSpeed,
            float verticalSpeed,
            float lateralEpsilon)
        {
            float lateral = playerX - sourceX;

            float direction;
            if (Mathf.Abs(lateral) > lateralEpsilon)
            {
                direction = Mathf.Sign(lateral);
            }
            else if (Mathf.Abs(playerVelocity.x) > lateralEpsilon)
            {
                direction = -Mathf.Sign(playerVelocity.x);
            }
            else
            {
                direction = facingLeft ? 1f : -1f;
            }

            return new Vector2(direction * horizontalSpeed, verticalSpeed);
        }
    }
}
