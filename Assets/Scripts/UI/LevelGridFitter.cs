using UnityEngine;
using UnityEngine.UI;

namespace PixelIsland.UI
{
    /// <summary>
    /// Mantiene los botones de nivel en una rejilla centrada sobre la mesa.
    ///
    /// GridLayoutGroup coloca los hijos anclados arriba a la izquierda de su rect, asi
    /// que un bloque incompleto queda descentrado. Este componente dimensiona el
    /// contenedor al tamaño exacto del contenido (columnas x filas reales) para que la
    /// rejilla, ya con las anclas del contenedor en el centro de la mesa, se distribuya
    /// de forma simetrica pase lo que pase con el numero de niveles.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/UI/Level Grid Fitter")]
    public sealed class LevelGridFitter : MonoBehaviour
    {
        GridLayoutGroup _grid;

        /// <summary>
        /// Rejilla del contenedor. Se resuelve de forma perezosa porque Awake no corre
        /// al abrir una escena en edicion, y sin esto la propiedad estaria nula justo
        /// cuando mas hace falta: centrar el bloque al montar la escena.
        /// </summary>
        public GridLayoutGroup Grid
        {
            get
            {
                if (_grid == null)
                {
                    _grid = GetComponent<GridLayoutGroup>();
                }

                return _grid;
            }
        }

        /// <summary>
        /// Tamano del bloque de rejilla para un numero de elementos dado. Estatico y sin
        /// estado para poder verificarlo en tests de EditMode sin montar escenas.
        /// </summary>
        public static Vector2 ComputeBlockSize(int itemCount, Vector2 cellSize,
            Vector2 spacing, RectOffset padding, int columnCount)
        {
            int columns = Mathf.Max(1, columnCount);
            if (itemCount <= 0)
            {
                return Vector2.zero;
            }

            int rows = Mathf.CeilToInt(itemCount / (float)columns);
            float width = padding.horizontal + columns * cellSize.x + (columns - 1) * spacing.x;
            float height = padding.vertical + rows * cellSize.y + (rows - 1) * spacing.y;
            return new Vector2(width, height);
        }

        void Start()
        {
            CenterOnContent();
        }

        /// <summary>Redimensiona el contenedor al contenido para que la rejilla quede centrada.</summary>
        public void CenterOnContent()
        {
            GridLayoutGroup grid = Grid;
            if (grid == null || grid.transform.childCount == 0)
            {
                return;
            }

            int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, grid.constraintCount)
                : Mathf.Max(1, grid.transform.childCount);

            var rect = (RectTransform)transform;
            rect.sizeDelta = ComputeBlockSize(grid.transform.childCount,
                grid.cellSize, grid.spacing, grid.padding, columns);
        }
    }
}