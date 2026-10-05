using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PixelIsland.Core;
using PixelIsland.Environment;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixelIsland.Tests.PlayMode
{
    /// <summary>
    /// Tests de integracion del parallax dentro del bucle real de Unity.
    ///
    /// Los EditMode tests verifican AccumulateScroll, WrapScroll y el cableado de la
    /// escena guardada con logica pura; estos comprueban lo que solo se ve en play:
    /// que la capa siga a la camara mientras esta se mueve, que arrastre su fraccion
    /// exacta, que el quad cubra la vista y que el envoltado no deje huecos tras un
    /// recorrido largo.
    ///
    /// Montan su propio escenario con una camara ortografica y una pila de capas, sin
    /// cargar ForestLevel: una escena con suelo y jugador contaminaria los tests de
    /// movimiento, que comparten el mundo de la fisica.
    /// </summary>
    public sealed class ParallaxLayerPlayModeTests
    {
        /// <summary>Vista de referencia: 640x360 a 32 PPU.</summary>
        const float ViewHeight = 11.25f;
        const float ViewWidth = 20f;

        GameObject _root;
        Camera _camera;
        readonly List<ParallaxLayer> _layers = new List<ParallaxLayer>();
        readonly List<float> _startRelativeX = new List<float>();

        /// <summary>Mismos factores que ForestLevel, de lejos a cerca.</summary>
        static readonly float[] Factors = { 0.05f, 0.20f, 0.35f, 0.55f, 0.85f };

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ParallaxTestRig");

            GameObject cameraGo = new GameObject("TestCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 3f, -10f);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = ViewHeight * 0.5f;
            _camera.aspect = ViewWidth / ViewHeight;

            for (int i = 0; i < Factors.Length; i++)
            {
                _layers.Add(CreateLayer(i, Factors[i]));
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ParallaxLayer layer in _layers)
            {
                if (layer != null)
                {
                    Object.Destroy(layer.gameObject);
                }
            }

            _layers.Clear();

            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }

            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }

        /// <summary>
        /// Crea una capa con un sprite del mismo tamano que las imagenes reales
        /// (320x180 a 16 PPU = 20x11.25 u), que es lo unico que necesita el motor
        /// para el envoltado y el dimensionado.
        /// </summary>
        ParallaxLayer CreateLayer(int index, float factor)
        {
            Texture2D texture = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            texture.name = "StandIn" + index;
            // Un degradado basta: lo que se comprueba es la geometria, no el arte.
            for (int y = 0; y < 180; y++)
            {
                for (int x = 0; x < 320; x++)
                {
                    texture.SetPixel(x, y, new Color((float)x / 320f, (float)y / 180f, 0.5f, 1f));
                }
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 320f, 180f),
                new Vector2(0.5f, 0.5f), 16f);
            sprite.name = "StandIn" + index;

            GameObject go = new GameObject("Layer_" + index);
            go.layer = GameLayers.Id(GameLayers.ParallaxBackground);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = new Vector3(0f, 3f, 0f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100 + index;

            ParallaxLayer layer = go.AddComponent<ParallaxLayer>();
            layer.Factor = factor;
            layer.VerticalFactor = 0f;
            layer.TargetCamera = _camera;
            return layer;
        }

        /// <summary>
        /// Posicion de una capa RELATIVA a la camara: es lo que el ojo percibe y lo
        /// que hace comparables capas con factores distintos.
        /// </summary>
        float RelativeX(ParallaxLayer layer) => layer.transform.position.x - _camera.transform.position.x;

        IEnumerator MoveCameraTo(float x)
        {
            _startRelativeX.Clear();
            foreach (ParallaxLayer layer in _layers)
            {
                _startRelativeX.Add(RelativeX(layer));
            }

            _camera.transform.position = new Vector3(x, 3f, -10f);

            // Un par de fotogramas: Start dimensiona el quad y LateUpdate acumula.
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CadaCapaArrastraSuFraccionDelRecorridoDeLaCamara()
        {
            // 5 u de recorrido: con los factores mas altos eso son 4.25 u de arrastre,
            // muy por debajo de medio tile, asi que todavia nadie ha dado la vuelta y el
            // desplazamiento se puede comparar con el puro.
            const float Travel = 5f;
            yield return MoveCameraTo(Travel);

            for (int i = 0; i < _layers.Count; i++)
            {
                ParallaxLayer layer = _layers[i];
                float drift = RelativeX(layer) - _startRelativeX[i];
                float measured = drift / Travel;

                Assert.AreEqual(layer.Factor, measured, 0.001f,
                    "La capa " + layer.name + " arrastro el " + (measured * 100f).ToString("0.##") +
                    " % del recorrido de la camara, pero su factor es " +
                    (layer.Factor * 100f).ToString("0.##") + " %.");
            }
        }

        [UnityTest]
        public IEnumerator LasCapasMasCercaSeSeparanMasDeLaCamara()
        {
            const float Travel = 5f;
            yield return MoveCameraTo(Travel);

            for (int i = 1; i < _layers.Count; i++)
            {
                float driftPrevious = Mathf.Abs(RelativeX(_layers[i - 1]) - _startRelativeX[i - 1]);
                float driftCurrent = Mathf.Abs(RelativeX(_layers[i]) - _startRelativeX[i]);

                Assert.Greater(driftCurrent, driftPrevious,
                    _layers[i].name + " esta mas cerca que " + _layers[i - 1].name +
                    " pero se ha separado menos: no se vera la profundidad.");
            }
        }

        [UnityTest]
        public IEnumerator TrasUnRecorridoLargoCadaCapaSoloSeAdelantaEnTilesEnteros()
        {
            // Al recorrer 400 u el arrastre real de una capaClose puede valer 340 u, y
            // lo que se ve son 340 menos un numero entero de tiles. Lo que NO puede
            // pasar es que se desvíe una fraccion de tile: eso seria un descuadre.
            const float Travel = 400f;
            yield return MoveCameraTo(Travel);

            float tile = _layers[0].TileSize.x;
            Assert.Greater(tile, 0f, "El sprite de apoyo deberia tener ancho.");

            for (int i = 0; i < _layers.Count; i++)
            {
                ParallaxLayer layer = _layers[i];
                float drift = RelativeX(layer) - _startRelativeX[i];
                float expected = Travel * layer.Factor;

                // expected - drift debe ser multiplo del ancho del tile.
                float tiles = (expected - drift) / tile;
                float nearest = Mathf.Round(tiles);

                Assert.AreEqual(nearest, tiles, 0.001f,
                    "La capa " + layer.name + " se ha desviado " + (expected - drift).ToString("0.###") +
                    " u, que no es un numero entero de tiles de " + tile.ToString("0.##") + " u.");
            }
        }

        [UnityTest]
        public IEnumerator ElQuadCubreLaVistaJustoAlEmpezar()
        {
            yield return null;

            foreach (ParallaxLayer layer in _layers)
            {
                Vector2 quad = layer.Renderer.size;
                Assert.GreaterOrEqual(quad.x, ViewWidth,
                    layer.name + " es mas estrecho que la pantalla.");
                Assert.GreaterOrEqual(quad.y, ViewHeight,
                    layer.name + " es mas bajo que la pantalla.");
            }
        }

        [UnityTest]
        public IEnumerator ElQuadSeRedimensionaSiLaVentanaCambiaDeProporcion()
        {
            // Una ventana mas ancha que la de referencia: si el quad no se
            // redimensiona en runtime se ve el borde del tile.
            _camera.aspect = 3.04f;
            yield return null;

            float viewWidth = ViewHeight * _camera.aspect;

            foreach (ParallaxLayer layer in _layers)
            {
                Assert.GreaterOrEqual(layer.Renderer.size.x, viewWidth,
                    layer.name + " no se ha redimensionado para la nueva proporcion de aspecto.");
            }
        }

        [UnityTest]
        public IEnumerator ElEnvoltadoNoDejaHuecosTrasUnRecorridoLargo()
        {
            // Al fondo del recorrido el desplazamiento acumulado ha dado vueltas
            // muchas veces: es donde un seam mal hecho se delata.
            yield return MoveCameraTo(400f);

            Vector3 centre = _camera.transform.position;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            foreach (ParallaxLayer layer in _layers)
            {
                Bounds bounds = layer.Renderer.bounds;

                Assert.LessOrEqual(bounds.min.x, centre.x - halfWidth + 0.01f,
                    layer.name + " deja un hueco a la izquierda.");
                Assert.GreaterOrEqual(bounds.max.x, centre.x + halfWidth - 0.01f,
                    layer.name + " deja un hueco a la derecha.");
                Assert.LessOrEqual(bounds.min.y, centre.y - halfHeight + 0.01f,
                    layer.name + " deja un hueco por abajo.");
                Assert.GreaterOrEqual(bounds.max.y, centre.y + halfHeight - 0.01f,
                    layer.name + " deja un hueco por arriba.");
            }
        }

        [UnityTest]
        public IEnumerator LaCamaraQueSubeNoMueveLasCapasEnVertical()
        {
            yield return MoveCameraTo(0f);

            _camera.transform.position = new Vector3(0f, 9f, -10f);
            yield return null;
            yield return null;

            for (int i = 0; i < _layers.Count; i++)
            {
                Assert.AreEqual(9f, _layers[i].transform.position.y, 0.001f,
                    _layers[i].name + " deberia seguir a la camara en vertical (factor vertical 0).");
            }
        }
    }
}