using NUnit.Framework;
using PixelIsland.Levels;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Tests de la logica pura de <see cref="LevelSequence"/> (sin cargar escenas).
    /// </summary>
    public sealed class LevelSequenceTests
    {
        [Test]
        public void ElOrdenEmpiezaEnPlayaYTerminaEnElTemplo()
        {
            string[] scenes = LevelSequence.Scenes;

            Assert.AreEqual("BeachZone", scenes[0]);
            Assert.AreEqual("ForestZone", scenes[1]);
            Assert.AreEqual("CaveZone", scenes[2]);
            Assert.AreEqual("AcienttempleZone", scenes[3]);
        }

        [Test]
        public void CadaZonaApuntaALaSiguiente()
        {
            Assert.AreEqual("ForestZone", LevelSequence.Next("BeachZone"));
            Assert.AreEqual("CaveZone", LevelSequence.Next("ForestZone"));
            Assert.AreEqual("AcienttempleZone", LevelSequence.Next("CaveZone"));
        }

        [Test]
        public void LaUltimaZonaNoTieneSiguiente()
        {
            Assert.IsTrue(LevelSequence.IsLast("AcienttempleZone"));
            Assert.IsNull(LevelSequence.Next("AcienttempleZone"));
        }

        [Test]
        public void UnaZonaDesconocidaNoTieneSiguienteNiEsLaUltima()
        {
            Assert.AreEqual(-1, LevelSequence.IndexOf("Narnia"));
            Assert.IsNull(LevelSequence.Next("Narnia"));
            Assert.IsFalse(LevelSequence.IsLast("Narnia"));
        }

        [Test]
        public void NombreVacioNoTieneSiguiente()
        {
            Assert.IsNull(LevelSequence.Next(null));
            Assert.IsNull(LevelSequence.Next(""));
        }

        [Test]
        public void SoloLaUltimaZonaSeDetectaComoFinal()
        {
            Assert.IsTrue(LevelExit.IsFinal("AcienttempleZone"));
            Assert.IsFalse(LevelExit.IsFinal("BeachZone"));
            Assert.IsFalse(LevelExit.IsFinal("CaveZone"));
            Assert.IsFalse(LevelExit.IsFinal(null));
            Assert.IsFalse(LevelExit.IsFinal("Narnia"));
        }
    }
}
