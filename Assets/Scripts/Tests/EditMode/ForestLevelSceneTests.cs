using NUnit.Framework;
using PixelIsland.Core;
using PixelIsland.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Verifica la escena ForestLevel tal y como queda GUARDADA en disco: que el
    /// generador cablee las cinco capas en el orden correcto y con el factor que le
    /// toca, y que la vista de referencia quede cubierta entera.
    ///
    /// El comportamiento en runtime lo cubren los PlayMode tests de ParallaxLayer,
    /// que montan su propio escenario. Aqui no se abre la escena en play precisamente
    /// para no meter su suelo y su jugador en el mundo de la fisica de los demas
    /// tests.
    /// </summary>
    public sealed class ForestLevelSceneTests
    {
        const string ScenePath = "Assets/Scenes/ForestLevel.unity";

        /// <summary>Vista de referencia: 640x360 a 32 PPU.</summary>
        const float ViewHeight = 11.25f;
        const float ViewWidth = 20f;

        SceneSetup[] _previousSetup;

        [SetUp]
        public void SetUp()
        {
            // Guardar el montaje de escenas para no dejar al usuario otra escena
            // abierta tras correr los tests.
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

        static Scene OpenForestLevel()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "No se pudo abrir " + ScenePath);
            Assert.IsTrue(scene.isLoaded, "No se pudo cargar " + ScenePath);
            return scene;
        }

        static ParallaxLayer[] LayersInDepthOrder()
        {
            ParallaxLayer[] layers = Object.FindObjectsOfType<ParallaxLayer>();
            System.Array.Sort(layers,
                (a, b) => a.Renderer.sortingOrder.CompareTo(b.Renderer.sortingOrder));
            return layers;
        }

        [Test]
        public void LaEscenaTieneCincoCapasOrdenadasDeLejosACerca()
        {
            OpenForestLevel();

            ParallaxLayer[] layers = LayersInDepthOrder();
            Assert.AreEqual(5, layers.Length, "ForestLevel deberia traer las cinco imagenes de fondo.");

            for (int i = 0; i < layers.Length; i++)
            {
                ParallaxLayer layer = layers[i];

                Assert.AreEqual(GameLayers.Id(GameLayers.ParallaxBackground), layer.gameObject.layer,
                    layer.name + " deberia estar en la capa " + GameLayers.ParallaxBackground + ".");

                Assert.AreEqual(0f, layer.VerticalFactor, 0.0001f,
                    layer.name + " deberia quedarse fija en vertical.");

                Assert.GreaterOrEqual(layer.Factor, 0f, layer.name);
                Assert.Less(layer.Factor, 1f,
                    layer.name + " con factor 1 no se separaria de la camara.");

                if (i > 0)
                {
                    ParallaxLayer previous = layers[i - 1];

                    Assert.Less(previous.Renderer.sortingOrder, layer.Renderer.sortingOrder,
                        layer.name + " se dibuja detras de " + previous.name +
                        " pese a estar mas cerca.");

                    Assert.Less(previous.Factor, layer.Factor,
                        layer.name + " esta mas cerca que " + previous.name +
                        " pero tiene un factor de parallax menor: se veria al reves.");
                }
            }
        }

