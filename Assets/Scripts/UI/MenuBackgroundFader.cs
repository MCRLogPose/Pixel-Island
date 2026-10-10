using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Rota los fondos del menu en bucle con un fundido cruzado suave.
    ///
    /// Usa dos imagenes superpuestas: la de abajo se queda visible y la de arriba
    /// aparece con un alfa creciente por encima. Al terminar, la de abajo adopta la
    /// imagen ya visible y la de arriba se apaga, de modo que el relevo no se nota y
    /// no hace falta fundir a negro.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Menu Background Fader")]
    public sealed class MenuBackgroundFader : MonoBehaviour
    {
        [Header("Imagenes superpuestas")]
        [Tooltip("Imagen de abajo, siempre visible.")]
        [SerializeField] Image _imageUnder = null;

        [Tooltip("Imagen de arriba, la que entra en cada transicion.")]
        [SerializeField] Image _imageOver = null;

        [SerializeField] CanvasGroup _groupUnder = null;
        [SerializeField] CanvasGroup _groupOver = null;

        [Header("Fondos")]
        [Tooltip("Fondos a rotar en bucle, en orden.")]
        [SerializeField] Sprite[] _backgrounds = new Sprite[0];

        [Header("Tiempos")]
        [Tooltip("Segundos que se queda cada fondo quieto antes de fundir al siguiente.")]
        [SerializeField] float _secondsShown = 6f;

        [Tooltip("Duracion del fundido entre dos fondos.")]
        [SerializeField] float _fadeDuration = 1.5f;

        int _current;

        /// <summary>Indice del fondo que se esta mostrando.</summary>
        public int CurrentIndex => _current;

        void Start()
        {
            if (_backgrounds == null || _backgrounds.Length == 0 || _imageUnder == null)
            {
                enabled = false;
                return;
            }

            _current = 0;
            _imageUnder.sprite = _backgrounds[0];

            if (_groupUnder != null)
            {
                _groupUnder.alpha = 1f;
            }

            if (_groupOver != null)
            {
                _groupOver.alpha = 0f;
            }

            // Con un solo fondo no hay nada que rotar.
            if (_backgrounds.Length > 1 && _imageOver != null)
            {
                StartCoroutine(Cycle());
            }
        }

        IEnumerator Cycle()
        {
            var wait = new WaitForSeconds(_secondsShown);

            while (true)
            {
                yield return wait;

                int next = (_current + 1) % _backgrounds.Length;
                _imageOver.sprite = _backgrounds[next];

                float elapsed = 0f;
                while (elapsed < _fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    if (_groupOver != null)
                    {
                        _groupOver.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
                    }

                    yield return null;
                }

                // Relevo de la imagen de arriba a la de abajo: en el mismo fotograma la
                // de abajo pasa a mostrar lo que ya se ve arriba y la de arriba se apaga.
                _imageUnder.sprite = _imageOver.sprite;
                if (_groupOver != null)
                {
                    _groupOver.alpha = 0f;
                }

                _current = next;
            }
        }
    }
}
