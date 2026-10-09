using NUnit.Framework;
using PixelIsland.Player;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Regla pura del rebote de dolor: direccion y sentido del empuje segun por
    /// donde llego el golpe. Se testea sin escena.
    /// </summary>
    public sealed class PlayerHurtBounceTests
    {
        const float Eps = 0.05f;

        [Test]
        public void GolpeDesdeLaIzquierda_EmpujaHaciaLaDerecha()
        {
            Vector2 impulse = PlayerHurtBounce.ComputeImpulse(Vector2.zero, 0f, -1f, false, 4f, 10f, Eps);

            Assert.Greater(impulse.x, 0f);
            Assert.AreEqual(10f, impulse.y, 0.0001f, "El brinco debe subir.");
        }

        [Test]
        public void GolpeDesdeLaDerecha_EmpujaHaciaLaIzquierda()
        {
            Vector2 impulse = PlayerHurtBounce.ComputeImpulse(Vector2.zero, 0f, 1f, false, 4f, 10f, Eps);

            Assert.Less(impulse.x, 0f);
        }

        [Test]
        public void GolpeCentradoEnMovimiento_RebotaContraElMovimiento()
        {
            Vector2 impulse = PlayerHurtBounce.ComputeImpulse(new Vector2(3f, 0f), 0f, 0f, false, 4f, 10f, Eps);

            Assert.Less(impulse.x, 0f,
                "Si se movia hacia la derecha, el rebote debe ir hacia la izquierda.");
        }

        [Test]
        public void GolpeCentradoEnReposo_RebotaHaciaAtrasSegunOrientacion()
        {
            Vector2 mirandoDerecha = PlayerHurtBounce.ComputeImpulse(Vector2.zero, 0f, 0f, false, 4f, 10f, Eps);
            Vector2 mirandoIzquierda = PlayerHurtBounce.ComputeImpulse(Vector2.zero, 0f, 0f, true, 4f, 10f, Eps);

            Assert.Less(mirandoDerecha.x, 0f);
            Assert.Greater(mirandoIzquierda.x, 0f);
        }
    }
}
