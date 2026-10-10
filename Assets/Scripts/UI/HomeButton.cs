using PixelIsland.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Boton Home de la HUD de una zona: al pulsarlo vuelve al menu principal. Va
    /// pegado al <see cref="Button"/> del icono Home; la escena engancha su onClick a
    /// <see cref="ReturnToMenu"/> y el componente tambien se auto-suscita por si el
    /// cableado de la escena se pierde al duplicar el boton.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Home Button")]
    public sealed class HomeButton : MonoBehaviour
    {
        [Tooltip("Boton que dispara la vuelta al menu. Vacio = se toma el de este objeto.")]
        [SerializeField] Button _button;

        void OnEnable()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_button != null)
            {
                _button.onClick.AddListener(ReturnToMenu);
            }
        }

        void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(ReturnToMenu);
            }
        }

        /// <summary>
        /// Carga la escena del menu principal. Si la llamada llega doble (onClick de la
        /// escena mas la auto-suscripcion) o estando ya en el menu, no hace nada.
        /// </summary>
        public void ReturnToMenu()
        {
            if (SceneManager.GetActiveScene().name == SceneFlow.MenuSceneName)
            {
                return;
            }

            SceneFlow.LoadMenu();
        }
    }
}