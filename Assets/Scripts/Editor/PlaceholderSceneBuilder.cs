using System.IO;
using PixelIsland.Core;
using PixelIsland.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Monta el escenario minimo de la Etapa 0 + Etapa 1 para poder testear el
    /// movimiento sin arte final:
    ///   Tools > Pixel Island > Construir escena de pruebas (Etapa 0-1)
    ///
    /// Reconstruye SampleScene desde cero, asi que se puede volver a ejecutar
    /// en cualquier momento tras cambiar el tuning.
    /// </summary>
    public static class PlaceholderSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SampleScene.unity";
        public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        const string ExplorerSprite = "Assets/Art/Characters/explorer_placeholder.png";
        const string GroundSprite = "Assets/Art/Environment/ground_placeholder.png";

        /// <summary>
        /// Punto de aparicion. La superficie transitable es la cara superior de
        /// Ground_Surface, en y = 2; los escalones se apoyan encima. Aparece aqui
        /// porque es el unico sitio donde los pies del Explorador tocan el suelo sin
        /// que el rigidbody nazca dentro del propio bloque de terreno.
        /// </summary>
        static readonly Vector3 SpawnPosition = new Vector3(1f, 2f, 0f);

        [MenuItem("Tools/Pixel Island/Construir escena de pruebas (Etapa 0-1)")]
        public static void BuildSandboxScene()
        {
            Sprite explorer = LoadSprite(ExplorerSprite);
            Sprite ground = LoadSprite(GroundSprite);

            if (explorer == null || ground == null)
            {
                Debug.LogError("[SceneBuilder] Faltan los sprites placeholder. Ejecuta antes " +
                               "Tools > Pixel Island > Regenerar arte placeholder.");
                return;
            }

            GameObject playerPrefab = BuildPlayerPrefab(explorer);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildGround(ground);
            PlacePlayer(playerPrefab);
            BuildSpawnMarker();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[SceneBuilder] Escena de pruebas construida en " + ScenePath +
                      ". Pulsa Play: A/D o flechas para caminar, Espacio para saltar, Shift para correr.");
        }

        // ---------------------------------------------------------------
        // Prefab del Explorador
        // ---------------------------------------------------------------

        static GameObject BuildPlayerPrefab(Sprite sprite)
        {
            GameObject go = new GameObject("Player");
            go.layer = GameLayers.PlayerId;
            go.tag = GameLayers.TagPlayer;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // El sprite tiene pivote abajo-centro, asi que el collider de 1x2
            // unidades va desplazado +1 y apoya exactamente sobre y = 0.
            CapsuleCollider2D capsule = go.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.72f, 2f);
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.offset = new Vector2(0f, 1f);

            PlayerInputReader input = go.AddComponent<PlayerInputReader>();
            GroundSensor sensor = go.AddComponent<GroundSensor>();
            PlayerController controller = go.AddComponent<PlayerController>();

            // Los campos del controller son [SerializeField] private, asi que se
            // cablean por serializado y no con setters publicos.
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_inputSource").objectReferenceValue = input;
            controllerSo.FindProperty("_groundSensor").objectReferenceValue = sensor;
            controllerSo.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject sensorSo = new SerializedObject(sensor);
            sensorSo.FindProperty("_groundMask").intValue = GameLayers.GroundMask.value;
            sensorSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/Prefabs");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);

            if (prefab == null)
            {
                Debug.LogError("[SceneBuilder] No se pudo guardar el prefab en " + PlayerPrefabPath);
            }

            return prefab;
        }

        // ---------------------------------------------------------------
        // Escena
        // ---------------------------------------------------------------

        static void BuildCamera()
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 3f, -10f);

            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.625f; // 360 px de alto / 2 / 32 PPU
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.44f, 0.78f, 0.92f);
            cameraGo.AddComponent<AudioListener>();

            System.Type ppcType =
                System.Type.GetType("UnityEngine.U2D.PixelPerfectCamera, Unity.2D.PixelPerfect");

            if (ppcType == null)
            {
                Debug.LogWarning("[SceneBuilder] PixelPerfectCamera no esta disponible; la camara " +
                                 "queda como ortografica simple.");
                return;
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
        }

        static void BuildGround(Sprite tile)
        {
            GameObject root = new GameObject("Ground");

            MakeTile("Ground_Surface", tile, 0f, 0f, 64, 2, root.transform);
            MakeTile("Ground_Floor", tile, 0f, -2f, 64, 2, root.transform);
            MakeTile("Ground_Step1", tile, 6f, 2f, 8, 1, root.transform);
            MakeTile("Ground_Step2", tile, 15f, 2f, 6, 1, root.transform);
            MakeTile("Ground_Step3", tile, 24f, 2f, 10, 1, root.transform);
        }

        static GameObject MakeTile(string name, Sprite tile, float x, float y, int columns, int rows,
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
            renderer.sortingOrder = 0;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(columns, rows);

            // Unity guarda en el BoxCollider2D una copia del "sprite tiling" del
            // SpriteRenderer (m_SpriteTilingProperty) y, al cambiar el tamano del
            // renderer en modo Tiled, realinea el collider por su cuenta dejando
            // offset = mitad del sprite original. Como aqui el renderer ya dibuja
            // centrado en el transform, el collider tambien debe estarlo.
            box.offset = Vector2.zero;

            return go;
        }

        static void PlacePlayer(GameObject playerPrefab)
        {
            if (playerPrefab == null)
            {
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            instance.name = "Player";
            instance.transform.position = SpawnPosition;
            Selection.activeGameObject = instance;
        }

        static void BuildSpawnMarker()
        {
            GameObject spawn = new GameObject("Spawn");
            spawn.transform.position = SpawnPosition;
        }

        // ---------------------------------------------------------------
        // Utilidades
        // ---------------------------------------------------------------

        static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
