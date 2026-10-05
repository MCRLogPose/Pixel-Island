using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Matematica del salto en pixeles/unidades. El motor define la altura
    /// alcanzada (no la velocidad de salida), de modo que el tuning es legible:
    /// "salta 2.75 unidades de alto" en vez de "vy = 13.6".
    /// </summary>
    public static class JumpMath
    {
        /// <summary>Velocidad vertical inicial necesaria para alcanzar <paramref name="height"/>.</summary>
        public static float VelocityForHeight(float height, float gravity)
        {
            if (height <= 0f || gravity <= 0f)
            {
                return 0f;
            }

            return Mathf.Sqrt(2f * height * gravity);
        }

        /// <summary>Altura maxima teorica alcanzada con una velocidad inicial dada.</summary>
        public static float PeakHeight(float velocity, float gravity)
        {
            if (velocity <= 0f || gravity <= 0f)
            {
                return 0f;
            }

            return velocity * velocity / (2f * gravity);
        }

        /// <summary>Tiempo que tarda en alcanzarse el apice del salto.</summary>
        public static float TimeToApex(float velocity, float gravity)
        {
            if (gravity <= 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, velocity) / gravity;
        }
    }
}
