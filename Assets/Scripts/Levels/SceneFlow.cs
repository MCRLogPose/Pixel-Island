using UnityEngine.SceneManagement;

namespace PixelIsland.Levels
{
    /// <summary>
    /// Unico sitio donde vive el nombre de la escena de menu. Tanto la bandera final
    /// (que vuelve al menu tras ganar) como el boton Home de la HUD de cada zona
    /// cargan el menu a traves de aqui, para no repetir el string en varios sitios.
    /// </summary>
    public static class SceneFlow
    {
        public const string MenuSceneName = "Menu";

        public static bool IsMenu(string sceneName) => sceneName == MenuSceneName;

        /// <summary>Carga la escena del menu principal.</summary>
        public static void LoadMenu() => SceneManager.LoadScene(MenuSceneName);
    }
}
