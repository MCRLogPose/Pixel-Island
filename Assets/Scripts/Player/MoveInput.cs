namespace PixelIsland.Player
{
    /// <summary>
    /// Fotograma de entrada del jugador. Estructura simple para que el motor
    /// de movimiento sea testeable sin MonoBehaviour ni Input real.
    /// </summary>
    public struct MoveInput
    {
        public float MoveX;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool RunHeld;

        public static MoveInput None => default;

        public MoveInput(float moveX, bool jumpPressed, bool jumpHeld, bool runHeld)
        {
            MoveX = moveX;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            RunHeld = runHeld;
        }
    }
}
