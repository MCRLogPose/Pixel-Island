using NUnit.Framework;
using PixelIsland.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Valida los dos AnimatorControllers como assets. El driver solo escribe
    /// el parametro int "State"; si un controller pierde un estado, cambia el
    /// nombre del parametro o se le quita el loop al Run, el Player se queda
    /// congelado en el clip anterior sin que nada falle en runtime.
    /// </summary>
    public class CharacterAnimatorControllerTests
    {
        static readonly string[] ControllerPaths =
        {
            "Assets/Character/Animations/Character1/Character1.controller",
            "Assets/Character/Animations/Character2/Character2.controller"
        };

        static AnimatorState State(AnimatorController controller, string name)
        {
            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state.name == name)
                {
                    return states[i].state;
                }
            }

            return null;
        }

        static ChildAnimatorState[] States(AnimatorController controller)
        {
            return controller.layers[0].stateMachine.states;
        }

        [Test]
        public void AmbosControllersExisten()
        {
            foreach (string path in ControllerPaths)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AnimatorController>(path), path);
            }
        }

        [Test]
        public void AmbosDeclaranElParametroStateComoInt()
        {
            foreach (string path in ControllerPaths)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                bool found = false;

                foreach (AnimatorControllerParameter p in controller.parameters)
                {
                    if (p.name == "State")
                    {
                        found = true;
                        Assert.AreEqual(AnimatorControllerParameterType.Int, p.type,
                            path + " necesita State como int.");
                    }
                }

                Assert.IsTrue(found, path + " no declara el parametro 'State'.");
            }
        }

        [TestCase("Assets/Character/Animations/Character1/Character1.controller", "Character1")]
        [TestCase("Assets/Character/Animations/Character2/Character2.controller", "Character2")]
        public void ElControllerTieneLosCuatroEstadosConSuClip(string path, string prefix)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            string[] expected = { "Idle", "Run", "Jump", "Fall" };

            foreach (string name in expected)
            {
                AnimatorState state = State(controller, name);
                Assert.IsNotNull(state, path + " no tiene el estado " + name);
                Assert.IsNotNull(state.motion, path + "/" + name + " no tiene clip.");

                var clip = state.motion as AnimationClip;
                Assert.IsNotNull(clip, path + "/" + name + " no es un AnimationClip.");
                Assert.AreEqual(prefix + name, clip.name);
            }
        }

        [Test]
        public void ElEstadoPorDefectoEsIdleEnAmbos()
        {
            foreach (string path in ControllerPaths)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                Assert.AreEqual("Idle", controller.layers[0].stateMachine.defaultState.name, path);
            }
        }

        [TestCase("Assets/Character/Animations/Character1/Character1.controller")]
        [TestCase("Assets/Character/Animations/Character2/Character2.controller")]
        public void SoloRunEsLoopeable(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            Assert.IsTrue(((AnimationClip)State(controller, "Run").motion).isLooping, "Run debe loopear.");
            Assert.IsFalse(((AnimationClip)State(controller, "Idle").motion).isLooping);
            Assert.IsFalse(((AnimationClip)State(controller, "Jump").motion).isLooping);
            Assert.IsFalse(((AnimationClip)State(controller, "Fall").motion).isLooping);
        }

        [TestCase("Assets/Character/Animations/Character1/Character1.controller")]
        [TestCase("Assets/Character/Animations/Character2/Character2.controller")]
        public void TodosLosClipsAnimanElSpriteRendererDelHijo(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            string[] expected = { "Idle", "Run", "Jump", "Fall" };

            foreach (string name in expected)
            {
                var clip = (AnimationClip)State(controller, name).motion;
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

                Assert.AreEqual(1, bindings.Length, path + "/" + name + " deberia tener una sola curva.");

                EditorCurveBinding binding = bindings[0];
                Assert.AreEqual("Character", binding.path, path + "/" + name + " debe animar al hijo 'Character'.");
                Assert.AreEqual(typeof(SpriteRenderer), binding.type);
                Assert.AreEqual("m_Sprite", binding.propertyName);

                ObjectReferenceKeyframe[] keys =
                    AnimationUtility.GetObjectReferenceCurve(clip, binding);
                Assert.IsNotEmpty(keys, path + "/" + name + " no tiene fotogramas.");

                foreach (ObjectReferenceKeyframe key in keys)
                {
                    Assert.IsNotNull(key.value, path + "/" + name + " tiene un fotograma nulo.");
                }
            }
        }

        [Test]
        public void RunTieneCuatroFotogramasEnLosDosPersonajes()
        {
            foreach (string path in ControllerPaths)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                var clip = (AnimationClip)State(controller, "Run").motion;
                EditorCurveBinding binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)[0];

                Assert.AreEqual(4, AnimationUtility.GetObjectReferenceCurve(clip, binding).Length,
                    path + " deberia animar 4 fotogramas de carrera.");
            }
        }

        [TestCase("Assets/Character/Animations/Character1/Character1.controller")]
        [TestCase("Assets/Character/Animations/Character2/Character2.controller")]
        public void LosIndicesDeLasTransicionesCoincidenConElEnum(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            ChildAnimatorState[] states = States(controller);

            // Cada estado debe poder ir a los otros tres, y la condicion debe
            // pedir exactamente el indice del destino.
            for (int from = 0; from < states.Length; from++)
            {
                AnimatorStateTransition[] transitions = states[from].state.transitions;
                Assert.AreEqual(3, transitions.Length,
                    path + "/" + states[from].state.name + " deberia tener 3 transiciones.");

                foreach (AnimatorStateTransition transition in transitions)
                {
                    Assert.IsFalse(transition.hasExitTime,
                        "Sin exit time el cambio es inmediato; con el, el estado anterior se alarga.");
                    Assert.AreEqual(1, transition.conditions.Length);
                    Assert.AreEqual(AnimatorConditionMode.Equals, transition.conditions[0].mode);
                    Assert.AreEqual("State", transition.conditions[0].parameter);

                    int target = System.Array.FindIndex(states, s => s.state == transition.destinationState);
                    Assert.GreaterOrEqual(target, 0, "Transicion a un estado fuera de la maquina.");
                    Assert.AreEqual((int)(CharacterAnimState)target, transition.conditions[0].threshold,
                        path + "/" + states[from].state.name + " -> " + transition.destinationState.name);
                }
            }
        }
    }
}