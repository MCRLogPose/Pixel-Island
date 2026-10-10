using System.Linq;
using NUnit.Framework;
using PixelIsland.Levels;
using PixelIsland.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Cada zona guardada en disco debe traer el icono Home en su HUD, listo para
    /// volver al menu, y la infraestructura UI minima para que el clic llegue al
    /// boton (EventSystem + onClick persistente). Se valida la escena guardada y se
    /// restaura la que el usuario tuviera abierta.
    /// </summary>
    public sealed class HomeButtonSceneTests
    {
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

        static string[] Zonas() => LevelSequence.Scenes;

        [Test]
        public void CadaZonaTraeUnHomeButtonEnLaHud([ValueSource(nameof(Zonas))] string zona)
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/" + zona + ".unity", OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid() && scene.isLoaded, "No se pudo abrir " + zona);

            HomeButton[] homes = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<HomeButton>(true))
                .ToArray();

            Assert.AreEqual(1, homes.Length, zona + " deberia tener exactamente un HomeButton.");

            Button button = homes[0].GetComponent<Button>();
            Assert.IsNotNull(button, "El HomeButton de " + zona + " necesita un Button que reciba los clics.");

            Assert.AreEqual("HUD", homes[0].transform.parent.name,
                "El HomeButton de " + zona + " deberia vivir dentro de la HUD.");

            EventSystem[] eventSystems = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                .ToArray();
            Assert.AreEqual(1, eventSystems.Length,
                zona + " necesita un EventSystem para que el clic llegue al boton Home.");

            Assert.IsTrue(HomeButtonTieneOnClickPersistente(button, homes[0]),
                "El onClick del HomeButton de " + zona +
                " deberia llamar a ReturnToMenu desde el inspector, no solo por listener de runtime.");
        }

        static bool HomeButtonTieneOnClickPersistente(Button button, HomeButton home)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == home &&
                    button.onClick.GetPersistentMethodName(i) == "ReturnToMenu")
                {
                    return true;
                }
            }

            return false;
        }
    }
}