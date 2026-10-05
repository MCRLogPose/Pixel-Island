using NUnit.Framework;
using PixelIsland.Environment;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Verifica la aritmetica del parallax sin necesidad de escena: como se acumula
    /// el desplazamiento de cada capa y como se envuelve para reaparecer por el
    /// lado opuesto en lugar de escaparse.
    /// </summary>
    public sealed class ParallaxLayerTests
    {
        const float Tile = 40f;

        [Test]
        public void WrapScroll_KeepsTheValueInsideHalfATile()
        {
            Assert.That(ParallaxLayer.WrapScroll(0f, Tile), Is.EqualTo(0f).Within(0.0001f));

            float half = Tile * 0.5f;
            for (float scroll = -Tile * 3f; scroll <= Tile * 3f; scroll += 3.7f)
            {
                float wrapped = ParallaxLayer.WrapScroll(scroll, Tile);
                Assert.That(wrapped, Is.GreaterThanOrEqualTo(-half - 0.0001f),
                    "El desplazamiento envuelto no puede pasar de -tile/2 (scroll=" + scroll + ")");
                Assert.That(wrapped, Is.LessThan(half + 0.0001f),
                    "El desplazamiento envuelto no puede pasar de +tile/2 (scroll=" + scroll + ")");
            }
        }

        [Test]
        public void WrapScroll_IsContinuousAcrossTheSeam()
        {
            // Justo antes y justo despues del punto de envoltura el resultado casi
            // coincide: si no, la capa daria un salto visible cada tile.
            const float Epsilon = 0.01f;
            float half = Tile * 0.5f;
            float before = ParallaxLayer.WrapScroll(half - Epsilon, Tile);
            float after = ParallaxLayer.WrapScroll(half + Epsilon, Tile);

            // Reaparece en el lado opuesto del intervalo...
            Assert.That(after, Is.LessThan(before),
                "Al cruzar la costura el valor debe reaparecer por el otro lado");

            // ...simetrico respecto a cero, que es lo que hace que el repetido encaje.
            Assert.That(after, Is.EqualTo(-before).Within(0.0001f));

            // Y el salto es de un tile entero menos la muestra: no se pierde
            // ningun tile por el camino.
            Assert.That(before - after, Is.EqualTo(Tile - Epsilon * 2f).Within(0.001f));
        }

        [Test]
        public void WrapScroll_WithZeroTileReturnsZero()
        {
            // Sin sprite asignado el ancho del tile es 0 y no debe aparecer un NaN
            // que se propagaria a la posicion de la capa.
            Assert.That(ParallaxLayer.WrapScroll(123f, 0f), Is.EqualTo(0f));
            Assert.That(ParallaxLayer.WrapScroll(123f, -5f), Is.EqualTo(0f));
        }

        [Test]
        public void AccumulateScroll_ZeroFactorNeverMoves()
        {
            // Factor 0 = capa pegada a la camara (cielo): no debe desplazarse nunca.
            float scroll = 0f;
            for (int i = 0; i < 20; i++)
            {
                scroll = ParallaxLayer.AccumulateScroll(scroll, 3f, 0f);
            }

            Assert.That(scroll, Is.EqualTo(0f));
        }

        [Test]
        public void AccumulateScroll_FullFactorFollowsTheWorld()
        {
            // Factor 1 = la capa se mueve tanto como el mundo, luego no se ve
            // desplazarse respecto al terreno.
            float scroll = ParallaxLayer.AccumulateScroll(0f, 2f, 1f);
            Assert.That(scroll, Is.EqualTo(2f).Within(0.0001f));

            scroll = ParallaxLayer.AccumulateScroll(scroll, -0.5f, 1f);
            Assert.That(scroll, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void AccumulateScroll_ClampsTheFactor()
        {
            // Aunque se serialice un factor fuera de rango por error, el
            // desplazamiento se limita a la velocidad de la camara.
            Assert.That(ParallaxLayer.AccumulateScroll(0f, 4f, 5f), Is.EqualTo(4f).Within(0.0001f));
            Assert.That(ParallaxLayer.AccumulateScroll(0f, 4f, -3f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void AccumulateScroll_HalfFactorIsHalfTheCameraTravel()
        {
            float scroll = 0f;
            for (int i = 0; i < 10; i++)
            {
                scroll = ParallaxLayer.AccumulateScroll(scroll, 2f, 0.5f);
            }

            Assert.That(scroll, Is.EqualTo(10f).Within(0.0001f));
        }
    }
}