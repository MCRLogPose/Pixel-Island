using NUnit.Framework;
using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Guarda de seguridad de la Etapa 0. Si alguien borra un layer o un tag
    /// en Project Settings, estos tests fallan con un mensaje claro en vez de
    /// fallar en silencio en tiempo de ejecucion (por ejemplo, el sensor de
    /// suelo que no detecta nada).
    /// </summary>
    public sealed class ProjectSetupTests
    {
        [Test]
        public void AllGameLayers_ExistInTagManager()
        {
            foreach (string layer in GameLayers.AllLayers)
            {
                Assert.GreaterOrEqual(LayerMask.NameToLayer(layer), 0,
                    "Falta el layer '" + layer + "' en Project Settings > Tags and Layers.");
            }
        }

        [Test]
        public void GameLayers_HaveUniqueIndices()
        {
            int[] ids = new int[GameLayers.AllLayers.Length];
            for (int i = 0; i < GameLayers.AllLayers.Length; i++)
            {
                ids[i] = LayerMask.NameToLayer(GameLayers.AllLayers[i]);
            }

            for (int i = 0; i < ids.Length; i++)
            {
                for (int j = i + 1; j < ids.Length; j++)
                {
                    Assert.AreNotEqual(ids[i], ids[j],
                        "Los layers '" + GameLayers.AllLayers[i] + "' y '" +
                        GameLayers.AllLayers[j] + "' comparten indice.");
                }
            }
        }

        [Test]
        public void AllGameTags_ExistInTagManager()
        {
            foreach (string tag in GameLayers.AllTags)
            {
                Assert.IsTrue(TagExists(tag), "Falta el tag '" + tag + "' en Project Settings.");
            }
        }

        [Test]
        public void GroundMask_IncludesGroundAndObstacleOnly()
        {
            LayerMask mask = GameLayers.GroundMask;

            Assert.AreEqual((1 << GameLayers.GroundId) | (1 << GameLayers.ObstacleId),
                mask.value, "La mascara de suelo debe cubrir exactamente Ground y Obstacle.");
        }

        [Test]
        public void CollisionMatrix_SeparatesParallaxFromGameplay()
        {
            int parallax = GameLayers.Id(GameLayers.ParallaxBackground);

            foreach (string gameplay in new[] { GameLayers.Player, GameLayers.Enemy, GameLayers.Coin })
            {
                int id = GameLayers.Id(gameplay);
                Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(parallax, id),
                    "ParallaxBackground no debe colisionar con " + gameplay + ".");
            }
        }

        [Test]
        public void CollisionMatrix_CoinLayerIsPairedWithPlayer()
        {
            // El "trigger" va en el collider de la moneda, no en la matriz: las
            // capas deben quedar emparejadas para que se emita OnTriggerEnter2D.
            Assert.IsFalse(
                Physics2D.GetIgnoreLayerCollision(GameLayers.CoinId, GameLayers.PlayerId),
                "Las capas Coin y Player deben estar emparejadas en la matriz.");
        }

        [Test]
        public void CollisionMatrix_ProjectilesDoNotHitThePlayer()
        {
            Assert.IsTrue(
                Physics2D.GetIgnoreLayerCollision(GameLayers.ProjectileId, GameLayers.PlayerId),
                "Las balas propias no deben danar al Explorador.");
        }

        [Test]
        public void CollisionMatrix_ProjectilesReachEnemies()
        {
            Assert.IsFalse(
                Physics2D.GetIgnoreLayerCollision(GameLayers.ProjectileId, GameLayers.EnemyId),
                "Las balas deben poder alcanzar a las criaturas.");
        }

        [Test]
        public void CollisionMatrix_EnemiesDoNotPushEachOther()
        {
            Assert.IsTrue(
                Physics2D.GetIgnoreLayerCollision(GameLayers.EnemyId, GameLayers.EnemyId),
                "Las criaturas no deben colisionar entre si.");
        }

        [Test]
        public void CollisionMatrix_PlayerCollidesWithGround()
        {
            Assert.IsFalse(
                Physics2D.GetIgnoreLayerCollision(GameLayers.PlayerId, GameLayers.GroundId),
                "El Explorador debe chocar con el suelo.");
        }

        [Test]
        public void CollisionMatrix_PlayerCollidesWithEnemies()
        {
            Assert.IsFalse(
                Physics2D.GetIgnoreLayerCollision(GameLayers.PlayerId, GameLayers.EnemyId),
                "El contacto con una criatura debe detectarse.");
        }

        [Test]
        public void ZoneId_FollowsIslandProgressionOrder()
        {
            Assert.AreEqual(4, ZoneIdExtensions.Count, "La isla tiene 4 zonas.");
            Assert.AreEqual(ZoneId.Jungle, ZoneId.Beach.Next());
            Assert.AreEqual(ZoneId.Cave, ZoneId.Jungle.Next());
            Assert.AreEqual(ZoneId.Temple, ZoneId.Cave.Next());
            Assert.AreEqual(ZoneId.Temple, ZoneId.Temple.Next(), "El Templo es la zona final.");
            Assert.AreEqual("Playa", ZoneId.Beach.DisplayName());
            Assert.AreEqual("Templo", ZoneId.Temple.DisplayName());
        }

        static bool TagExists(string tag)
        {
            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
