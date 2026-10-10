using System.Linq;
using NUnit.Framework;
using PixelIsland.Levels;
using PixelIsland.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Verifica la escena Menu tal y como queda GUARDADA en disco: que el canvas traiga
    /// los tres botones del menu, que el fondo rote por los tres ladrillos del
    /// spritesheet con dos imagenes superpuestas, y que el panel de niveles ofrezca un
    /// boton por cada zona de <see cref="LevelSequence"/>.
    ///
    /// No abre la escena en Play Mode; monta el escenario de fisica ajeno a estos tests.
    /// </summary>
    public sealed class MenuSceneTests
    {
        const string ScenePath = "Assets/Scenes/Menu.unity";

        SceneSetup[] _previousSetup;

        [SetUp]
        public void SetUp()
        {
            _previousSetup = EditorSceneManager.GetSceneManagerSetup();
        }

        [TearDown]
        public void TearDown()
        {
            if (_previousSetup != null && _previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(_previousSetup);
            }
        }

        static Scene OpenMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "No se pudo abrir " + ScenePath);
            Assert.IsTrue(scene.isLoaded, "No se pudo cargar " + ScenePath);
            return scene;
        }

        /// <summary>Todos los componentes del tipo dado, incluidos los de objetos inactivos
        /// (el panel de niveles empieza oculto, y FindObjectsOfType se lo salta).</summary>
        static T[] FindAll<T>(Scene scene) where T : Component
        {
            var found = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found.ToArray();
        }

        static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        return t.gameObject;
                    }
                }
            }

            return null;
        }

        [Test]
        public void LaEscenaTieneUnCanvasConLosTresBotones()
        {
            OpenMenu();

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            Assert.IsNotNull(canvas, "El menu deberia traer un Canvas.");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode,
                "El menu deberia dibujarse por encima de la escena, en overlay.");

            Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>(),
                "El Canvas necesita un GraphicRaycaster para que los botones reciban clics.");

            Button play = GameObject.Find("PlayButton") != null
                ? GameObject.Find("PlayButton").GetComponent<Button>()
                : null;
            Button levels = GameObject.Find("LevelsButton") != null
                ? GameObject.Find("LevelsButton").GetComponent<Button>()
                : null;
            Button exit = GameObject.Find("ExitButton") != null
                ? GameObject.Find("ExitButton").GetComponent<Button>()
                : null;

            Assert.IsNotNull(play, "Falta el boton de jugar (PlayButton).");
            Assert.IsNotNull(levels, "Falta el boton de niveles (LevelsButton).");
            Assert.IsNotNull(exit, "Falta el boton de salir (ExitButton).");
            Assert.IsNotNull(play.image, "PlayButton deberia llevar su icono.");
            Assert.IsNotNull(levels.image, "LevelsButton deberia llevar su icono.");
            Assert.IsNotNull(exit.image, "ExitButton deberia llevar su icono.");
        }

        [Test]
        public void ElMenuTieneFondoRotativoConDosImagenesYSusTresLadrillos()
        {
            OpenMenu();

            MenuBackgroundFader fader = Object.FindObjectOfType<MenuBackgroundFader>();
            Assert.IsNotNull(fader, "El menu deberia rotar sus fondos con MenuBackgroundFader.");

            var images = Object.FindObjectsOfType<Image>();
            Image[] overlays = System.Array.FindAll(images, i => i.sprite != null && i.sprite.name.StartsWith("Backgrounds_"));
            Assert.GreaterOrEqual(overlays.Length, 2,
                "El fundido necesita dos imagenes de fondo superpuestas.");

            string sheetPath = AssetDatabase.GetAssetPath(overlays[0].sprite);
            Assert.AreEqual("Assets/Art/ExtraMaterialsUI/Art/Sprites/Backgrounds.png", sheetPath,
                "Los fondos deberian salir del spritesheet Backgrounds.");

            Sprite[] sheetSprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .Where(s => s.name.StartsWith("Backgrounds_"))
                .ToArray();

            Assert.AreEqual(3, sheetSprites.Length,
                "Backgrounds.png deberia traer exactamente tres fondos para rotar.");
        }

        [Test]
        public void HayUnBotonDeNivelPorCadaZonaDeLaSecuencia()
        {
            Scene scene = OpenMenu();

            LevelButton[] levelButtons = FindAll<LevelButton>(scene);
            int expected = LevelSequence.Scenes.Length;

            Assert.AreEqual(expected, levelButtons.Length,
                "Deberia haber un boton por cada zona de LevelSequence.");

            for (int i = 0; i < levelButtons.Length; i++)
            {
                Assert.AreEqual(i, levelButtons[i].LevelIndex,
                    "El boton " + i + " deberia cargar el nivel " + i + " de la secuencia.");
                Assert.IsNotNull(levelButtons[i].Menu,
                    "El boton " + i + " no tiene MenuController al que avisar.");
            }
        }

        [Test]
        public void ElPanelDeNivelesEmpiezaOcultoYElMenuControllerLoConecta()
        {
            Scene scene = OpenMenu();

            MenuController controller = FindAll<MenuController>(scene).FirstOrDefault();
            Assert.IsNotNull(controller, "La escena deberia traer un MenuController.");

            GameObject panel = FindInScene(scene, "LevelsPanel");
            Assert.IsNotNull(panel, "Falta el panel de niveles (LevelsPanel).");
            Assert.IsFalse(panel.activeSelf,
                "El panel de niveles deberia empezar oculto y abrirse desde LevelsButton.");
        }

        [Test]
        public void LaEscenaTieneUnaCamaraPrincipal()
        {
            OpenMenu();

            Camera[] cameras = Camera.allCameras;
            Assert.AreEqual(1, cameras.Length,
                "El menu deberia tener una sola camara para que el Game View no diga 'No Camera rendering'.");

            Camera camera = cameras[0];
            Assert.AreEqual("MainCamera", camera.tag,
                "La camara del menu deberia estar etiquetada como principal.");
            Assert.IsTrue(camera.orthographic,
                "El menu es 2D: la camara debe ser ortografica.");
            Assert.NotNull(camera.GetComponent<AudioListener>(),
                "La camara deberia oir el audio del juego.");
        }

        [Test]
        public void LaRejillaDeNivelesCentraElBloqueConCualquierNumeroDeNiveles()
        {
            var cell = new Vector2(150f, 150f);
            var spacing = new Vector2(24f, 24f);
            var padding = new RectOffset();
            const int columns = 3;

            // Cuatro niveles: dos filas (tres arriba, uno abajo) y el bloque centrado
            // mide lo mismo que dos celdas completas mas su separacion.
            Vector2 four = LevelGridFitter.ComputeBlockSize(4, cell, spacing, padding, columns);
            Assert.AreEqual(new Vector2(498f, 324f), four);

            // Cinco niveles siguen siendo dos filas: el alto no crece hasta pasar de tres.
            Vector2 five = LevelGridFitter.ComputeBlockSize(5, cell, spacing, padding, columns);
            Assert.AreEqual(four.y, five.y, "Cinco niveles caben en dos filas, no deberia crecer.");

            // Siete niveles: tres filas, alto y ancho simetricos con la rejilla.
            Vector2 seven = LevelGridFitter.ComputeBlockSize(7, cell, spacing, padding, columns);
            Assert.AreEqual(new Vector2(498f, 498f), seven);

            // Sin niveles no hay bloque que centrar.
            Assert.AreEqual(Vector2.zero,
                LevelGridFitter.ComputeBlockSize(0, cell, spacing, padding, columns));
        }

        [Test]
        public void LosNivelesEstanEnUnaRejillaCentrada()
        {
            Scene scene = OpenMenu();

            LevelButton[] levelButtons = FindAll<LevelButton>(scene);
            Assert.Greater(levelButtons.Length, 0, "No hay ningun boton de nivel.");

            GridLayoutGroup grid = null;
            LevelGridFitter fitter = null;
            foreach (LevelButton button in levelButtons)
            {
                grid = button.GetComponentInParent<GridLayoutGroup>(true);
                fitter = button.GetComponentInParent<LevelGridFitter>(true);
                Assert.IsNotNull(grid,
                    button.name + " deberia vivir dentro de una rejilla para distribuir los niveles simetricos.");
                Assert.IsNotNull(fitter,
                    "Falta LevelGridFitter: la rejilla se quedaria anclada arriba a la izquierda.");
                break;
            }

            Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, grid.constraint,
                "La rejilla necesita columnas fijas: si no, el ancho cambia con cada nivel nuevo.");
            Assert.AreEqual(3, grid.constraintCount,
                "Tres columnas reparten los niveles en bloques simetricos.");

            var container = (RectTransform)grid.transform;
            Vector2 expected = LevelGridFitter.ComputeBlockSize(levelButtons.Length,
                grid.cellSize, grid.spacing, grid.padding, grid.constraintCount);

            Assert.AreEqual(expected.x, container.sizeDelta.x, 0.01f,
                "El contenedor no mide el ancho real de la rejilla: el bloque no quedaria centrado.");
            Assert.AreEqual(expected.y, container.sizeDelta.y, 0.01f,
                "El contenedor no mide el alto real de la rejilla: el bloque no quedaria centrado.");

            Assert.AreEqual(container.anchorMin, container.anchorMax,
                "El contenedor deberia anclarse a un unico punto para centrarse en la mesa.");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), container.anchorMin,
                "El contenedor deberia estar anclado al centro de la mesa.");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), container.pivot,
                "El pivote deberia ser el centro para que el tamano crezca hacia los dos lados.");
        }

        [Test]
        public void LaEscenaEstaRegistradaEnBuildSettingsComoPrimera()
        {
            bool found = false;
            int index = -1;
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (EditorBuildSettings.scenes[i].path == ScenePath)
                {
                    found = true;
                    index = i;
                    break;
                }
            }

            Assert.IsTrue(found, "Menu deberia estar en build settings para poder jugarla.");
            Assert.IsTrue(EditorBuildSettings.scenes[index].enabled,
                "Menu esta en build settings pero desactivada.");
            Assert.AreEqual(0, index,
                "Menu deberia ser la primera escena: al arrancar el juego se abre el menu.");
        }
    }
}