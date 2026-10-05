namespace PixelIsland.Core
{
    /// <summary>
    /// Zonas de la isla, en orden de progresion. Cada zona es un nivel (Etapa 8).
    /// El orden del enum define el orden de desbloqueo.
    /// </summary>
    public enum ZoneId
    {
        Beach = 0,
        Jungle = 1,
        Cave = 2,
        Temple = 3
    }

    public static class ZoneIdExtensions
    {
        public static int Count => 4;

        public static string DisplayName(this ZoneId zone)
        {
            switch (zone)
            {
                case ZoneId.Beach: return "Playa";
                case ZoneId.Jungle: return "Selva";
                case ZoneId.Cave: return "Cueva";
                case ZoneId.Temple: return "Templo";
                default: return zone.ToString();
            }
        }

        public static ZoneId Next(this ZoneId zone)
        {
            return zone >= ZoneId.Temple ? ZoneId.Temple : zone + 1;
        }
    }
}
