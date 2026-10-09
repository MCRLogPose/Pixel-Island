using PixelIsland.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelIsland.Editor
{
    /// <summary>
    /// Amplia la vista de la camara de la escena ABIERTA y reescala el parallax para
    /// que siga cubriendo la pantalla sin repetirse en bandas verticales.
    ///
    /// Al subir orthographicSize la vista puede hacerse mas alta que la imagen de
    /// fondo (11.25 u). ParallaxLayer cubre la vista repitiendo el sprite, asi que una
    /// imagen corta se veria dos veces. Escalar la capa agranda el motivo a la par y
    /// conserva la proporcion de la escena; el quad se recalcula despues.
    /// </summary>
    public static class CameraViewFitter
    {
        /// <summary>Solape extra para que el borde del tile no asome por redondeo.</summary>
        public const float CoverMargin = 1.03f;

        /// <summary>Alto de vista por defecto: 14 u (orthographicSize 7).</summary>
        public const float DefaultViewHeight = 14f;

        [MenuItem("Tools/Pixel Island/Ajustar vista de camara y parallax (14 u)")]
        public static void ApplyDefault()
        {
            ApplyToActiveScene(DefaultViewHeight);
        }

        /// <summary>
        /// Fija <c>orthographicSize = viewHeight / 2</c> en la camara principal y
        /// reescala cada capa de parallax para que su sprite cubra esa altura.
        /// </summary>
        public static void ApplyToActiveScene(float viewHeight)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[CameraViewFitter] No hay escena abierta que modificar.");
                return;
            }

            if (viewHeight <= 0f)
            {
                Debug.LogError("[CameraViewFitter] El alto de vista debe ser positivo.");
                return;
            }

            Camera camera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("[CameraViewFitter] No hay camara en la escena '" + scene.name + "'.");
                return;
            }

            camera.orthographicSize = viewHeight * 0.5f;

            ParallaxLayer[] layers = Object.FindObjectsOfType<ParallaxLayer>();
            foreach (ParallaxLayer layer in layers)
            {
                SpriteRenderer renderer = layer.Renderer;
                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                float baseHeight = renderer.sprite.bounds.size.y;
                if (baseHeight <= 0f)
                {
                    continue;
                }

                float scale = viewHeight * CoverMargin / baseHeight;
                layer.transform.localScale = new Vector3(scale, scale, 1f);

                layer.RefreshQuad(camera);
                layer.ResetScroll();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[CameraViewFitter] '" + scene.name + "': vista de " + viewHeight.ToString("0.##") +
                      " u (ortho " + camera.orthographicSize.ToString("0.##") + "), parallax reescalado en " +
                      layers.Length + " capa(s).");
        }
    }
}
