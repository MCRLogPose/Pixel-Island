using PixelIsland.Player;
using UnityEngine;

namespace PixelIsland.Tests.PlayMode
{
    /// <summary>
    /// Fuente de entrada programada para los tests de PlayMode. Permite verificar
    /// saltos y carreras dentro del bucle de fisica real sin simular teclado.
    /// Vive en el assembly de tests, no en el codigo de juego.
    /// </summary>
    public sealed class FakePlayerInput : MonoBehaviour, IPlayerInputSource
    {
        public float MoveX;
        public bool JumpHeld;
        public bool RunHeld;

        /// <summary>Se consume en el siguiente paso fisico.</summary>
        public bool JumpOnce;

        /// <summary>Se consume en el siguiente paso fisico.</summary>
        public bool FireOnce;

        public MoveInput ReadInput()
        {
            MoveInput input = new MoveInput(MoveX, JumpOnce, JumpHeld, RunHeld, FireOnce);
            JumpOnce = false;
            FireOnce = false;
            return input;
        }

        public void PressJump()
        {
            JumpOnce = true;
            JumpHeld = true;
        }

        public void PressFire()
        {
            FireOnce = true;
        }
    }
}
