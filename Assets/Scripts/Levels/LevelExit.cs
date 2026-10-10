using System;
using System.Collections;
using PixelIsland.Collectibles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelIsland.Levels
{
    /// <summary>
    /// Avanza a la siguiente zona cuando la <see cref="GoalFlag"/> del nivel se
    /// completa. La bandera solo decide; este componente es quien reacciona cargando la
    /// escena siguiente de <see cref="LevelSequence"/>.
    ///
    /// La escena de destino se resuelve por el nombre de la escena ACTIVA, asi que el
    /// componente no guarda rutas y el flujo se cambia en un solo sitio.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/Levels/Level Exit")]
    public sealed class LevelExit : MonoBehaviour
    {
        [Tooltip("Bandera a escuchar. Vacio = se busca en la escena.")]
        [SerializeField] GoalFlag _flag;

        [Tooltip("Segundos antes de cargar la siguiente zona, para que se lea el mensaje.")]
        [SerializeField] float _delaySeconds = 1f;

        [Tooltip("Segundos antes de volver al menu al ganar la ultima zona, para leer el mensaje.")]
        [SerializeField] float _finalDelaySeconds = 2f;

        /// <summary>Escena que se cargara al completar el nivel, o null si es la ultima.</summary>
        public string NextScene { get; private set; }

        /// <summary>True cuando la escena activa es la ultima de <see cref="LevelSequence"/>:
        /// al completarla el juego termina en vez de cargar otra zona.</summary>
        public bool IsFinalLevel { get; private set; }

        /// <summary>Se dispara al completar la ultima zona: el juego ha culminado.</summary>
        public event Action GameCompleted;

        /// <summary>
        /// True si la escena es la ultima de la secuencia. Estatico y sin estado para
        /// verificarlo en tests de EditMode sin cargar escenas.
        /// </summary>
        public static bool IsFinal(string sceneName) => LevelSequence.IsLast(sceneName);

        void OnEnable()
        {
            NextScene = LevelSequence.Next(SceneManager.GetActiveScene().name);
            IsFinalLevel = IsFinal(SceneManager.GetActiveScene().name);

            if (_flag == null)
            {
                _flag = FindObjectOfType<GoalFlag>();
            }

            if (_flag != null)
            {
                _flag.Reached += HandleReached;
            }
        }

        void OnDisable()
        {
            if (_flag != null)
            {
                _flag.Reached -= HandleReached;
            }
        }

        void HandleReached(GoalFlag flag)
        {
            if (IsFinalLevel)
            {
                Debug.Log("[LevelExit] '" + SceneManager.GetActiveScene().name +
                          "' es la ultima zona: el juego ha culminado. Volviendo al menu.");
                GameCompleted?.Invoke();
                LoadScene(SceneFlow.MenuSceneName, _finalDelaySeconds);
                return;
            }

            if (string.IsNullOrEmpty(NextScene))
            {
                Debug.LogWarning("[LevelExit] '" + SceneManager.GetActiveScene().name +
                                 "' no pertenece a LevelSequence: no se carga ninguna escena.");
                return;
            }

            LoadScene(NextScene, _delaySeconds);
        }

        void LoadScene(string sceneName, float delay)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (delay > 0f)
            {
                StartCoroutine(LoadAfterDelay(sceneName, delay));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        IEnumerator LoadAfterDelay(string sceneName, float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(sceneName);
        }
    }
}
