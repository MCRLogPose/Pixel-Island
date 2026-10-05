using NUnit.Framework;
using PixelIsland.Player;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Tests del lector de entrada. Update() depende de Input.GetKeyDown y no se
    /// puede ejercitar en un test, pero ReadInput() si: es el contrato entre el
    /// fotograma de render y el paso fisico, y es donde un struct se copia mal.
    ///
    /// MoveInput es un struct, asi que limpiar Sample.JumpPressed ANTES de
    /// devolverlo hace que el motor reciba siempre JumpPressed = false y el salto
    /// no llegue nunca a dispararse, aunque la pulsacion se haya leido bien.
    /// </summary>
    public sealed class PlayerInputReaderTests
    {
        PlayerInputReader _reader;

        [SetUp]
        public void SetUp()
        {
            _reader = new GameObject("PlayerInputReaderTests").AddComponent<PlayerInputReader>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_reader.gameObject);
        }

        /// <summary>
        /// Un flanco de salto debe llegar al paso fisico aunque se limpia el latch
        /// justo despues. Este es el test que falla con el orden invertido.
        /// </summary>
        [Test]
        public void ReadInput_DeliversJumpPressed_WhenTheLatchWasSet()
        {
            _reader.Sample = new MoveInput(0f, true, true, false);

            MoveInput delivered = _reader.ReadInput();

            Assert.IsTrue(delivered.JumpPressed,
                "ReadInput debe entregar el flanco de salto que Sample tenía.");
        }

        /// <summary>El resto de campos travels sin alterarse.</summary>
        [Test]
        public void ReadInput_DeliversMovementAndHeldFlags()
        {
            _reader.Sample = new MoveInput(-1f, false, true, true);

            MoveInput delivered = _reader.ReadInput();

            Assert.AreEqual(-1f, delivered.MoveX);
            Assert.IsTrue(delivered.JumpHeld);
            Assert.IsTrue(delivered.RunHeld);
        }

        /// <summary>
        /// El latch es de un solo uso: el paso fisico siguiente ya no debe ver el
        /// flanco, para que un toque no dispare dos saltos.
        /// </summary>
        [Test]
        public void ReadInput_ClearsTheLatch_AfterDeliveringIt()
        {
            _reader.Sample = new MoveInput(0f, true, true, false);

            _reader.ReadInput();
            MoveInput second = _reader.ReadInput();

            Assert.IsFalse(second.JumpPressed,
                "El flanco de salto no debe repetirse en el paso fisico siguiente.");
        }

        /// <summary>Sin pulsacion no hay nada que entregar.</summary>
        [Test]
        public void ReadInput_ReportsNoJump_WhenNothingWasPressed()
        {
            _reader.Sample = new MoveInput(1f, false, false, false);

            MoveInput delivered = _reader.ReadInput();

            Assert.IsFalse(delivered.JumpPressed);
            Assert.AreEqual(1f, delivered.MoveX);
        }
    }
}