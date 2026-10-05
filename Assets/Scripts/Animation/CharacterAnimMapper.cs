using PixelIsland.Core;

namespace PixelIsland.Animation
{
    /// <summary>
    /// Traduce el estado de la maquina del jugador al estado de animacion.
    /// Es C# plano (no MonoBehaviour) para poder testear el mapeo entero en
    /// EditMode sin escena ni Animator.
    ///
    /// Solo hay cuatro estados y los cuatro clips de cada personaje. Si el
    /// Player gana un estado nuevo hay que anadirlo aqui Y al controller.
    /// </summary>
    public static class CharacterAnimMapper
    {
        /// <summary>
        /// Estado de animacion para un nombre de estado del Player. Devuelve
        /// false si el nombre no corresponde a ningun estado conocido, para que
        /// el driver pueda avisar en vez de animar en silencio.
        /// </summary>
        public static bool TryMap(string stateName, out CharacterAnimState result)
        {
            switch (stateName)
            {
                case nameof(CharacterAnimState.Idle):
                    result = CharacterAnimState.Idle;
                    return true;
                case nameof(CharacterAnimState.Run):
                    result = CharacterAnimState.Run;
                    return true;
                case nameof(CharacterAnimState.Jump):
                    result = CharacterAnimState.Jump;
                    return true;
                case nameof(CharacterAnimState.Fall):
                    result = CharacterAnimState.Fall;
                    return true;
                default:
                    result = CharacterAnimState.Idle;
                    return false;
            }
        }

        /// <summary>
        /// Mapea un IState del Player. Un estado desconocido cae en Idle en
        /// lugar de propagar un valor arbitrario al Animator.
        /// </summary>
        public static CharacterAnimState Map(IState state)
        {
            return TryMap(state != null ? state.Name : null, out CharacterAnimState result)
                ? result
                : CharacterAnimState.Idle;
        }
    }
}