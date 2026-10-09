using PixelIsland.Collectibles;
using PixelIsland.Core;
using PixelIsland.Levels;
using PixelIsland.Player;
using UnityEngine;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Muestra un mensaje al llegar a la meta: "Nivel completado!" si ya estaban
    /// todas las monedas, o un aviso si falta alguna. Escucha al <see cref="GoalFlag"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Goal HUD")]
    public sealed class GoalHud : MonoBehaviour
    {
        [Header("Origen de datos")]
        [Tooltip("Bandera a escuchar. Vacio = se busca en la escena.")]
        [SerializeField] GoalFlag _flag;

        [Header("Mensaje")]
        [SerializeField] Text _label;

        [SerializeField] string _completeMessage = "Nivel completado!";

        [Tooltip("Aviso al tocar la bandera sin todas las monedas.")]
        [SerializeField] string _blockedMessage = "Faltan monedas";

        [Header("Fin de juego")]
        [Tooltip("Bandera de salida para saber si esta zona es la ultima. Vacia = se busca en la escena.")]
        [SerializeField] LevelExit _exit;

        [Tooltip("Mensaje de la ultima zona, donde el juego culmina.")]
        [SerializeField] string _finalMessage = "Juego culminado!";

        void OnEnable()
        {
            if (_flag == null)
            {
                _flag = FindObjectOfType<GoalFlag>();
            }

            if (_exit == null)
            {
                _exit = FindObjectOfType<LevelExit>();
            }

            if (_flag != null)
            {
                _flag.Reached += HandleReached;
                _flag.Blocked += HandleBlocked;
            }

            if (_label != null)
            {
                _label.enabled = false;
            }
        }

        void OnDisable()
        {
            if (_flag != null)
            {
                _flag.Reached -= HandleReached;
                _flag.Blocked -= HandleBlocked;
            }
        }

        void HandleReached(GoalFlag flag)
        {
            bool final = _exit != null && _exit.IsFinalLevel;
            Show(final ? _finalMessage : _completeMessage);

            if (final)
            {
                StopPlayer();
            }
        }

        void HandleBlocked(GoalFlag flag) => Show(_blockedMessage);

        /// <summary>
        /// Al culminar el juego el Explorador se detiene: se corta la lectura de
        /// entrada y se anula la velocidad, para que no siga escapandose mientras se
        /// lee el mensaje final.
        /// </summary>
        void StopPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag(GameLayers.TagPlayer);
            if (player == null)
            {
                return;
            }

            PlayerInputReader input = player.GetComponent<PlayerInputReader>();
            if (input != null)
            {
                input.enabled = false;
            }

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
            }
        }

        void Show(string message)
        {
            if (_label == null)
            {
                return;
            }

            _label.text = message;
            _label.enabled = true;
        }
    }
}
