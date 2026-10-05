using System;

namespace PixelIsland.Core
{
    /// <summary>
    /// Contrato de un estado del patron State. Las subclases son PLAIN C#,
    /// no MonoBehaviour: se testean en EditMode sin escena ni play mode.
    /// </summary>
    public interface IState
    {
        string Name { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    /// <summary>
    /// Maquina de estados del patron State. Encapsula Enter/Exit/Tick para que
    /// los owners (Player, Enemy) solo tengan que pedir cambios de estado.
    /// </summary>
    public sealed class StateMachine
    {
        IState _current;
        IState _previous;

        public IState Current => _current;
        public IState Previous => _previous;
        public string CurrentName => _current != null ? _current.Name : "<none>";
        public bool HasState => _current != null;

        /// <summary>Se dispara con (anterior, nuevo) en cada transicion efectiva.</summary>
        public event Action<IState, IState> StateChanged;

        public void ChangeState(IState next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (ReferenceEquals(next, _current))
            {
                return;
            }

            _previous = _current;
            _current?.Exit();
            _current = next;
            _current.Enter();
            StateChanged?.Invoke(_previous, _current);
        }

        public void Tick(float deltaTime)
        {
            _current?.Tick(deltaTime);
        }

        public void Reset()
        {
            _current?.Exit();
            _previous = null;
            _current = null;
        }
    }
}
