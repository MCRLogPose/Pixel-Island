namespace PixelIsland.Core
{
    /// <summary>
    /// Objetivo que puede recibir dano directo (p.ej. una criatura golpeada por un
    /// proyectil). A diferencia de <see cref="IDamageReaction"/>, que es solo el
    /// aviso de "me han herido", esta interfaz es la que decide si el impacto
    /// cuenta y aplica el dano.
    ///
    /// Vive en Core para que el sistema de proyectiles no dependa del assembly de
    /// enemigos: solo conoce la interfaz, y cada objetivo decide como muere.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Vida restante. 0 significa derrotado.</summary>
        int CurrentHealth { get; }

        /// <summary>True mientras le quede vida.</summary>
        bool IsAlive { get; }

        /// <summary>Aplica dano. Ignora cantidades no positivas y objetivos ya caidos.</summary>
        void TakeDamage(int amount);
    }
}
