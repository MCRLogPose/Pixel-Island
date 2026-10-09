using PixelIsland.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Pinta las vidas como una fila de corazones llenos o vacios.
    ///
    /// Si se le asigna un <see cref="LivesComponent"/> se limita a reflejarlo; los
    /// corazones que sobren por encima de <see cref="Lives.Max"/> se ocultan. Sin
    /// origen de datos solo pinta los corazones que le demos, todos llenos.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Lives HUD")]
    public sealed class LivesHud : MonoBehaviour
    {
        [Header("Origen de datos")]
        [Tooltip("Componente de vidas a escuchar. Vacio = pinta los corazones como vidas fijas.")]
        [SerializeField] LivesComponent _lives;

        [Header("Iconos")]
        [SerializeField] Sprite _heartFull;
        [SerializeField] Sprite _heartEmpty;

        [Tooltip("Corazones en orden de izquierda a derecha. " +
                 "Si se deja vacio se recogen los hijos con Image.")]
        [SerializeField] Image[] _hearts;

        void OnEnable()
        {
            if (_lives != null)
            {
                _lives.Changed += HandleChanged;
            }

            Refresh();
        }

        void OnDisable()
        {
            if (_lives != null)
            {
                _lives.Changed -= HandleChanged;
            }
        }

        /// <summary>Repinta los corazones con el estado actual de las vidas.</summary>
        public void Refresh()
        {
            Image[] hearts = ResolveHearts();
            int max = _lives != null ? _lives.MaxLives : hearts.Length;
            int current = _lives != null ? _lives.CurrentLives : hearts.Length;

            for (int i = 0; i < hearts.Length; i++)
            {
                Image heart = hearts[i];
                if (heart == null)
                {
                    continue;
                }

                heart.enabled = i < max;
                heart.sprite = i < current ? _heartFull : _heartEmpty;
            }
        }

        Image[] ResolveHearts()
        {
            if (_hearts != null && _hearts.Length > 0)
            {
                return _hearts;
            }

            return GetComponentsInChildren<Image>(true);
        }

        void HandleChanged(Lives lives) => Refresh();
    }
}
