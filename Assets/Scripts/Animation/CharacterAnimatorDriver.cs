using PixelIsland.Player;
using UnityEngine;

namespace PixelIsland.Animation
{
    /// <summary>
    /// Unico puente entre la maquina de estados del Player y el Animator.
    ///
    /// Solo lee <see cref="PlayerController.CurrentStateName"/> y escribe el
    /// parametro int "State" que ambos AnimatorControllers comparten. No decide
    /// animaciones: la logica de estados vive en el Player y la logica de
    /// fotogramas en el controller, aqui solo se copia el valor de un lado a
    /// otro. Por eso cambiar de personaje es reemplazar el controller en el
    /// Animator, sin tocar codigo.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterAnimatorDriver : MonoBehaviour
    {
        [Tooltip("Player cuya maquina de estados se refleja. Se resuelve solo si esta vacio.")]
        [SerializeField] PlayerController _player;

        [Tooltip("Parametro int del AnimatorController. Debe coincidir en Character1 y Character2.")]
        [SerializeField] string _stateParameter = "State";

        /// <summary>Ultimo estado enviado al Animator, para no escribirlo cada frame.</summary>
        int _lastSent = int.MinValue;

        Animator _animator;

        /// <summary>Parametro int del AnimatorController.</summary>
        public string StateParameter => _stateParameter;

        void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_player == null)
            {
                _player = GetComponent<PlayerController>();
            }
        }

        void OnEnable()
        {
            // Fuerza el envio en el primer frame aunque el estado no haya cambiado.
            _lastSent = int.MinValue;
        }

        void LateUpdate()
        {
            Sync();
        }

        /// <summary>
        /// Copia el estado actual del Player al parametro del Animator. Es
        /// idempotente: solo escribe cuando el estado ha cambiado, y avisa por
        /// consola si el Player tiene un estado que el controller no conoce.
        /// </summary>
        public void Sync()
        {
            // Sin controller no hay nada que animar: se sale antes de escribir,
            // para no llenar la consola con avisos de un prefab sin configurar.
            if (_animator == null || _player == null ||
                _animator.runtimeAnimatorController == null)
            {
                return;
            }

            if (!CharacterAnimMapper.TryMap(_player.CurrentStateName, out CharacterAnimState state))
            {
                Debug.LogWarning("[Animation] Estado sin animacion: " + _player.CurrentStateName);
                return;
            }

            int value = (int)state;

            if (value == _lastSent)
            {
                return;
            }

            _lastSent = value;
            _animator.SetInteger(_stateParameter, value);
        }
    }
}