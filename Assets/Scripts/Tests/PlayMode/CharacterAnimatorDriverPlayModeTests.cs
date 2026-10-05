using System.Collections;
using NUnit.Framework;
using PixelIsland.Animation;
using PixelIsland.Core;
using PixelIsland.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixelIsland.Tests.PlayMode
{
    /// <summary>
    /// Tests del cableado real entre la maquina de estados del Player y el
    /// Animator. Los EditMode tests comprueban el mapeo y los controllers como
    /// assets; aqui se ve lo que solo existe en play: que el Animator recibe el
    /// estado, que reproduce el clip del estado actual y que reenviar el mismo
    /// estado no lo reinicia.
    ///
    /// Monta su propio Player en memoria, sin cargar ForestZone ni ForestLevel.
    /// </summary>
    public sealed class CharacterAnimatorDriverPlayModeTests
    {
        const string ControllerPath = "Assets/Character/Animations/Character1/Character1.controller";

        GameObject _ground;
        GameObject _root;
        FakePlayerInput _input;
        Animator _animator;
        CharacterAnimatorDriver _driver;
        PlayerController _player;

        [SetUp]
        public void SetUp()
        {
            // Suelo para que Run sea un estado estable: si no, la maquina del
            // Player cae a Fall en cuanto corre un paso fisico.
            _ground = new GameObject("AnimationTestGround");
            _ground.layer = GameLayers.GroundId;
            _ground.transform.position = new Vector3(0f, -1f, 0f);
            _ground.AddComponent<BoxCollider2D>().size = new Vector2(40f, 2f);

            _root = new GameObject("AnimationTestRig");
            _root.layer = GameLayers.PlayerId;

            // GroundSensor asume los pies en transform.position (su _boxOffset
            // por defecto es (0,-0.03)), asi que el collider se pone con offset
            // +1 para que la planta del capsule quede en y = 0, igual que en
            // PlayerMovementPlayModeTests. ForestZone usa offset -1 y por eso
            // necesita su sensor reconfigurado a mano.
            CapsuleCollider2D capsule = _root.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.72f, 2f);
            capsule.offset = new Vector2(0f, 1f);

            Rigidbody2D body = _root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            _input = _root.AddComponent<FakePlayerInput>();

            GroundSensor sensor = _root.AddComponent<GroundSensor>();
            SerializedGroundMask(sensor);

            // El Animator y el driver deben existir antes de que el Player pase
            // a Idle, para que el primer Sync tenga a quien escribir.
            _animator = _root.AddComponent<Animator>();
            _animator.runtimeAnimatorController = LoadController();

            _player = _root.AddComponent<PlayerController>();
            _driver = _root.AddComponent<CharacterAnimatorDriver>();
        }

        static void SerializedGroundMask(GroundSensor sensor)
        {
            // El PlayerController tambien asigna la mascara en su Awake, pero se
            // fija aqui para que el sensor sea correcto desde el primer paso.
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(sensor);
            so.FindProperty("_groundMask").intValue = GameLayers.GroundMask.value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_ground);
            Object.Destroy(_root);
        }

        static RuntimeAnimatorController LoadController()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        }

        static IEnumerator WaitPhysics(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator ElEstadoInicialEsIdle()
        {
            yield return WaitPhysics(2);

            Assert.AreEqual((int)CharacterAnimState.Idle, _animator.GetInteger("State"));
        }

        [UnityTest]
        public IEnumerator ElParametroStateSigueAlEstadoDelPlayer()
        {
            yield return WaitPhysics(2);

            // Jump y Fall decaen al siguiente paso fisico, asi que se comprueba
            // el cableado sin dejar que la maquina advanced.
            _player.SetState(_player.FallState);
            _driver.Sync();
            Assert.AreEqual((int)CharacterAnimState.Fall, _animator.GetInteger("State"));

            _player.SetState(_player.JumpState);
            _driver.Sync();
            Assert.AreEqual((int)CharacterAnimState.Jump, _animator.GetInteger("State"));
        }

        [UnityTest]
        public IEnumerator CorrerEnElPlayerReproduceElClipDeRun()
        {
            yield return WaitPhysics(2);

            _input.MoveX = 1f;
            yield return WaitPhysics(5);

            Assert.AreEqual((int)CharacterAnimState.Run, _animator.GetInteger("State"),
                "Mover el player en suelo deberia llevar la maquina a Run.");
            Assert.AreEqual("Character1Run", CurrentClipName());
        }

        [UnityTest]
        public IEnumerator PararEnSueloReproduceElClipDeIdle()
        {
            _input.MoveX = 1f;
            yield return WaitPhysics(5);

            _input.MoveX = 0f;
            yield return WaitPhysics(5);

            Assert.AreEqual((int)CharacterAnimState.Idle, _animator.GetInteger("State"));
            Assert.AreEqual("Character1Idle", CurrentClipName());
        }

        [UnityTest]
        public IEnumerator ElClipDeRunAvanzaEnElTiempo()
        {
            _input.MoveX = 1f;
            yield return WaitPhysics(5);

            float before = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSeconds(0.2f);
            float after = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

            Assert.Greater(after, before,
                "Run debe loopear: reenviar el mismo estado no puede reiniciar el clip.");
        }

        [UnityTest]
        public IEnumerator SinControllerElDriverNoEscribeNiLanzaErrores()
        {
            yield return WaitPhysics(2);
            int before = _animator.GetInteger("State");

            _animator.runtimeAnimatorController = null;
            _player.SetState(_player.JumpState);

            yield return WaitPhysics(2);

            // El Player loguea cada transicion con un Log normal y Unity no
            // falla el test por eso; los errores y excepciones si. Lo que se
            // comprueba es que sin controller el driver abandona en vez de
            // escribir y provocar un aviso de Unity.
            Assert.AreEqual(before, _animator.GetInteger("State"));
        }

        string CurrentClipName()
        {
            var info = _animator.GetCurrentAnimatorClipInfo(0);
            return info.Length > 0 && info[0].clip != null ? info[0].clip.name : "<nada>";
        }
    }
}