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
    /// Construye <c>Assets/Scenes/ForestLevel.unity</c> con el fondo parallax de
    /// cinco capas. Vive en codigo, y no a mano en el editor, para que la escena
    /// se pueda reconstruir y revisar en control de versiones.
    /// </summary>
    public static class ForestLevelBuilder
    {
        public const string ScenePath = "Assets/Scenes/ForestLevel.unity";
        public const string BackgroundFolder = "Assets/Art/Environment/Background";

        const string GroundSprite = "Assets/Art/Environment/ground_placeholder.png";
        const string PlayerPrefabPath = PlaceholderSceneBuilder.PlayerPrefabPath;

        /// <summary>Alto de la vista en unidades de mundo: 360 px / 32 PPU.</summary>
        const float ViewHeight = 11.25f;

        static readonly Vector3 CameraPosition = new Vector3(0f, 3f, -10f);
        static readonly Vector3 SpawnPosition = new Vector3(1f, 2f, 0f);

        /// <summary>
        /// Una capa por imagen, ordenadas de mas lejos a mas cerca. El factor es la
        /// fraccion del movimiento de camara que recibe cada una: 0 la deja clavada
        /// a la camara y 1 la hace moverse con el mundo.
        /// </summary>
        struct LayerConfig
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

        static readonly LayerConfig[] Layers =
        {
            // El cielo es la unica capa opaca al 100%: es el fondo de verdad.
            new LayerConfig("Background5", "BG_Cielo", "Cielo, opaco al 100%", -100, 0.05f),

            // B1 es la mas oscura y dispersa, luego es la mas lejana. B2 y B3 son
            // bandas de terreno cada vez mas densas. B4 es opaca arriba y transparente
            // abajo: son ramas que cuelgan, o sea el elemento mas cercano.
            new LayerConfig("Background1", "BG_MontanasLejanas", "Montanas lejanas", -90, 0.20f),
            new LayerConfig("Background2", "BG_ColinaMedia", "Colina intermedia", -80, 0.35f),
            new LayerConfig("Background3", "BG_ArbolesCerca", "Arboles cercanos", -70, 0.55f),
            new LayerConfig("Background4", "BG_RamasFrente", "Ramas colgantes del frente", -60, 0.85f),
        };

        [MenuItem("Tools/Pixel Island/Construir ForestLevel (parallax)")]
        public static void BuildForestLevel()
        {
            var missing = new List<string>();
            foreach (LayerConfig layer in Layers)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundFolder + "/" + layer.FileName + ".png") == null)
                {
                    missing.Add(layer.FileName);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogError("[ForestLevel] Faltan imagenes de fondo: " + string.Join(", ", missing) +
                               ". Se esperan en " + BackgroundFolder + ".");
                return;
            }

            Sprite groundTile = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSprite);
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            if (groundTile == null)
            {
                Debug.LogError("[ForestLevel] Falta el sprite de suelo en " + GroundSprite);
                return;
            }

            if (playerPrefab == null)
            {
                Debug.LogError("[ForestLevel] Falta el prefab del Explorador en " + PlayerPrefabPath +
                               ". Ejecuta antes Tools > Pixel Island > Construir escena de pruebas (Etapa 0-1).");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = BuildCamera();
            BuildParallax(camera);
            BuildGround(groundTile);
            GameObject player = PlacePlayer(playerPrefab);

            if (player != null)
            {
                CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
                if (follow != null)
                {
                    SerializedObject followSo = new SerializedObject(follow);
                    followSo.FindProperty("_target").objectReferenceValue = player.transform;
                    followSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[ForestLevel] Escena construida en " + ScenePath + " con " + Layers.Length +
                      " capas de parallax. Camina con A/D o las flechas: las capas se separan " +
                      "segun su factor. El suelo llega hasta x = " + GroundLength + ".");
        }

        [MenuItem("Tools/Pixel Island/Abrir ForestLevel")]
        public static void OpenForestLevel()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        // ---------------------------------------------------------------
        // Escena
        // ---------------------------------------------------------------

        /// <summary>Largo del suelo en unidades de mundo.</summary>
        const float GroundLength = 160f;

        static Camera BuildCamera()
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = CameraPosition;

            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ViewHeight * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;

            // El cielo ya es opaco, pero un color coherente evita destellos rosados
            // si alguna capa quedase un pixel corto en el borde de la pantalla.
            camera.backgroundColor = new Color(0.44f, 0.78f, 0.92f);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<CameraFollow2D>();

            System.Type ppcType =
                System.Type.GetType("UnityEngine.U2D.PixelPerfectCamera, Unity.2D.PixelPerfect");

            if (ppcType == null)
            {
                Debug.LogWarning("[ForestLevel] PixelPerfectCamera no esta disponible; " +
                                 "la camara queda como ortografica simple.");
                return camera;
            }

            Component ppc = cameraGo.AddComponent(ppcType);
            SerializedObject so = new SerializedObject(ppc);
            so.FindProperty("m_AssetsPPU").intValue = PlaceholderArtGenerator.PPU;
            so.FindProperty("m_RefResolutionX").intValue = 640;
            so.FindProperty("m_RefResolutionY").intValue = 360;
            so.FindProperty("m_UpscaleRT").boolValue = true;
            so.FindProperty("m_PixelSnapping").boolValue = true;
            so.FindProperty("m_CropFrameX").boolValue = false;
            so.FindProperty("m_CropFrameY").boolValue = false;
            so.FindProperty("m_StretchFill").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            return camera;
        }

        static void BuildParallax(Camera camera)
        {
            GameObject root = new GameObject("Parallax");

            foreach (LayerConfig config in Layers)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    BackgroundFolder + "/" + config.FileName + ".png");

                GameObject go = new GameObject(config.ObjectName);
                go.layer = GameLayers.Id(GameLayers.ParallaxBackground);
                go.transform.SetParent(root.transform, false);

                // Las imagenes se importan a 16 PPU, que da justo 20x11.25 u: la
                // medida exacta de la vista. Cada capa se coloca centrada en la camara.
                go.transform.position = new Vector3(CameraPosition.x, CameraPosition.y, 0f);

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

                // Se dimensiona el quad aqui y no al arrancar para que la escena
                // guardada ya se vea correcta en la vista de Scene.
                layer.RefreshQuad(camera);
                layer.ResetScroll();
            }
        }

        static void BuildGround(Sprite tile)
        {
            GameObject root = new GameObject("Ground");

            // Superficie pisable en y = 2 y un bloque de relleno debajo, como en la
            // escena de la etapa 0-1. El collider va centrado porque el SpriteRenderer
            // en modo Tiled tambien dibuja centrado (ver AGENTS.md).
            MakeTile("Ground_Surface", tile, 0f, 0f, (int)GroundLength, 2, root.transform);
            MakeTile("Ground_Floor", tile, 0f, -2f, (int)GroundLength, 2, root.transform);

            // Un par de escalones para comprobar que el sensor y el pegado funcionan
            // sobre terreno con cambios de altura.
            MakeTile("Ground_Step1", tile, 20f, 2f, 6, 1, root.transform);
            MakeTile("Ground_Step2", tile, 44f, 2f, 8, 2, root.transform);
        }

        static void MakeTile(string name, Sprite tile, float x, float y, int columns, int rows,
            Transform parent)
        {
            GameObject go = new GameObject(name);
            go.layer = GameLayers.GroundId;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + columns * 0.5f, y + rows * 0.5f, 0f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = tile;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(columns, rows);
            renderer.sortingOrder = -10;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(columns, rows);

            // Unity realinea el collider por su cuenta al cambiar el tamano del
            // renderer en modo Tiled, y lo deja desplazado media casilla.
            box.offset = Vector2.zero;
        }

        static GameObject PlacePlayer(GameObject playerPrefab)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            instance.name = "Player";
            instance.transform.position = SpawnPosition;
            Selection.activeGameObject = instance;
            return instance;
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene existing in scenes)
            {
                if (existing.path == path)
                {
                    existing.enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}