using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Reaccion opcional de un objetivo tras recibir dano REAL (por ejemplo el
    /// rebote de dolor del Explorador al pincharse). Los hazards la invocan solo
    /// cuando <see cref="LivesComponent.TakeDamage"/> ha restado vidas de verdad,
    /// de modo que la ventana de invulnerabilidad tambien evita reacciones en cadena.
    /// </summary>
    public interface IDamageReaction
    {
        void OnDamaged(Vector2 sourcePosition);
    }
}
