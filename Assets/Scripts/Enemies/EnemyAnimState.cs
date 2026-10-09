namespace PixelIsland.Enemies
{
    /// <summary>
    /// Estados de animacion del enemigo. El orden NO es arbitrario: es el valor
    /// del parametro int "State" del AnimatorController del enemigo, y las
    /// condiciones de transicion se generan con estos indices.
    ///
    /// A diferencia del personaje, el enemigo no tiene clip de caminar (su arte
    /// solo trae Idle, Jump y Fall), asi que patrullar en el suelo usa Idle.
    /// Reordenar este enum rompe el controller.
    /// </summary>
    public enum EnemyAnimState
    {
        Idle = 0,
        Jump = 1,
        Fall = 2
    }
}
