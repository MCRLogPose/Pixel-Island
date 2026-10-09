using System.Collections;
using NUnit.Framework;
using PixelIsland.Core;
using PixelIsland.Hazards;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixelIsland.Tests.PlayMode
{
    /// <summary>
    /// Verifica el cableado real de los pinchos dentro del bucle de fisica: caer
    /// sobre la punta resta una vida, pero chocar de costado solo empuja. Monta su
    /// propio escenario en memoria para no depender de la escena abierta.
    /// </summary>
    public sealed class SpikeHazardPlayModeTests
    {
        GameObject _ground;
        GameObject _spike;
        GameObject _player;
        Rigidbody2D _body;
        LivesComponent _lives;

        [SetUp]
        public void SetUp()
        {
            // Suelo con la superficie en y = 0.
            _ground = new GameObject("TestGround");
            _ground.layer = GameLayers.GroundId;
            _ground.transform.position = new Vector3(0f, -1f, 0f);
            BoxCollider2D groundBox = _ground.AddComponent<BoxCollider2D>();
            groundBox.size = new Vector2(40f, 2f);

            // Pinchos solidos apoyados en el suelo: ocupan y = 0..1, x = -0.5..0.5.
            _spike = new GameObject("TestSpike");
            _spike.layer = GameLayers.ObstacleId;
            _spike.transform.position = new Vector3(0f, 0.5f, 0f);
            BoxCollider2D spikeBox = _spike.AddComponent<BoxCollider2D>();
            spikeBox.size = new Vector2(1f, 1f);
            _spike.AddComponent<SpikeHazard>();

            // Explorador de prueba: collider de 1x2 con pivote abajo (pies en y).
            _player = new GameObject("TestPlayer");
            _player.layer = GameLayers.PlayerId;

            _body = _player.AddComponent<Rigidbody2D>();
            _body.gravityScale = 1f;
            _body.freezeRotation = true;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D bodyBox = _player.AddComponent<BoxCollider2D>();
            bodyBox.size = new Vector2(1f, 2f);
            bodyBox.offset = new Vector2(0f, 1f);

            _lives = _player.AddComponent<LivesComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_ground);
            Object.Destroy(_spike);
            Object.Destroy(_player);
        }

        static IEnumerator WaitPhysics(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator CaeSobreLaPunta_RestaUnaVida()
        {
            int start = _lives.CurrentLives;
            _player.transform.position = new Vector3(0f, 4f, 0f);

            // Se cae hasta posarse sobre la punta (techo de los pinchos en y = 1).
            for (int i = 0; i < 200; i++)
            {
                yield return new WaitForFixedUpdate();
                if (_player.transform.position.y <= 1.05f && _body.velocity.y <= 0f)
                {
                    break;
                }
            }

            Assert.AreEqual(start - 1, _lives.CurrentLives,
                "Caer sobre la punta debe restar exactamente una vida.");
        }

        [UnityTest]
        public IEnumerator ChocaDeCostado_NoRestaVida()
        {
            int start = _lives.CurrentLives;
            _player.transform.position = new Vector3(-1.6f, 0f, 0f);

            yield return WaitPhysics(10);

            // Se empuja contra el flanco; los pies siguen por debajo de la punta.
            for (int i = 0; i < 60; i++)
            {
                _body.velocity = new Vector2(3f, _body.velocity.y);
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(start, _lives.CurrentLives,
                "Chocar de costado no debe restar vidas.");
        }

        [UnityTest]
        public IEnumerator CaeSobreLaPunta_AvisaLaReaccionDeDano()
        {
            RecordingReaction reaction = _player.AddComponent<RecordingReaction>();
            _player.transform.position = new Vector3(0f, 4f, 0f);

            for (int i = 0; i < 200; i++)
            {
                yield return new WaitForFixedUpdate();
                if (_player.transform.position.y <= 1.05f && _body.velocity.y <= 0f)
                {
                    break;
                }
            }

            Assert.IsTrue(reaction.Called, "El hazard debe avisar a la reaccion de dano.");
            Assert.AreEqual(_spike.transform.position.x, reaction.LastSource.x, 0.001f);
            Assert.AreEqual(_spike.transform.position.y, reaction.LastSource.y, 0.001f);
        }

        sealed class RecordingReaction : MonoBehaviour, IDamageReaction
        {
            public bool Called;
            public Vector2 LastSource;

            public void OnDamaged(Vector2 sourcePosition)
            {
                Called = true;
                LastSource = sourcePosition;
            }
        }
    }
}
