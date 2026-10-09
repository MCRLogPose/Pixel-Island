using UnityEditor;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Monta el parallax de la playa sobre la escena que este ABIERTA, sin regenerarla:
    /// solo reemplaza los hijos del objeto "Parallax". Asi la escena del mapa del
    /// usuario se conserva y el fondo se puede reconstruir y revisar en control de
    /// versiones.
    ///
    /// Las seis imagenes de <c>Assets/Art/Environment/Background/Beach</c> son
    /// fotogramas completos de 1920x1080 (salvo el cielo, todas con alfa) pensados
    /// como capas, de mas lejos a mas cerca: cielo, sol, nubes, mar, arena y decoracion
    /// de primer plano. La mecanica comun vive en <see cref="ParallaxZoneBuilder"/>.
    /// </summary>
    public static class BeachParallaxBuilder
    {
        public const string BackgroundFolder = "Assets/Art/Environment/Background/Beach";

        /// <summary>
        /// Una capa por imagen, de mas lejos a mas cerca. El factor es la fraccion del
        /// movimiento de camara que recibe cada capa: 0 la deja pegada a la camara y 1
        /// la hace moverse con el mundo.
        /// </summary>
        static readonly ParallaxZoneBuilder.LayerConfig[] Layers =
        {
            // El cielo es la unica capa opaca al 100%: es el fondo de verdad.
            new ParallaxZoneBuilder.LayerConfig("sky", "BG_Cielo", "Cielo, opaco al 100%", -110, 0.05f),

            // El sol va en el centro de su fotograma: casi pegado al cielo, con un
            // pelin mas de factor para que se intuya la profundidad.
            new ParallaxZoneBuilder.LayerConfig("sun", "BG_Sol", "Sol", -100, 0.10f),

            new ParallaxZoneBuilder.LayerConfig("cloud", "BG_Nubes", "Nubes lejanas", -90, 0.20f),
            new ParallaxZoneBuilder.LayerConfig("sea", "BG_Mar", "Mar", -80, 0.35f),
            new ParallaxZoneBuilder.LayerConfig("land", "BG_Arena", "Arena", -70, 0.55f),
            new ParallaxZoneBuilder.LayerConfig("decor", "BG_Decor", "Decoracion del frente", -60, 0.85f),
        };

        [MenuItem("Tools/Pixel Island/Construir parallax de playa (escena actual)")]
        public static void BuildBeachParallax()
        {
            ParallaxZoneBuilder.Build(BackgroundFolder, "playa", Layers);
        }
    }
}
