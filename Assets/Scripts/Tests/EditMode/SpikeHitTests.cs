using NUnit.Framework;
using PixelIsland.Hazards;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    public class SpikeHitTests
    {
        const float Top = 5f;

        [Test]
        public void NormalVerticalYPiesSobreLaPunta_EsPisotazo()
        {
            Assert.IsTrue(SpikeHit.IsTopHit(Vector2.up, Top, Top));
            Assert.IsTrue(SpikeHit.IsTopHit(Vector2.down, Top, Top));
        }

        [Test]
        public void NormalHorizontalAunqueLlegueArriba_NoEsPisotazo()
        {
            Assert.IsFalse(SpikeHit.IsTopHit(Vector2.right, Top, Top));
            Assert.IsFalse(SpikeHit.IsTopHit(Vector2.left, Top, Top));
        }

        [Test]
        public void PiesPorDebajoDeLaPunta_NoEsPisotazo()
        {
            Assert.IsFalse(SpikeHit.IsTopHit(Vector2.up, Top, Top - 0.5f));
        }

        [Test]
        public void PiesDentroDeLaTolerancia_EsPisotazo()
        {
            Assert.IsTrue(SpikeHit.IsTopHit(Vector2.up, Top, Top - 0.05f, 1f, 0.1f));
            Assert.IsFalse(SpikeHit.IsTopHit(Vector2.up, Top, Top - 0.2f, 1f, 0.1f));
        }

        [Test]
        public void NormalDiagonalEmpinada_EsPisotazo()
        {
            Assert.IsTrue(SpikeHit.IsTopHit(new Vector2(0.5f, 1f).normalized, Top, Top));
        }

        [Test]
        public void NormalDiagonalTendida_NoEsPisotazo()
        {
            Assert.IsFalse(SpikeHit.IsTopHit(new Vector2(1f, 0.5f).normalized, Top, Top));
        }
    }
}
