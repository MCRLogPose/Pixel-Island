using System.Collections.Generic;
using PixelIsland.Collectibles;
using PixelIsland.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Construye el AnimatorController, la animacion y el prefab de la moneda, y el
    /// prefab de la bandera de meta (verde) a partir del arte que ya existe.
    /// Vive en codigo, y no a mano en el editor, para que se pueda reconstruir.
    ///
    ///   Tools > Pixel Island > Construir monedas y meta
    ///
    /// OJO: en <c>Assets/coins</c> y en <c>Flagpoles</c> hay assets de un paquete
    /// antiguo (formato pre-5.0) cuya animacion de moneda apunta a sprites que ya no
    /// existen con esos fileID. Por eso la moneda se regenera aqui en formato nuevo.
    /// </summary>
    public static class CollectibleBuilder
    {
        public const string CoinSheetPath = "Assets/coins/Sprites/coin-animation.png";
        public const string CoinClipPath = "Assets/coins/Animations/CoinSpin.anim";
        public const string CoinControllerPath = "Assets/coins/Animations/Coin.controller";
        public const string CoinPrefabPath = "Assets/Prefabs/Coin.prefab";

        public const string FlagClipPath =
            "Assets/FlagsIndicators/Art/Sprites/Animation/Animations/Flag_green.anim";
        public const string FlagControllerPath =
            "Assets/FlagsIndicators/Art/Sprites/Animation/Animator Controllers/Flag_green.controller";
        public const string FlagPrefabPath = "Assets/Prefabs/GoalFlag.prefab";

        [MenuItem("Tools/Pixel Island/Construir monedas y meta")]
        public static void BuildAll()
        {
            BuildCoin();
            BuildFlag();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CollectibleBuilder] Moneda en " + CoinPrefabPath + " y bandera en " + FlagPrefabPath + ".");
        }

        // ---------------------------------------------------------------
        // Moneda
        // ---------------------------------------------------------------

        static void BuildCoin()
        {
            List<Sprite> frames = LoadSubSprites(CoinSheetPath);
            if (frames.Count == 0)
            {
                Debug.LogError("[CollectibleBuilder] No hay sprites en " + CoinSheetPath);
                return;
            }

            frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            AnimationClip clip = BuildClip(CoinClipPath, "CoinSpin", frames, 12f);
            AnimatorController controller = BuildSingleStateController(CoinControllerPath, "Spin", clip);
            BuildCoinPrefab(controller, frames[0]);
        }

        static void BuildCoinPrefab(AnimatorController controller, Sprite firstFrame)
        {
            GameObject go = new GameObject("Coin");
            go.layer = GameLayers.CoinId;
            go.tag = GameLayers.TagCoin;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = firstFrame;
            renderer.sortingOrder = 3;

            Animator animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.7f, 1.7f);

            go.AddComponent<Coin>();

            // El sprite mide ~2 unidades a 32 PPU; se escala a ~1 unidad para que
            // ocupe un tile, como el resto del nivel.
            go.transform.localScale = Vector3.one * ScaleForTargetSize(firstFrame, 1f);

            EnsureFolder("Assets/Prefabs");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, CoinPrefabPath);
            Object.DestroyImmediate(go);

            if (prefab == null)
            {
                Debug.LogError("[CollectibleBuilder] No se pudo guardar " + CoinPrefabPath);
            }
        }

        // ---------------------------------------------------------------
        // Bandera de meta
        // ---------------------------------------------------------------

        static void BuildFlag()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(FlagControllerPath);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FlagClipPath);
            Sprite firstFrame = FirstSpriteOfClip(clip);

            if (controller == null || firstFrame == null)
            {
                Debug.LogError("[CollectibleBuilder] Faltan el controller o el sprite de " + FlagControllerPath);
                return;
            }

            BuildFlagPrefab(controller, firstFrame);
        }

        static void BuildFlagPrefab(AnimatorController controller, Sprite firstFrame)
        {
            GameObject go = new GameObject("GoalFlag");
            go.layer = GameLayers.CoinId;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = firstFrame;
            renderer.sortingOrder = 4;

            Animator animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            // Pivote al pie (0.5, 0): el collider sube desde la base.
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.7f, 1f);
            collider.offset = new Vector2(0f, 0.5f);

            go.AddComponent<GoalFlag>();

            go.transform.localScale = Vector3.one * ScaleForTargetSize(firstFrame, 2f);

            EnsureFolder("Assets/Prefabs");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, FlagPrefabPath);
            Object.DestroyImmediate(go);

            if (prefab == null)
            {
                Debug.LogError("[CollectibleBuilder] No se pudo guardar " + FlagPrefabPath);
            }
        }

        // ---------------------------------------------------------------
        // Utilidades
        // ---------------------------------------------------------------

        static AnimationClip BuildClip(string path, string name, List<Sprite> frames, float frameRate)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AnimationClip clip = new AnimationClip { name = name, frameRate = frameRate };
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

            // +1 para cerrar el bucle repitiendo el primer frame.
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Count + 1];
            for (int i = 0; i < frames.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
            }

            keys[frames.Count] = new ObjectReferenceKeyframe { time = frames.Count / frameRate, value = frames[0] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static AnimatorController BuildSingleStateController(string path, string stateName, AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorState state = controller.layers[0].stateMachine.AddState(stateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static List<Sprite> LoadSubSprites(string path)
        {
            var sprites = new List<Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        static Sprite FirstSpriteOfClip(AnimationClip clip)
        {
            if (clip == null)
            {
                return null;
            }

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            return (keys != null && keys.Length > 0) ? keys[0].value as Sprite : null;
        }

        static float ScaleForTargetSize(Sprite sprite, float targetHeight)
        {
            float native = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            return native > 0.0001f ? targetHeight / native : 1f;
        }

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
