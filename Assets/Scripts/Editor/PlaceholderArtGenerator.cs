using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Genera arte placeholder por codigo para poder testear TODAS las mecanicas
    /// antes de tener los sprites finales. Se ejecuta con:
    ///   Tools > Pixel Island > Regenerar arte placeholder
    ///
    /// Cuando lleguen los sprites reales hay que:
    ///   1. dejarlos en Assets/Art/... con los nombres definitivos,
    ///   2. borrarlos en este generador,
    ///   3. reasignar el campo Sprite del prefab correspondiente.
    /// Los scripts no dependen de estas imagenes: solo de los prefabs.
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        public const int PPU = 32;

        [MenuItem("Tools/Pixel Island/Regenerar arte placeholder")]
        public static void GenerateAll()
        {
            EnsureFolder("Assets/Art/Characters");
            EnsureFolder("Assets/Art/Environment");
            EnsureFolder("Assets/Art/Collectibles");
            EnsureFolder("Assets/Art/Projectiles");

            GenerateExplorer();
            GenerateSlime();
            GenerateGroundTile();
            GenerateRock();
            GenerateSpikes();
            GenerateCoin();
            GenerateBullet();

            AssetDatabase.Refresh();
            Debug.Log("[PlaceholderArt] Arte placeholder regenerado en Assets/Art/.");
        }

        // ---------------------------------------------------------------
        // Sprites
        // ---------------------------------------------------------------

        static void GenerateExplorer()
        {
            int w = 32, h = 64;
            Color32[] px = Blank(w, h, new Color32(0, 0, 0, 0));

            Color32 skin = new Color32(240, 200, 160, 255);
            Color32 skinShade = new Color32(200, 156, 118, 255);
            Color32 hat = new Color32(150, 100, 55, 255);
            Color32 hatDark = new Color32(110, 72, 38, 255);
            Color32 shirt = new Color32(48, 148, 132, 255);
            Color32 shirtDark = new Color32(30, 104, 94, 255);
            Color32 pants = new Color32(58, 70, 120, 255);
            Color32 boot = new Color32(78, 52, 32, 255);
            Color32 eye = new Color32(30, 30, 40, 255);

            // Botas
            Rect(px, w, 8, 0, 8, 12, boot);
            Rect(px, w, 20, 0, 8, 12, boot);
            // Piernas
            Rect(px, w, 9, 8, 7, 18, pants);
            Rect(px, w, 19, 8, 7, 18, pants);
            // Torso
            Rect(px, w, 9, 26, 18, 20, shirt);
            Rect(px, w, 9, 26, 18, 3, shirtDark);
            // Brazos
            Rect(px, w, 5, 28, 4, 16, skin);
            Rect(px, w, 27, 28, 4, 16, skin);
            // Cabeza
            Rect(px, w, 10, 46, 16, 12, skin);
            Rect(px, w, 10, 46, 16, 2, skinShade);
            // Ojos
            Rect(px, w, 13, 53, 3, 3, eye);
            Rect(px, w, 20, 53, 3, 3, eye);
            // Sombrero
            Rect(px, w, 6, 58, 24, 3, hat);
            Rect(px, w, 13, 61, 10, 3, hat);
            Rect(px, w, 13, 61, 10, 1, hatDark);
            // Contorno inferior del torso
            Rect(px, w, 9, 26, 18, 1, shirtDark);

            Save("Assets/Art/Characters/explorer_placeholder.png", px, w, h,
                new Vector2(0.5f, 0f), "Explorer placeholder (32x64 px, 1x2 unidades)");
        }

        static void GenerateSlime()
        {
            int w = 32, h = 32;
            Color32[] px = Blank(w, h, new Color32(0, 0, 0, 0));

            Color32 body = new Color32(120, 200, 90, 255);
            Color32 bodyDark = new Color32(80, 150, 60, 255);
            Color32 eye = new Color32(25, 35, 25, 255);
            Color32 gloss = new Color32(210, 240, 190, 255);

            Rect(px, w, 2, 0, 28, 22, body);
            Rect(px, w, 6, 22, 20, 4, body);
            Rect(px, w, 12, 26, 8, 3, body);
            Rect(px, w, 2, 0, 28, 3, bodyDark);
            Rect(px, w, 8, 18, 5, 5, eye);
            Rect(px, w, 19, 18, 5, 5, eye);
            Rect(px, w, 6, 8, 6, 5, gloss);

            Save("Assets/Art/Characters/enemy_slime_placeholder.png", px, w, h,
                new Vector2(0.5f, 0f), "Slime placeholder (32x32 px)");
        }

        static void GenerateGroundTile()
        {
            int w = 32, h = 32;
            Color32[] px = Blank(w, h, new Color32(0, 0, 0, 255));

            Color32 sand = new Color32(226, 200, 130, 255);
            Color32 sandDark = new Color32(198, 170, 105, 255);
            Color32 grass = new Color32(106, 176, 84, 255);
            Color32 grassDark = new Color32(78, 140, 62, 255);

            Rect(px, w, 0, 0, 32, 22, sand);
            Rect(px, w, 0, 0, 32, 3, sandDark);
            Rect(px, w, 0, 22, 32, 10, grass);
            Rect(px, w, 0, 22, 32, 2, grassDark);
            Rect(px, w, 4, 26, 5, 4, grassDark);
            Rect(px, w, 19, 25, 7, 5, grassDark);
            Rect(px, w, 10, 10, 6, 4, sandDark);

            Save("Assets/Art/Environment/ground_placeholder.png", px, w, h,
                new Vector2(0f, 0f), "Suelo placeholder (32x32 px, tile)");
        }

        static void GenerateRock()
        {
            int w = 32, h = 32;
            Color32[] px = Blank(w, h, new Color32(0, 0, 0, 0));

            Color32 stone = new Color32(130, 130, 140, 255);
            Color32 stoneDark = new Color32(92, 92, 102, 255);
            Color32 stoneLight = new Color32(168, 168, 178, 255);

            Rect(px, w, 3, 2, 26, 16, stone);
            Rect(px, w, 7, 18, 18, 8, stone);
            Rect(px, w, 3, 2, 26, 4, stoneDark);
            Rect(px, w, 7, 18, 8, 4, stoneLight);

            Save("Assets/Art/Environment/rock_placeholder.png", px, w, h,
                new Vector2(0.5f, 0f), "Roca placeholder (32x32 px)");
        }

        static void GenerateSpikes()
        {
            int w = 32, h = 32;
            Color32[] px = Blank(w, h, new Color32(0, 0, 0, 0));

            Color32 steel = new Color32(198, 204, 214, 255);
            Color32 steelDark = new Color32(126, 132, 142, 255);

            // Tres triangulos de 10 px de base y 20 px de alto.
            for (int i = 0; i < 3; i++)
            {
                int centerX = 6 + i * 10;
                for (int y = 0; y < 20; y++)
                {
                    float t = (19f - y) / 19f;
                    int half = Mathf.RoundToInt((1f - t) * 5f);
                    Rect(px, w, centerX - half, y, half * 2 + 1, 1, y > 14 ? steelDark : steel);
                }
            }

            Rect(px, w, 0, 0, 32, 3, steelDark);

            Save("Assets/Art/Environment/spikes_placeholder.png", px, w, h,
                new Vector2(0.5f, 0f), "Espinas placeholder (32x32 px, hazard)");
        }

        static void GenerateCoin()
        {
            int w = 16, h = 16;
            Color32[] px = Blank(w, h, new Color32(0, 0, 0, 0));

            Color32 gold = new Color32(246, 200, 60, 255);
            Color32 goldDark = new Color32(196, 148, 30, 255);
            Color32 shine = new Color32(255, 244, 190, 255);

            float cx = 7.5f, cy = 7.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d > 7.4f)
                    {
                        continue;
                    }

                    Color32 c = d > 5.6f ? goldDark : gold;
                    if (d < 2.2f && x < 6 && y > 8)
                    {
                        c = shine;
                    }

                    Set(px, w, h, x, y, c);
                }
            }

            Save("Assets/Art/Collectibles/coin_placeholder.png", px, w, h,
                new Vector2(0.5f, 0.5f), "Moneda placeholder (16x16 px)");
        }

        static void GenerateBullet()
        {
            int w = 8, h = 8;
            Color32[] px = Blank(w, h, new Color32(0, 0, 0, 0));

            Color32 core = new Color32(255, 236, 150, 255);
            Color32 edge = new Color32(240, 170, 50, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - 3.5f;
                    float dy = y - 3.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 3.6f)
                    {
                        continue;
                    }

                    Set(px, w, h, x, y, d > 2.2f ? edge : core);
                }
            }

            Save("Assets/Art/Projectiles/bullet_placeholder.png", px, w, h,
                new Vector2(0.5f, 0.5f), "Bala placeholder (8x8 px)");
        }

        // ---------------------------------------------------------------
        // Utilidades de dibujo
        // ---------------------------------------------------------------

        static Color32[] Blank(int width, int height, Color32 fill)
        {
            Color32[] px = new Color32[width * height];
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = fill;
            }

            return px;
        }

        static void Rect(Color32[] px, int width, int x, int y, int w, int h, Color32 color)
        {
            int height = px.Length / width;
            for (int py = y; py < y + h; py++)
            {
                for (int pxi = x; pxi < x + w; pxi++)
                {
                    Set(px, width, height, pxi, py, color);
                }
            }
        }

        static void Set(Color32[] px, int width, int height, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            px[y * width + x] = color;
        }

        // ---------------------------------------------------------------
        // Guardado e importacion
        // ---------------------------------------------------------------

        static void Save(string assetPath, Color32[] px, int width, int height, Vector2 pivot, string label)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (directory != null)
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(px);
            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            ConfigureImporter(assetPath, pivot);

            Debug.Log("[PlaceholderArt] " + label + " -> " + assetPath);
        }

        /// <summary>
        /// Ajustes de importacion obligatorios para pixel art: 32 px por unidad,
        /// filtro Point (sin blur), sin compresion y sin mipmaps.
        /// </summary>
        public static void ConfigureImporter(string assetPath, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[PlaceholderArt] No se encontro TextureImporter para " + assetPath);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PPU;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;

            // FullRect: es lo estandar en 2D y ademas es lo que exige
            // SpriteDrawMode.Tiled en los suelos (si no, Unity avisa por consola).
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
