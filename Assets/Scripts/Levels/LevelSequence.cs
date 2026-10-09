namespace PixelIsland.Levels
{
    /// <summary>
    /// Orden de las zonas del juego. La progresion es una sola lista aqui para que no
    /// haya que repetir el "siguiente nivel" en cada escena y para poder probarla sin
    /// cargar escenas.
    /// </summary>
    public static class LevelSequence
    {
        static readonly string[] Order =
        {
            "BeachZone",
            "ForestZone",
            "CaveZone",
            "AcienttempleZone",
        };

        public static string[] Scenes => Order;

        /// <summary>Indice de una zona en el orden, o -1 si no pertenece a la secuencia.</summary>
        public static int IndexOf(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return -1;
            }

            for (int i = 0; i < Order.Length; i++)
            {
                if (Order[i] == sceneName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Escena siguiente a la dada, o null si es la ultima (o desconocida).</summary>
        public static string Next(string sceneName)
        {
            int index = IndexOf(sceneName);
            return index >= 0 && index + 1 < Order.Length ? Order[index + 1] : null;
        }

        public static bool IsLast(string sceneName) => IndexOf(sceneName) == Order.Length - 1;
    }
}
