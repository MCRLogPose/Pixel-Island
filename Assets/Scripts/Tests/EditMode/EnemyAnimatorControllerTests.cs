using NUnit.Framework;
using PixelIsland.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Valida el AnimatorController del enemigo como asset. El controller solo
    /// escribe el parametro int "State"; si pierde un estado, se renombra el
    /// parametro o se rompen las condiciones, el enemigo se queda congelado en
    /// el clip anterior sin que nada falle en runtime.
    /// </summary>
    public sealed class EnemyAnimatorControllerTests
    {
        const string ControllerPath = "Assets/Enemy/Animations/Enemy.controller";

        static AnimatorState State(AnimatorController controller, string name)
        {
            foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            {
                if (child.state.name == name)
                {
                    return child.state;
                }
            }

            return null;
        }

        [Test]
        public void ElControllerExiste()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath), ControllerPath);
        }

        [Test]
        public void DeclaraElParametroStateComoInt()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            bool found = false;

            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == EnemyPatrolController.StateParameter)
                {
                    found = true;
                    Assert.AreEqual(AnimatorControllerParameterType.Int, parameter.type);
                }
            }

            Assert.IsTrue(found, "Falta el parametro 'State'.");
        }

        [Test]
        public void TieneLosTresEstadosConSuClip()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            foreach (EnemyAnimState state in System.Enum.GetValues(typeof(EnemyAnimState)))
            {
                string name = state.ToString();
                AnimatorState animState = State(controller, name);
                Assert.IsNotNull(animState, "Falta el estado " + name);
                Assert.IsNotNull(animState.motion, name + " no tiene clip.");

                var clip = animState.motion as AnimationClip;
                Assert.IsNotNull(clip, name + " no es un AnimationClip.");
                Assert.AreEqual("Enemy" + name, clip.name);
            }
        }

        [Test]
        public void ElEstadoPorDefectoEsIdle()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.AreEqual("Idle", controller.layers[0].stateMachine.defaultState.name);
        }

        [Test]
        public void LosIndicesDeLasTransicionesCoincidenConElEnum()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;

            for (int from = 0; from < states.Length; from++)
            {
                AnimatorStateTransition[] transitions = states[from].state.transitions;
                Assert.AreEqual(2, transitions.Length,
                    states[from].state.name + " deberia poder ir a los otros dos estados.");

                foreach (AnimatorStateTransition transition in transitions)
                {
                    Assert.IsFalse(transition.hasExitTime);
                    Assert.AreEqual(1, transition.conditions.Length);
                    Assert.AreEqual(AnimatorConditionMode.Equals, transition.conditions[0].mode);
                    Assert.AreEqual(EnemyPatrolController.StateParameter, transition.conditions[0].parameter);

                    int target = System.Array.FindIndex(states,
                        s => s.state == transition.destinationState);
                    Assert.GreaterOrEqual(target, 0, "Transicion a un estado fuera de la maquina.");
                    Assert.AreEqual((int)(EnemyAnimState)target, transition.conditions[0].threshold,
                        states[from].state.name + " -> " + transition.destinationState.name);
                }
            }
        }

        [Test]
        public void LosClipsAnimanElSpriteRendererDeLaRaiz()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            foreach (EnemyAnimState state in System.Enum.GetValues(typeof(EnemyAnimState)))
            {
                var clip = (AnimationClip)State(controller, state.ToString()).motion;
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

                Assert.AreEqual(1, bindings.Length, state + " deberia tener una sola curva.");
                Assert.AreEqual(string.Empty, bindings[0].path,
                    state + " debe animar el SpriteRenderer de la raiz (no un hijo).");
                Assert.AreEqual(typeof(SpriteRenderer), bindings[0].type);
                Assert.AreEqual("m_Sprite", bindings[0].propertyName);

                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                Assert.IsNotEmpty(keys, state + " no tiene fotogramas.");

                foreach (ObjectReferenceKeyframe key in keys)
                {
                    Assert.IsNotNull(key.value, state + " tiene un fotograma nulo.");
                }
            }
        }
    }
}
