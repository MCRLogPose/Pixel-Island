using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Lee el Input Manager legado (el proyecto NO tiene el Input System
    /// instalado, ver AGENTS.md) y lo expone como <see cref="MoveInput"/>.
    ///
    /// Update muestrea los flancos (GetKeyDown) y el paso fisico los consume
    /// mediante <see cref="ReadInput"/>. El latch de JumpPressed evita que un
    /// pulso de salto se pierda cuando hay varios pasos fisicos en un mismo
    /// fotograma de render; a partir de ahi la tolerancia temporal la aporta el
    /// jump buffer del PlayerMotor.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputSource
    {
        [Header("Movimiento")]
        [SerializeField] KeyCode _leftKey = KeyCode.A;
        [SerializeField] KeyCode _leftAltKey = KeyCode.LeftArrow;
        [SerializeField] KeyCode _rightKey = KeyCode.D;
        [SerializeField] KeyCode _rightAltKey = KeyCode.RightArrow;

        [Header("Acciones")]
        [SerializeField] KeyCode _jumpKey = KeyCode.Space;
        [SerializeField] KeyCode _runKey = KeyCode.LeftShift;
        [SerializeField] KeyCode _runAltKey = KeyCode.RightShift;

        /// <summary>Ultimo fotograma muestreado. Public para inspeccion y depuracion.</summary>
        public MoveInput Sample;

        bool _jumpLatch;

        void Update()
        {
            float moveX = 0f;
            if (Input.GetKey(_leftKey) || Input.GetKey(_leftAltKey))
            {
                moveX -= 1f;
            }

            if (Input.GetKey(_rightKey) || Input.GetKey(_rightAltKey))
            {
                moveX += 1f;
            }

            if (Input.GetKeyDown(_jumpKey))
            {
                _jumpLatch = true;
            }

            Sample = new MoveInput(
                moveX,
                _jumpLatch,
                Input.GetKey(_jumpKey),
                Input.GetKey(_runKey) || Input.GetKey(_runAltKey));
        }

        /// <summary>
        /// Entrada del paso fisico actual. Cierra el latch de salto una vez
        /// entregado: el PlayerMotor ya lo ha copiado a su jump buffer.
        /// </summary>
        public MoveInput ReadInput()
        {
            // MoveInput es un struct: hay que COPIAR el valor antes de limpiar el
            // latch. Si se limpia Sample antes de devolverlo, el motor recibe
            // siempre JumpPressed = false y el salto nunca llega a dispararse.
            MoveInput input = Sample;

            _jumpLatch = false;
            Sample.JumpPressed = false;

            return input;
        }
    }
}
