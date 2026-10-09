using System.Collections.Generic;
using PixelIsland.Core;
using PixelIsland.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Nucleo compartido para montar el parallax de una zona sobre la escena ABIERTA,
    /// sin regenerarla: solo reemplaza los hijos del objeto "Parallax". Asi la escena
    /// del mapa del usuario se conserva y el fondo se puede reconstruir y revisar en
    /// control de versiones.
    ///
    /// Cada builder de zona (por ejemplo <see cref="BeachParallaxBuilder"/> o
    /// <see cref="CaveParallaxBuilder"/>) solo aporta la carpeta de imagenes y la lista
    /// de capas; este tipo hace el resto.
    /// </summary>
    public static class ParallaxZoneBuilder
    {
        public const string ParallaxRootName = "Parallax";

        /// <summary>
        /// Pixeles por unidad para que una imagen de 1920x1080 mida 20 x 11.25 u, el
        /// mismo tamano de mundo que el resto de fondos (las imagenes de bosque son
        /// 320x180 a 16 PPU). Asi cada capa cubre la vista sin repetirse en vertical.
        /// </summary>
        const float SpritePpu = 96f;

        /// <summary>Una capa de parallax: que imagen usa y como se coloca.</summary>
        public struct LayerConfig
        {
            public string FileName;
            public string ObjectName;
            public string Role;
            public int SortingOrder;
            public float Factor;

            public LayerConfig(string fileName, string objectName, string role,
                int sortingOrder, float factor)
            {
                FileName = fileName;
                ObjectName = objectName;
                Role = role;
                SortingOrder = sortingOrder;
                Factor = factor;
            }
        }

        /// <summary>
        /// Monta <paramref name="layers"/> (de mas lejos a mas cerca) usando las imagenes
        /// de <paramref name="backgroundFolder"/>. Corrige el import de cada PNG y
        /// reescala las capas al alto de vista actual de la camara.
        /// </summary>
        public static void Build(string backgroundFolder, string zoneLabel, LayerConfig[] layers)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[ParallaxZone] No hay escena abierta que modificar.");
                return;
            }

            // Importar antes de cargar: el tamano de mundo del sprite depende del PPU.
            foreach (LayerConfig layer in layers)
            {
                EnsureImport(backgroundFolder, layer.FileName);
            }

            var sprites = new Sprite[layers.Length];
            var missing = new List<string>();

            for (int i = 0; i < layers.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                    backgroundFolder + "/" + layers[i].FileName + ".png");

                if (sprites[i] == null)
                {
                    missing.Add(layers[i].FileName);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogError("[ParallaxZone] Faltan imagenes de fondo: " + string.Join(", ", missing) +
                               ". Se esperan en " + backgroundFolder + ".");
                return;
            }

            Camera camera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("[ParallaxZone] No hay camara en la escena: las capas se " +
                                 "colocaran en el origen y seguiran a Camera.main en runtime.");
            }

            GameObject root = GameObject.Find(ParallaxRootName);
            if (root == null)
            {
                root = new GameObject(ParallaxRootName);
            }

            RemoveExistingLayers(root.transform);

            for (int i = 0; i < layers.Length; i++)
            {
                BuildLayer(root.transform, layers[i], sprites[i], camera);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ParallaxZone] Escena '" + scene.name + "' con " + layers.Length +
                      " capas de parallax de " + zoneLabel + ". Camina para ver como se separan.");
        }

        /// <summary>Quita las capas previas (por ejemplo, las de otra zona) sin tocar el resto de la escena.</summary>
        static void RemoveExistingLayers(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (child.GetComponent<ParallaxLayer>() != null)
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        static void BuildLayer(Transform parent, LayerConfig config, Sprite sprite, Camera camera)
        {
            GameObject go = new GameObject(config.ObjectName);
            go.layer = GameLayers.Id(GameLayers.ParallaxBackground);
            go.transform.SetParent(parent, false);

            // Las capas se centran en la camara; en runtime ParallaxLayer las recoloca.
            Vector3 camPosition = camera != null ? camera.transform.position : Vector3.zero;
            go.transform.position = new Vector3(camPosition.x, camPosition.y, 0f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = config.SortingOrder;

            ParallaxLayer layer = go.AddComponent<ParallaxLayer>();

            SerializedObject layerSo = new SerializedObject(layer);
            layerSo.FindProperty("_factor").floatValue = config.Factor;
            layerSo.FindProperty("_verticalFactor").floatValue = 0f;
            layerSo.FindProperty("_wrapHorizontal").boolValue = true;
            layerSo.FindProperty("_targetCamera").objectReferenceValue = camera;
            layerSo.ApplyModifiedPropertiesWithoutUndo();

            // Si la vista de la camara es mas alta que la imagen, escalar la capa
            // agranda el motivo a la par y evita que el parallax lo repita en bandas
            // verticales (ParallaxLayer cubre la vista repitiendo el sprite).
            float baseHeight = sprite.bounds.size.y;
            if (baseHeight > 0f)
            {
                float viewHeight = camera != null ? camera.orthographicSize * 2f : baseHeight;
                float scale = viewHeight * CameraViewFitter.CoverMargin / baseHeight;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }

            // Se dimensiona el quad aqui y no solo al arrancar, para que la escena
            // guardada ya se vea correcta en la vista de Scene.
            layer.RefreshQuad(camera);
            layer.ResetScroll();
        }

        static void EnsureImport(string backgroundFolder, string fileName)
        {
            string path = backgroundFolder + "/" + fileName + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool dirty = false;

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, SpritePpu))
            {
                importer.spritePixelsPerUnit = SpritePpu;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            // El modo Tiled de SpriteRenderer necesita el rectangulo completo del
            // sprite; con malla Tight Unity avisa y el mosaico puede salir mal.
            // spriteMeshType solo se puede tocar via TextureImporterSettings.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
