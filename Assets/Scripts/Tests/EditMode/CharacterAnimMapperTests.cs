using PixelIsland.Animation;
using NUnit.Framework;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// El mapeo Player -> animacion es el contrato que comparten Character1 y
    /// Character2, asi que se testea sin escena ni Animator.
    /// </summary>
    public class CharacterAnimMapperTests
    {
        [TestCase("Idle", CharacterAnimState.Idle)]
        [TestCase("Run", CharacterAnimState.Run)]
        [TestCase("Jump", CharacterAnimState.Jump)]
        [TestCase("Fall", CharacterAnimState.Fall)]
        public void LosNombresDelPlayerMapeanAEstadoDeAnimacion(string stateName, CharacterAnimState expected)
        {
            bool mapped = CharacterAnimMapper.TryMap(stateName, out CharacterAnimState result);

            Assert.IsTrue(mapped);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void LosIndicesCoincidenConElParametroIntDelController()
        {
            // Los controllers se generaron con estos valores en las condiciones
            // de transicion. Si el enum se reordena, ambos controllers fallan.
            Assert.AreEqual(0, (int)CharacterAnimState.Idle);
            Assert.AreEqual(1, (int)CharacterAnimState.Run);
            Assert.AreEqual(2, (int)CharacterAnimState.Jump);
            Assert.AreEqual(3, (int)CharacterAnimState.Fall);
        }

        [Test]
        public void UnNombreDesconocidoNoSeMapea()
        {
            bool mapped = CharacterAnimMapper.TryMap("Dash", out _);

            Assert.IsFalse(mapped, "Un estado sin animacion debe avisar en vez de animar en silencio.");
        }

        [Test]
        public void UnNombreNuloNoSeMapea()
        {
            Assert.IsFalse(CharacterAnimMapper.TryMap(null, out _));
        }

        [Test]
        public void UnNombreDesconocidoCaeEnIdleParaNoPropagarBasuraAlAnimator()
        {
            CharacterAnimMapper.TryMap("Dash", out CharacterAnimState result);

            Assert.AreEqual(CharacterAnimState.Idle, result);
        }

        [Test]
        public void MapConEstadoNuloDevuelveIdle()
        {
            Assert.AreEqual(CharacterAnimState.Idle, CharacterAnimMapper.Map(null));
        }
    }
}