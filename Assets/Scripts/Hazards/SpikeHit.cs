using UnityEngine;

namespace PixelIsland.Hazards
{
    /// <summary>
    /// Regla pura de los pinchos: solo hieren cuando el objetivo cae sobre la punta.
    /// Un roce de costado (normal horizontal) empuja pero no resta vidas.
    /// Se aisla del MonoBehaviour para poder testearla en EditMode sin escena.
    /// </summary>
    public static class SpikeHit
    {
        public const float DefaultVerticalRatio = 1f;
        public const float DefaultFeetTolerance = 0.1f;

        /// <summary>
        /// True si el contacto es un pisotazo sobre la punta: la normal es
        /// esencialmente vertical (no lateral) y los pies del objetivo llegan a la
        /// altura de la punta, o por encima, dentro de <paramref name="feetTolerance"/>.
        /// </summary>
        public static bool IsTopHit(
            Vector2 contactNormal,
            float spikeTopY,
            float targetFeetY,
            float verticalRatio = DefaultVerticalRatio,
            float feetTolerance = DefaultFeetTolerance)
        {
            if (targetFeetY < spikeTopY - feetTolerance)
            {
                return false;
            }

            return Mathf.Abs(contactNormal.y) >= verticalRatio * Mathf.Abs(contactNormal.x);
        }
    }
}
