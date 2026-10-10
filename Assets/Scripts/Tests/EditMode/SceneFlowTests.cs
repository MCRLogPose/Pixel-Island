using NUnit.Framework;
using PixelIsland.Levels;
using UnityEditor;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// El "ganar" y el boton Home vuelven al menu por <see cref="SceneFlow"/>: se
    /// comprueba que la escena de menu existe en Build Settings y que el nombre no
    /// se improvisa.
    /// </summary>
    public sealed class SceneFlowTests
    {
        [Test]
        public void ElMenuEstaEnBuildSettingsBajoSuNombre()
        {
            string expected = "Assets/Scenes/" + SceneFlow.MenuSceneName + ".unity";
            bool found = false;

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == expected && scene.enabled)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found,
                "La escena de menu '" + expected + "' deberia estar habilitada en Build Settings.");
        }

        [Test]
        public void IsMenuSoloReconoceElMenu()
        {
            Assert.IsTrue(SceneFlow.IsMenu(SceneFlow.MenuSceneName));
            Assert.IsFalse(SceneFlow.IsMenu("BeachZone"));
            Assert.IsFalse(SceneFlow.IsMenu(null));
        }

        [Test]
        public void LaUltimaZonaEsLaQueGanaElJuego()
        {
            string[] zonas = LevelSequence.Scenes;
            Assert.IsTrue(LevelExit.IsFinal(zonas[zonas.Length - 1]),
                "La ultima zona deberia disparar la vuelta al menu.");
            Assert.IsFalse(LevelExit.IsFinal(zonas[0]),
                "La primera zona no deberia darse por terminada: carga la siguiente.");
        }
    }
}
