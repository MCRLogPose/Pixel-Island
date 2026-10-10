using UnityEngine;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Boton de un nivel del panel de seleccion. Guarda su indice en la secuencia y
    /// se lo pasa a <see cref="MenuController"/> al pulsarlo.
    ///
    /// Asi el indice de cada boton vive en la escena (editable y visible) en vez de en
    /// cuatro bloques de onClick distintos que hay que tocar a mano cada vez.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Level Button")]
    public sealed class LevelButton : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] MenuController _menu;

        [Tooltip("Indice 0-based en LevelSequence.")]
        [SerializeField] int _levelIndex;

        /// <summary>Indice de la zona que carga este boton.</summary>
        public int LevelIndex
        {
            get => _levelIndex;
            set => _levelIndex = value;
        }

        public MenuController Menu
        {
            get => _menu;
            set => _menu = value;
        }

        void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
        }

        void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }

        void HandleClick()
        {
            if (_menu != null)
            {
                _menu.LoadLevel(_levelIndex);
            }
        }
    }
}
