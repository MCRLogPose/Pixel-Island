namespace PixelIsland.Animation
{
    /// <summary>
    /// Estados de animacion del personaje. El orden NO es arbitrario: es el
    /// valor del parametro int "State" de los AnimatorControllers, y las
    /// condiciones de transicion se generaron con estos indices.
    /// Reordenar este enum rompe los dos controllers a la vez.
    /// </summary>
    public enum CharacterAnimState
    {
        Idle = 0,
        Run = 1,
        Jump = 2,
        Fall = 3
    }
}