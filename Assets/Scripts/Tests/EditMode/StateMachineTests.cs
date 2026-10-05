using System;
using NUnit.Framework;
using PixelIsland.Core;

namespace PixelIsland.Tests.EditMode
{
    public sealed class StateMachineTests
    {
        sealed class SpyState : IState
        {
            readonly string _name;

            public SpyState(string name)
            {
                _name = name;
            }

            public string Name => _name;
            public int Enters { get; private set; }
            public int Exits { get; private set; }
            public int Ticks { get; private set; }
            public float LastDeltaTime { get; private set; }

            public void Enter() => Enters++;
            public void Tick(float deltaTime)
            {
                Ticks++;
                LastDeltaTime = deltaTime;
            }

            public void Exit() => Exits++;
        }

        [Test]
        public void NewMachine_HasNoState()
        {
            StateMachine machine = new StateMachine();

            Assert.IsFalse(machine.HasState);
            Assert.IsNull(machine.Current);
            Assert.AreEqual("<none>", machine.CurrentName);
        }

        [Test]
        public void ChangeState_EntersNewStateAndExitsPrevious()
        {
            StateMachine machine = new StateMachine();
            SpyState a = new SpyState("A");
            SpyState b = new SpyState("B");

            machine.ChangeState(a);
            Assert.AreEqual(1, a.Enters);
            Assert.AreEqual(0, a.Exits);

            machine.ChangeState(b);

            Assert.AreEqual(1, a.Exits, "El estado anterior debe salir al cambiar.");
            Assert.AreEqual(1, b.Enters, "El estado nuevo debe entrar al cambiar.");
            Assert.AreSame(b, machine.Current);
            Assert.AreSame(a, machine.Previous);
            Assert.AreEqual("B", machine.CurrentName);
        }

        [Test]
        public void ChangeState_ToSameState_IsIgnored()
        {
            StateMachine machine = new StateMachine();
            SpyState a = new SpyState("A");
            machine.ChangeState(a);

            machine.ChangeState(a);

            Assert.AreEqual(1, a.Enters, "No debe reentrar en el mismo estado.");
            Assert.AreEqual(0, a.Exits, "No debe salir del mismo estado.");
        }

        [Test]
        public void Tick_ForwardsToCurrentStateOnly()
        {
            StateMachine machine = new StateMachine();
            SpyState a = new SpyState("A");
            SpyState b = new SpyState("B");
            machine.ChangeState(a);

            machine.Tick(0.02f);
            machine.ChangeState(b);
            machine.Tick(0.03f);

            Assert.AreEqual(1, a.Ticks);
            Assert.AreEqual(1, b.Ticks);
            Assert.AreEqual(0.03f, b.LastDeltaTime, 0.0001f);
        }

        [Test]
        public void Tick_WithNoState_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new StateMachine().Tick(0.02f));
        }

        [Test]
        public void StateChanged_FiresWithPreviousAndNext()
        {
            StateMachine machine = new StateMachine();
            SpyState a = new SpyState("A");
            SpyState b = new SpyState("B");
            IState reportedFrom = null;
            IState reportedTo = null;
            int calls = 0;

            machine.StateChanged += (from, to) =>
            {
                calls++;
                reportedFrom = from;
                reportedTo = to;
            };

            machine.ChangeState(a);
            Assert.AreEqual(1, calls);
            Assert.IsNull(reportedFrom);
            Assert.AreSame(a, reportedTo);

            machine.ChangeState(b);
            Assert.AreEqual(2, calls);
            Assert.AreSame(a, reportedFrom);
            Assert.AreSame(b, reportedTo);

            machine.ChangeState(b);
            Assert.AreEqual(2, calls, "No debe emitir si el estado no cambia.");
        }

        [Test]
        public void ChangeState_Null_Throws()
        {
            StateMachine machine = new StateMachine();

            Assert.Throws<ArgumentNullException>(() => machine.ChangeState(null));
        }

        [Test]
        public void Reset_ExitsCurrentAndClears()
        {
            StateMachine machine = new StateMachine();
            SpyState a = new SpyState("A");
            machine.ChangeState(a);

            machine.Reset();

            Assert.AreEqual(1, a.Exits);
            Assert.IsFalse(machine.HasState);
            Assert.IsNull(machine.Current);
        }

        [Test]
        public void Tick_AfterReset_DoesNotThrow()
        {
            StateMachine machine = new StateMachine();
            machine.ChangeState(new SpyState("A"));

            machine.Reset();

            Assert.DoesNotThrow(() => machine.Tick(0.02f));
        }
    }
}
