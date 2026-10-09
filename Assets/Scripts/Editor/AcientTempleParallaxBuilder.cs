using UnityEditor;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Monta el parallax del templo sobre la escena que este ABIERTA, sin regenerarla:
    /// solo reemplaza los hijos del objeto "Parallax".
    ///
    /// Las seis imagenes de <c>Assets/Art/Environment/Background/AcientTemple</c>
    /// (Plan 1.png..Plan 6.png) son fotogramas completos de 576x324 pensados como
    /// capas. El analisis de alfa y color las ordena de mas lejos a mas cerca: cielo
    /// degradado opaco, bruma alta, silueta oscura, arquitectura, base/terreno y una
    /// franja de tierra del primer plano. La mecanica comun vive en
    /// <see cref="ParallaxZoneBuilder"/>.
    /// </summary>
    public static class AcientTempleParallaxBuilder
    {
        public const string BackgroundFolder = "Assets/Art/Environment/Background/AcientTemple";

        /// <summary>
        /// Una capa por imagen, de mas lejos a mas cerca. El factor es la fraccion del
        /// movimiento de camara que recibe cada capa: 0 la deja pegada a la camara y 1
        /// la hace moverse con el mundo.
        /// </summary>
        static readonly ParallaxZoneBuilder.LayerConfig[] Layers =
        {
            // La 6 es la unica opaca al 100% y con gradiente de luz: es el cielo.
            new ParallaxZoneBuilder.LayerConfig("Plan 6", "BG_TemploCielo", "Cielo degradado, opaco al 100%", -110, 0.05f),

            // La 5 es clara y ocupa solo la mitad de arriba: bruma o nubes altas; la 4
            // es una silueta oscura de todo el fotograma y la 3 la arquitectura del
            // templo; la 2 es la base/terreno y la 1 una franja de tierra del frente.
            new ParallaxZoneBuilder.LayerConfig("Plan 5", "BG_TemploBruma", "Bruma y nubes altas", -100, 0.10f),
            new ParallaxZoneBuilder.LayerConfig("Plan 4", "BG_TemploSilueta", "Silueta oscura del fondo", -90, 0.20f),
            new ParallaxZoneBuilder.LayerConfig("Plan 3", "BG_TemploArquitectura", "Arquitectura principal", -80, 0.35f),
            new ParallaxZoneBuilder.LayerConfig("Plan 2", "BG_TemploBase", "Base y terreno", -70, 0.55f),
            new ParallaxZoneBuilder.LayerConfig("Plan 1", "BG_TemploFrente", "Tierra del primer plano", -60, 0.85f),
        };

        [MenuItem("Tools/Pixel Island/Construir parallax de templo (escena actual)")]
        public static void BuildAcientTempleParallax()
        {
            ParallaxZoneBuilder.Build(BackgroundFolder, "templo", Layers);
        }
    }
}