[Test]
        public void ElCieloEsLaCapaQueMasLentaSeMueveYLaMasOpaca()
        {
            OpenForestLevel();

            ParallaxLayer[] layers = LayersInDepthOrder();

            ParallaxLayer sky = layers[0];
            Assert.AreEqual("BG_Cielo", sky.name,
                "La primera capa por orden de dibujo deberia ser el cielo.");
            Assert.Less(sky.Factor, 0.1f,
                "El cielo es lo mas lejano: apenas debeacompanar a la camara.");

            // El cielo es la unica capa que tapa del todo, asi que no necesita canal
            // alfa: el PNG es opaco de nacimiento y Unity lo sube sin transparency.
            // Si se le pidiera alfa seria memoria desperdiciada.
            string path = AssetDatabase.GetAssetPath(sky.Renderer.sprite);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            Assert.IsNotNull(importer, "No se encontro el importador de " + path);
            Assert.IsFalse(importer.DoesSourceTextureHaveAlpha(),
                "El cielo es opaco: no deberia gastar memoria con un canal alfa vacio.");

            // Las capas de encima si son transparentes, y de ahi depende que se vea el
            // cielo a traves de ellas.
            for (int i = 1; i < layers.Length; i++)
            {
                string layerPath = AssetDatabase.GetAssetPath(layers[i].Renderer.sprite);
                TextureImporter layerImporter = (TextureImporter)AssetImporter.GetAtPath(layerPath);

                Assert.IsTrue(layerImporter.DoesSourceTextureHaveAlpha(),
                    layerPath + " se dibuja sobre el cielo, asi que necesita canal alfa.");

                Assert.AreEqual(FilterMode.Point, layerImporter.filterMode,
                    layerPath + " deberia importarse sin filtrado: es pixel art.");

                Assert.AreEqual(TextureImporterCompression.Uncompressed, layerImporter.textureCompression,
                    layerPath + " no deberia comprimirse: el pixel art se ensucia.");

                Assert.IsFalse(layerImporter.mipmapEnabled,
                    layerPath + " no necesita mipmaps a esta resolucion.");
            }
        }

        [Test]
        public void CadaCapaCubreLaVistaDeReferenciaCompleta()
        {
            OpenForestLevel();

            foreach (ParallaxLayer layer in LayersInDepthOrder())
            {
                Vector2 quad = layer.Renderer.size;

                Assert.GreaterOrEqual(quad.x, ViewWidth,
                    layer.name + " es mas estrecho que la vista de referencia.");

                Assert.GreaterOrEqual(quad.y, ViewHeight,
                    layer.name + " es mas bajo que la vista de referencia.");
            }
        }

        [Test]
        public void CadaCapaUsaSuImagenSinRepetirlaEnVertical()
        {
            OpenForestLevel();

            foreach (ParallaxLayer layer in LayersInDepthOrder())
            {
                Assert.IsNotNull(layer.Renderer.sprite, layer.name + " no tiene imagen asignada.");
                Assert.AreEqual(TextureWrapMode.Repeat, layer.Renderer.sprite.texture.wrapMode,
                    layer.name + " necesita wrapMode Repeat para que el repetido en horizontal no tenga costura.");

                // Con el factor vertical a 0 y la imagen cubriendo la altura, el alto
                // del quad debe ser el de un solo tile: si no, el motivo se repite en
                // bandas horizontales.
                Assert.AreEqual(layer.TileSize.y, layer.Renderer.size.y, 0.01f,
                    layer.name + " se esta repitiendo en vertical; con factor vertical 0 se veria el doble de motivo.");
            }
        }

        [Test]
        public void LaCamaraEsOrtograficaYPersigueAlExplorador()
        {
            OpenForestLevel();

            Camera[] cameras = Camera.allCameras;
            Assert.AreEqual(1, cameras.Length, "ForestLevel deberia tener una sola camara.");

            Camera camera = cameras[0];
            Assert.IsTrue(camera.orthographic, "La camara del parallax tiene que ser ortografica.");
            Assert.AreEqual(ViewHeight * 0.5f, camera.orthographicSize, 0.01f,
                "El alto de la vista no cuadra con la referencia 640x360 a 32 PPU.");

            CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
            Assert.IsNotNull(follow, "La camara necesita CameraFollow2D para Revealer al Explorador.");

            GameObject player = GameObject.Find("Player");
            Assert.IsNotNull(player, "ForestLevel deberia traer al Explorador para poder probar el parallax.");
            Assert.AreEqual(GameLayers.PlayerId, player.layer,
                "El Explorador deberia estar en la capa " + GameLayers.Player + ".");
        }

        [Test]
        public void ElExploradorAterrizaJustoSobreLaSuperficie()
        {
            OpenForestLevel();

            GameObject player = GameObject.Find("Player");
            Assert.IsNotNull(player, "ForestLevel deberia traer al Explorador.");

            CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();
            Assert.IsNotNull(capsule, "El Explorador necesita su collider para apoyarse.");

            // El pivote del sprite es abajo-centro, luego los pies del collider estan
            // en transform.y + offset.y - size.y/2. Debe coincidir con la parte alta
            // de Ground_Surface, o el sensor de suelo lo separa del suelo.
            float feet = player.transform.position.y + capsule.offset.y - capsule.size.y * 0.5f;

            BoxCollider2D surface = GameObject.Find("Ground_Surface").GetComponent<BoxCollider2D>();
            float top = surface.transform.position.y + surface.offset.y + surface.size.y * 0.5f;

            Assert.AreEqual(top, feet, 0.02f,
                "Los pies del Explorador no coinciden con la superficie: quedaria flotando o enterrado.");
        }

        [Test]
        public void LaEscenaEstaRegistradaEnBuildSettings()
        {
            bool found = false;
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry.path == ScenePath)
                {
                    found = true;
                    Assert.IsTrue(entry.enabled, "ForestLevel esta en build settings pero desactivada.");
                }
            }

            Assert.IsTrue(found, "ForestLevel deberia estar en build settings para poder jugarla.");
        }

        [Test]
        public void LasImagenesDeFondoSiguenEnLaCarpetaDeArte()
        {
            // La carpeta de origen quedo vacia cuando se movieron las imagenes.
            string destination = "Assets/Art/Environment/Background";

            foreach (string file in new[] { "Background1", "Background2", "Background3", "Background4", "Background5" })
            {
                string path = destination + "/" + file + ".png";
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path),
                    "Falta " + path + ". Las imagenes deben vivir en la carpeta de arte del proyecto.");
            }
        }
    }
}