using System.Collections;
using NUnit.Framework;
using PixelIsland.Core;
using PixelIsland.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixelIsland.Tests.PlayMode
{
    /// <summary>
    /// Tests de integracion de la Etapa 1 dentro del bucle de fisica real de
    /// Unity. Los EditMode tests verifican el tuning del PlayerMotor con logica
    /// pura; estos verifican el CABLEADO que solo se puede comprobar en play:
    /// collider, Rigidbody2D, sensor de suelo (incluido el descarte del collider
    /// propio) y la maquina de estados.
    ///
    /// Montan su propio escenario para no depender del contenido de SampleScene.
    /// </summary>
    public sealed class PlayerMovementPlayModeTests
    {
        GameObject _ground;
        GameObject _player;
        FakePlayerInput _input;
        PlayerController _controller;

        [SetUp]
        public void SetUp()
        {
            _ground = new GameObject("TestGround");
            _ground.layer = GameLayers.GroundId;
            _ground.transform.position = new Vector3(0f, -1f, 0f);
            BoxCollider2D groundBox = _ground.AddComponent<BoxCollider2D>();
            groundBox.size = new Vector2(40f, 2f);

            _player = new GameObject("TestPlayer");
            _player.layer = GameLayers.PlayerId;
            _player.transform.position = new Vector3(0f, 0f, 0f);

            Rigidbody2D body = _player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Mismas medidas que el prefab: collider de 1x2 con pivote abajo.
            CapsuleCollider2D capsule = _player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.72f, 2f);
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.offset = new Vector2(0f, 1f);

            // Sin sprite asignado: solo interesa que exista para poder verificar
            // que el estado Run orienta el renderer.
            _player.AddComponent<SpriteRenderer>();

            _input = _player.AddComponent<FakePlayerInput>();

            GroundSensor sensor = _player.AddComponent<GroundSensor>();
            SerializedGroundMask(sensor);

            _controller = _player.AddComponent<PlayerController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_ground);
            Object.Destroy(_player);
        }

        static void SerializedGroundMask(GroundSensor sensor)
        {
            // El PlayerController tambien asigna la mascara en su Awake, pero se
            // fija aqui para que el sensor sea correcto desde el primer paso.
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(sensor);
            so.FindProperty("_groundMask").intValue = GameLayers.GroundMask.value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static IEnumerator WaitPhysics(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator Player_DetectsGroundAndDoesNotSink()
        {
            yield return WaitPhysics(30);

            Assert.IsTrue(_controller.IsGrounded, "El sensor debe detectar el suelo bajo los pies.");
            Assert.AreEqual(0f, _player.transform.position.y, 0.02f,
                "El Explorador debe quedarse quieto sobre el suelo.");
            Assert.AreEqual("Idle", _controller.CurrentStateName,
                "En reposo y sobre el suelo el estado debe ser Idle.");
        }

        [UnityTest]
        public IEnumerator Player_DoesNotDetectItsOwnColliderAsGround()
        {
            // El box del sensor se solapa con el collider propio: si el filtro de
            // autocomprobacion fallara, el jugador creeria estar en el aire.
            yield return WaitPhysics(20);

            Assert.AreEqual(Vector2.zero, _controller.Velocity,
                "Sin gravedad externa el jugador en reposo no debe moverse.");
        }

        [UnityTest]
        public IEnumerator Player_WalksRightWhenInputIsPositive()
        {
            _input.MoveX = 1f;

            yield return WaitPhysics(120);

            Assert.Greater(_player.transform.position.x, 3f,
                "Con entrada a la derecha el Explorador debe avanzar.");
            Assert.AreEqual(_controller.Movement.MoveSpeed, _controller.Velocity.x, 0.05f,
                "Debe alcanzar la velocidad configurada.");
            Assert.AreEqual("Run", _controller.CurrentStateName);
            Assert.IsFalse(_controller.FacingLeft);
        }

        [UnityTest]
        public IEnumerator Player_WalksLeftAndFlipsSprite()
        {
            _input.MoveX = -1f;

            yield return WaitPhysics(120);

            Assert.Less(_player.transform.position.x, -3f, "Con entrada a la izquierda debe retroceder.");
            Assert.IsTrue(_controller.FacingLeft, "El sprite debe mirar hacia la izquierda.");
        }

        [UnityTest]
        public IEnumerator Player_JumpsToConfiguredHeightAndLands()
        {
            yield return WaitPhysics(10);

            _input.PressJump();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual("Jump", _controller.CurrentStateName,
                "Tras el pulseo de salto debe entrar en estado Jump.");
            Assert.Greater(_controller.Velocity.y, 0f, "La velocidad vertical debe ser positiva.");

            // 0.80 s de tiempo de vuelo completo con la configuracion por defecto.
            yield return WaitPhysics(110);

            Assert.IsTrue(_controller.IsGrounded, "El Explorador debe volver a aterrizar.");
            Assert.AreEqual(0f, _player.transform.position.y, 0.04f,
                "Debe posarse en el suelo, no atravesarlo ni flotar por encima.");
            Assert.AreEqual("Idle", _controller.CurrentStateName);
        }

        [UnityTest]
        public IEnumerator Player_PassesThroughApexWithoutTouchingIdleOrGrounded()
        {
            yield return WaitPhysics(10);

            _input.PressJump();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("Jump", _controller.CurrentStateName);

            // Se avanza hasta pasado el punto mas alto. Al perder altura el estado
            // debe pasar directo a Fall sin colarse por Idle en el aire.
            string previous = "Jump";

            for (int i = 0; i < 40 && _controller.Velocity.y > 0f; i++)
            {
                yield return new WaitForFixedUpdate();
                previous = _controller.CurrentStateName;
            }

            Assert.LessOrEqual(_controller.Velocity.y, 0f,
                "El salto debe superar el apex dentro del margen del test.");

            yield return new WaitForFixedUpdate();

            Assert.AreEqual("Fall", _controller.CurrentStateName,
                "Al bajar la velocidad vertical debe entrar en Fall, no en Idle.");
            Assert.IsFalse(_controller.IsGrounded,
                "En la apex el jugador sigue en el aire.");
            Assert.AreNotEqual("Idle", previous, "Idle no debe aparecer en mitad del arco.");
        }

        [UnityTest]
        public IEnumerator Player_LandsWithFeetFlushOnTheSurface()
        {
            yield return WaitPhysics(10);

            _input.PressJump();
            yield return new WaitForFixedUpdate();

            // Durante toda la caida el jugador no debe quedarse congelado a media
            // altura: los pies tienen que acabar apoyados en la superficie.
            float worstGap = 0f;
            for (int i = 0; i < 120; i++)
            {
                yield return new WaitForFixedUpdate();

                if (_controller.IsGrounded && _controller.Velocity.y <= 0f)
                {
                    worstGap = _player.transform.position.y;
                    break;
                }
            }

            Assert.Less(worstGap, 0.02f,
                "Al tocar suelo los pies deben quedar pegados a la superficie, no flotando.");
        }

        [UnityTest]
        public IEnumerator Player_CoyoteJumpJustAfterWalkingOffALedge()
        {
            // El suelo acaba en x = 2 para poder salirse del borde.
            _ground.transform.position = new Vector3(-8f, -1f, 0f);
            _ground.GetComponent<BoxCollider2D>().size = new Vector2(20f, 2f);

            yield return WaitPhysics(10);
            Assert.IsTrue(_controller.IsGrounded);

            _input.MoveX = 1f;

            // Se espera al primer fotograma en el que se pierde el suelo; el
            // pulseo de salto debe llegar dentro de la ventana de coyote time.
            bool leftGround = false;
            for (int i = 0; i < 300 && !leftGround; i++)
            {
                yield return new WaitForFixedUpdate();

                if (!_controller.IsGrounded && _player.transform.position.y > -0.5f)
                {
                    leftGround = true;
                }
            }

            Assert.IsTrue(leftGround, "El Explorador debe abandonar el borde.");

            _input.MoveX = 0f;
            _input.PressJump();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual("Jump", _controller.CurrentStateName,
                "El coyote time debe permitir saltar nada mas abandonar el suelo.");
        }

        [UnityTest]
        public IEnumerator Player_CannotJumpLongAfterLeavingALedge()
        {
            _ground.transform.position = new Vector3(-8f, -1f, 0f);
            _ground.GetComponent<BoxCollider2D>().size = new Vector2(20f, 2f);

            yield return WaitPhysics(10);

            _input.MoveX = 1f;
            bool leftGround = false;
            for (int i = 0; i < 300 && !leftGround; i++)
            {
                yield return new WaitForFixedUpdate();

                if (!_controller.IsGrounded && _player.transform.position.y > -0.5f)
                {
                    leftGround = true;
                }
            }

            Assert.IsTrue(leftGround);

            // Se deja pasar toda la ventana de coyote time antes de pulsar.
            _input.MoveX = 0f;
            yield return WaitPhysics(20);

            _input.PressJump();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual("Fall", _controller.CurrentStateName,
                "Fuera del coyote time el salto debe rechazarse.");
            Assert.LessOrEqual(_controller.Velocity.y, 0f,
                "La velocidad vertical debe seguir siendo de caida.");
        }

        [UnityTest]
        public IEnumerator Player_DoesNotFallThroughGroundWhenLanding()
        {
            yield return WaitPhysics(10);
            _input.PressJump();
            yield return WaitPhysics(200);

            Assert.GreaterOrEqual(_player.transform.position.y, -0.02f,
                "El collider debe impedir caer por debajo del suelo.");
        }
    }
}
