using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Player
{
    /// <summary>
    /// Base de los estados del Explorador dentro del patron State.
    /// No es MonoBehaviour: guarda una referencia al PlayerController y decide
    /// sus propias transiciones, de modo que toda la logica de estados vive en
    /// esta jerarquia y es verificable con tests.
    /// </summary>
    public abstract class PlayerState : IState
    {
        protected readonly PlayerController Player;

        protected PlayerState(PlayerController player)
        {
            Player = player;
        }

        public abstract string Name { get; }

        public virtual void Enter()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }

        /// <summary>
        /// Prioridad de la transicion: si el motor disparo un salto en este paso,
        /// todos los estados pasan a Jump. Devuelve true si ya transiciono, para
        /// que el estado salga sin aplicar su propia logica.
        /// </summary>
        protected bool WentUpToJump()
        {
            if (!Player.JumpedThisStep)
            {
                return false;
            }

            Player.SetState(Player.JumpState);
            return true;
        }

        /// <summary>Estado de suelo al que se vuelve segun la intencion de movimiento.</summary>
        protected PlayerState GroundedState()
        {
            return Mathf.Abs(Player.MoveInput.MoveX) > 0.01f ? Player.RunState : Player.IdleState;
        }
    }

    /// <summary>En suelo, sin intencion de movimiento horizontal.</summary>
    public sealed class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerController player) : base(player)
        {
        }

        public override string Name => "Idle";

        public override void Tick(float deltaTime)
        {
            if (WentUpToJump())
            {
                return;
            }

            if (!Player.IsGrounded)
            {
                Player.SetState(Player.FallState);
                return;
            }

            if (Mathf.Abs(Player.MoveInput.MoveX) > 0.01f)
            {
                Player.SetState(Player.RunState);
            }
        }
    }

    /// <summary>En suelo desplazandose. Tambien se encarga de orientar el sprite.</summary>
    public sealed class PlayerRunState : PlayerState
    {
        public PlayerRunState(PlayerController player) : base(player)
        {
        }

        public override string Name => "Run";

        public override void Tick(float deltaTime)
        {
            if (WentUpToJump())
            {
                return;
            }

            if (!Player.IsGrounded)
            {
                Player.SetState(Player.FallState);
                return;
            }

            float moveX = Player.MoveInput.MoveX;
            if (Mathf.Abs(moveX) < 0.01f)
            {
                Player.SetState(Player.IdleState);
                return;
            }

            Player.FaceTowards(moveX);
        }
    }

    /// <summary>En ascension. Se abandona al tocar suelo o al terminar el impulso.</summary>
    public sealed class PlayerJumpState : PlayerState
    {
        public PlayerJumpState(PlayerController player) : base(player)
        {
        }

        public override string Name => "Jump";

        public override void Tick(float deltaTime)
        {
            if (Player.IsGrounded)
            {
                Player.SetState(GroundedState());
                return;
            }

            // Al cruzar el apex la velocidad vertical pasa a negativa: el impulso
            // ya se ha agotado y toca la fase de caida aunque no se haya tocado
            // suelo todavia. Sin esta distincion el estado pasaria por Idle un
            // fotograma en el aire.
            if (Player.Velocity.y <= 0f)
            {
                Player.SetState(Player.FallState);
            }
        }
    }

    /// <summary>En descenso. Al tocar suelo vuelve a Idle o Run.</summary>
    public sealed class PlayerFallState : PlayerState
    {
        public PlayerFallState(PlayerController player) : base(player)
        {
        }

        public override string Name => "Fall";

        public override void Tick(float deltaTime)
        {
            if (WentUpToJump())
            {
                return;
            }

            if (!Player.IsGrounded)
            {
                return;
            }

            Player.SetState(GroundedState());
        }
    }
}
