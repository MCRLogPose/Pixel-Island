namespace PixelIsland.Player
{
    /// <summary>
    /// Fuente de entrada del Explorador. Existe para que el PlayerController no
    /// dependa del teclado: los tests de PlayMode inyectan una fuente falsa y
    /// pueden verificar saltos y carreras sin simular pulsaciones reales.
    /// </summary>
    public interface IPlayerInputSource
    {
        /// <summary>
        /// Entrada correspondiente al paso fisico en curso. Se invoca una sola
        /// vez por paso, desde PlayerController.FixedUpdate.
        /// </summary>
        MoveInput ReadInput();
    }
}
