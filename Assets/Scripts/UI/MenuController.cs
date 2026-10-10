using PixelIsland.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Logica del menu principal: arranca la primera zona, abre y cierra el panel de
    /// seleccion de niveles y cierra el juego.
    ///
    /// Las zonas salen siempre de <see cref="LevelSequence"/>, nunca de rutas escritas
    /// a mano, para que el orden de los niveles viva en un solo sitio. Los botones de
    /// nivel no pasan por aqui: cada uno usa <see cref="LevelButton"/> con su indice.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Menu Controller")]
    public sealed class MenuController : MonoBehaviour
    {
        [Header("Pantallas")]
        [Tooltip("Panel de seleccion de niveles. Vacio = no se puede abrir.")]
        [SerializeField] GameObject _levelSelectPanel;

        [Header("Botones principales")]
        [SerializeField] Button _playButton;
        [SerializeField] Button _levelsButton;
        [SerializeField] Button _quitButton;

        [Header("Panel de niveles")]
        [SerializeField] Button _closeLevelSelectButton;

        void OnEnable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(PlayFirstLevel);
            }

            if (_levelsButton != null)
            {
                _levelsButton.onClick.AddListener(OpenLevelSelect);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(QuitGame);
            }

            if (_closeLevelSelectButton != null)
            {
                _closeLevelSelectButton.onClick.AddListener(CloseLevelSelect);
            }
        }

        void OnDisable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(PlayFirstLevel);
            }

            if (_levelsButton != null)
            {
                _levelsButton.onClick.RemoveListener(OpenLevelSelect);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(QuitGame);
            }

            if (_closeLevelSelectButton != null)
            {
                _closeLevelSelectButton.onClick.RemoveListener(CloseLevelSelect);
            }
        }

        /// <summary>Arranca la primera zona de <see cref="LevelSequence"/>.</summary>
        public void PlayFirstLevel()
        {
            string[] scenes = LevelSequence.Scenes;
            if (scenes == null || scenes.Length == 0)
            {
                Debug.LogWarning("[Menu] No hay zonas registradas en LevelSequence.");
                return;
            }

            SceneManager.LoadScene(scenes[0]);
        }

        /// <summary>Muestra el panel de seleccion de niveles.</summary>
        public void OpenLevelSelect()
        {
            if (_levelSelectPanel != null)
            {
                _levelSelectPanel.SetActive(true);
            }
        }

        /// <summary>Oculta el panel de seleccion de niveles.</summary>
        public void CloseLevelSelect()
        {
            if (_levelSelectPanel != null)
            {
                _levelSelectPanel.SetActive(false);
            }
        }

        /// <summary>Carga la zona <paramref name="index"/>-esima de <see cref="LevelSequence"/> (0-based).</summary>
        public void LoadLevel(int index)
        {
            string[] scenes = LevelSequence.Scenes;
            if (scenes == null || index < 0 || index >= scenes.Length)
            {
                Debug.LogWarning("[Menu] No existe el nivel " + index + " en LevelSequence.");
                return;
            }

            SceneManager.LoadScene(scenes[index]);
        }

        /// <summary>Cierra el juego, o detiene el Play Mode si se esta en el editor.</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
