using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelIsland.Core
{
    /// <summary>
    /// Consecuencia de quedarse sin vidas: recarga la escena activa (reinicio de
    /// partida). Escucha a <see cref="LivesComponent"/>, de modo que da igual la
    /// causa de la muerte (caer al vacio o que un enemigo te quite la ultima
    /// vida): el hazard solo tiene que llevar las vidas a 0.
    ///
    /// Vive en el mismo GameObject que el <see cref="LivesComponent"/>; si no se
    /// asigna, se resuelve solo. La espera antes de recargar deja ver los
    /// corazones apagarse en el HUD.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/Core/Player Death")]
    public sealed class PlayerDeath : MonoBehaviour
    {
        [Tooltip("LivesComponent a vigilar. Vacio = el de este mismo GameObject.")]
        [SerializeField] LivesComponent _lives;

        [Header("Reinicio")]
        [Tooltip("Segundos que se espera antes de reiniciar, para que se vea perder las vidas.")]
        [SerializeField] float _restartDelay = 0.5f;

        [Tooltip("Si esta activo, recarga la escena al quedarse sin vidas.")]
        [SerializeField] bool _restartScene = true;

        bool _handling;

        public float RestartDelay
        {
            get => _restartDelay;
            set => _restartDelay = value;
        }

        void Awake()
        {
            if (_lives == null)
            {
                _lives = GetComponent<LivesComponent>();
            }
        }

        void OnEnable()
        {
            if (_lives != null)
            {
                _lives.Changed += HandleLivesChanged;
            }
        }

        void OnDisable()
        {
            if (_lives != null)
            {
                _lives.Changed -= HandleLivesChanged;
            }

            _handling = false;
        }

        void HandleLivesChanged(Lives lives)
        {
            if (lives != null && lives.IsEmpty)
            {
                BeginRestart();
            }
        }

        void BeginRestart()
        {
            if (_handling)
            {
                return;
            }

            _handling = true;

            if (_restartScene)
            {
                StartCoroutine(RestartAfterDelay());
            }
        }

        IEnumerator RestartAfterDelay()
        {
            if (_restartDelay > 0f)
            {
                yield return new WaitForSeconds(_restartDelay);
            }

            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.name);
        }
    }
}
