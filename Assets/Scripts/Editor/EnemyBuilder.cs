using System.Collections.Generic;
using PixelIsland.Core;
using PixelIsland.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Construye el AnimatorController y el prefab del enemigo a partir del arte
    /// que ya existe en <c>Assets/Enemy</c>. Vive en codigo, y no a mano en el
    /// editor, para que se pueda reconstruir y revisar en control de versiones.
    ///
    ///   Tools > Pixel Island > Construir enemigo (controller + prefab)
    /// </summary>
    public static class EnemyBuilder
    {
        public const string SheetPath = "Assets/Enemy/EnemySheet.png";
        public const string ControllerPath = "Assets/Enemy/Animations/Enemy.controller";
        public const string PrefabPath = "Assets/Prefabs/Enemy.prefab";

        const string IdleClipPath = "Assets/Enemy/Animations/EnemyIdle.anim";
        const string JumpClipPath = "Assets/Enemy/Animations/EnemyJump.anim";
        const string FallClipPath = "Assets/Enemy/Animations/EnemyFall.anim";

        [MenuItem("Tools/Pixel Island/Construir enemigo (controller + prefab)")]
        public static void BuildEnemy()
        {
            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            AnimationClip jump = AssetDatabase.LoadAssetAtPath<AnimationClip>(JumpClipPath);
            AnimationClip fall = AssetDatabase.LoadAssetAtPath<AnimationClip>(FallClipPath);
            Sprite sprite = LoadSubSprite(SheetPath, "EnemySheet_0");

            if (idle == null || jump == null || fall == null)
            {
                Debug.LogError("[EnemyBuilder] Faltan clips en " + "Assets/Enemy/Animations" + ".");
                return;
            }

            if (sprite == null)
            {
                Debug.LogError("[EnemyBuilder] No se encontro el sprite EnemySheet_0 en " + SheetPath);
                return;
            }

            AnimatorController controller = BuildController(idle, jump, fall);
            BuildPrefab(controller, sprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[EnemyBuilder] Controller en " + ControllerPath + " y prefab en " + PrefabPath +
                      ". El enemigo patrulla una linea horizontal centrada en su aparicion.");
        }

        // ---------------------------------------------------------------
        // AnimatorController
        // ---------------------------------------------------------------

        static AnimatorController BuildController(AnimationClip idle, AnimationClip jump, AnimationClip fall)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(EnemyPatrolController.StateParameter, AnimatorControllerParameterType.Int);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = idle;

            AnimatorState jumpState = machine.AddState("Jump");
            jumpState.motion = jump;

            AnimatorState fallState = machine.AddState("Fall");
            fallState.motion = fall;

            machine.defaultState = idleState;

            var states = new List<AnimatorState> { idleState, jumpState, fallState };
            for (int from = 0; from < states.Count; from++)
            {
                for (int to = 0; to < states.Count; to++)
                {
                    if (from == to)
                    {
                        continue;
                    }

                    AnimatorStateTransition transition = states[from].AddTransition(states[to], false);
                    transition.hasExitTime = false;
                    transition.duration = 0f;
                    transition.AddCondition(AnimatorConditionMode.Equals, to,
                        EnemyPatrolController.StateParameter);
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ---------------------------------------------------------------
        // Prefab
        // ---------------------------------------------------------------

        static void BuildPrefab(AnimatorController controller, Sprite sprite)
        {
            GameObject go = new GameObject("Enemy");
            go.layer = GameLayers.EnemyId;
            go.tag = GameLayers.TagEnemy;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // El sprite de 16x16 se importa a 8 PPU: 2x2 unidades con pivote al
            // centro. El collider solido ocupa el cuerpo (fondo en y = -0.4) y el
            // trigger de contacto es algo mas ancho y alto para alcanzar al Player.
            BoxCollider2D solid = go.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1.2f, 0.6f);
            solid.offset = new Vector2(0f, -0.1f);

            BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.8f, 1f);
            trigger.offset = new Vector2(0f, 0.1f);

            Animator animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            EnemyPatrolController patrol = go.AddComponent<EnemyPatrolController>();
            go.AddComponent<EnemyContactDamage>();

            SerializedObject patrolSo = new SerializedObject(patrol);
            patrolSo.FindProperty("_animator").objectReferenceValue = animator;
            patrolSo.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
            patrolSo.FindProperty("_solidMask").intValue = GameLayers.GroundMask.value;
            patrolSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/Prefabs");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);

            if (prefab == null)
            {
                Debug.LogError("[EnemyBuilder] No se pudo guardar el prefab en " + PrefabPath);
            }
        }

        // ---------------------------------------------------------------
        // Utilidades
        // ---------------------------------------------------------------

        static Sprite LoadSubSprite(string path, string spriteName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite candidate && candidate.name == spriteName)
                {
                    return candidate;
                }
            }

            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
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
