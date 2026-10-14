using System.Collections;
using NUnit.Framework;
using PixelIsland.Combat;
using PixelIsland.Core;
using PixelIsland.Enemies;
using UnityEngine;
using UnityEngine.TestTools;

namespace PixelIsland.Tests.PlayMode
{
    /// <summary>
    /// Tests de integracion del proyectil en el bucle de fisica real. Montan su
    /// propio escenario (suelo, enemigo y proyectil) para no depender de las
    /// escenas de juego, igual que PlayerMovementPlayModeTests.
    /// </summary>
    public sealed class ProjectilePlayModeTests
    {
        GameObject _ground;
        GameObject _enemy;
        GameObject _projectile;

        [TearDown]
        public void TearDown()
        {
            DestroyIfAlive(_projectile);
            DestroyIfAlive(_enemy);
            DestroyIfAlive(_ground);
        }

        static void DestroyIfAlive(GameObject go)
        {
            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        static IEnumerator WaitPhysics(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        void BuildGround()
        {
            _ground = new GameObject("TestGround");
            _ground.layer = GameLayers.GroundId;
            _ground.transform.position = new Vector3(0f, -1f, 0f);
            BoxCollider2D box = _ground.AddComponent<BoxCollider2D>();
            box.size = new Vector2(60f, 2f);
        }

        void BuildEnemy(Vector2 position)
        {
            _enemy = new GameObject("TestEnemy");
            _enemy.layer = GameLayers.EnemyId;
            _enemy.tag = GameLayers.TagEnemy;
            _enemy.transform.position = position;

            Rigidbody2D body = _enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            BoxCollider2D box = _enemy.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f);

            _enemy.AddComponent<EnemyHealth>();
        }

        Projectile BuildProjectile(Vector2 position, Vector2 direction)
        {
            _projectile = new GameObject("TestProjectile");
            _projectile.layer = GameLayers.ProjectileId;
            _projectile.transform.position = position;

            Rigidbody2D body = _projectile.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            BoxCollider2D box = _projectile.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.4f, 0.4f);

            Projectile projectile = _projectile.AddComponent<Projectile>();
            projectile.Launch(direction, 15f);
            return projectile;
        }

        [UnityTest]
        public IEnumerator Projectile_KillsEnemyWithASingleShot()
        {
            BuildGround();
            BuildEnemy(new Vector2(5f, 1f));

            Projectile projectile = BuildProjectile(new Vector2(0f, 1f), Vector2.right);

            yield return WaitPhysics(40);

            Assert.IsTrue(projectile.IsSpent, "El proyectil debe gastarse al impactar.");
            Assert.IsTrue(_enemy == null, "El enemigo debe caer de un solo disparo.");
        }

        [UnityTest]
        public IEnumerator Projectile_DespawnsWhenHittingTheGround()
        {
            BuildGround();

            Projectile projectile = BuildProjectile(new Vector2(0f, 0.6f), Vector2.down);

            yield return WaitPhysics(20);

            Assert.IsTrue(projectile.IsSpent, "El proyectil debe desaparecer al tocar el suelo.");
        }

        [UnityTest]
        public IEnumerator Projectile_TravelsWithNoObstaclesUntilLifetime()
        {
            Projectile projectile = BuildProjectile(new Vector2(0f, 20f), Vector2.right);

            yield return WaitPhysics(5);

            Assert.IsFalse(projectile.IsSpent, "Sin nada en el camino debe seguir en vuelo.");
            Assert.Greater(_projectile.transform.position.x, 0f, "Debe avanzar en su direccion.");
        }
    }
}
