using UnityEditor;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Monta el parallax de la cueva sobre la escena que este ABIERTA, sin regenerarla:
    /// solo reemplaza los hijos del objeto "Parallax".
    ///
    /// Las seis imagenes de <c>Assets/Art/Environment/Background/Cave</c> (1.png..6.png)
    /// son fotogramas completos de 1920x1080 pensados como capas, de mas lejos a mas
    /// cerca: fondo opaco, muro y techo, estalactitas, suelo, rocas y detalles del primer
    /// plano. La mecanica comun vive en <see cref="ParallaxZoneBuilder"/>.
    /// </summary>
    public static class CaveParallaxBuilder
    {
        public const string BackgroundFolder = "Assets/Art/Environment/Background/Cave";

        /// <summary>
        /// Una capa por imagen, de mas lejos a mas cerca. El factor es la fraccion del
        /// movimiento de camara que recibe cada capa: 0 la deja pegada a la camara y 1
        /// la hace moverse con el mundo.
        /// </summary>
        static readonly ParallaxZoneBuilder.LayerConfig[] Layers =
        {
            // La imagen 1 es la unica opaca al 100%: es el fondo de verdad.
            new ParallaxZoneBuilder.LayerConfig("1", "BG_CuevaFondo", "Fondo de cueva, opaco al 100%", -110, 0.05f),

            // La 2 cubre casi toda la pantalla (muro) con huecos arriba; la 3 son las
            // estalactitas del techo, la 4 el suelo, la 5 las rocas y la 6 el detalle.
            new ParallaxZoneBuilder.LayerConfig("2", "BG_MuroCueva", "Muro y techo de la cueva", -100, 0.10f),
            new ParallaxZoneBuilder.LayerConfig("3", "BG_Estalactitas", "Estalactitas del techo", -90, 0.20f),
            new ParallaxZoneBuilder.LayerConfig("4", "BG_Suelo", "Suelo de la cueva", -80, 0.35f),
            new ParallaxZoneBuilder.LayerConfig("5", "BG_Rocas", "Rocas del frente", -70, 0.55f),
            new ParallaxZoneBuilder.LayerConfig("6", "BG_Detalles", "Detalles del primer plano", -60, 0.85f),
        };

        [MenuItem("Tools/Pixel Island/Construir parallax de cueva (escena actual)")]
        public static void BuildCaveParallax()
        {
            ParallaxZoneBuilder.Build(BackgroundFolder, "cueva", Layers);
        }
    }
}
