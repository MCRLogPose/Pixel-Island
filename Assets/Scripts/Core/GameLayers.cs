using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Nombres de layers y tags. Centralizarlos aqui evita strings magicos
    /// desincronizados entre codigo, matriz de colisiones y prefabs.
    /// La configuracion real vive en Project Settings > Tags and Layers
    /// (Etapa 0) y se valida con los EditMode tests.
    /// </summary>
    public static class GameLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Ground = "Ground";
        public const string Obstacle = "Obstacle";
        public const string Coin = "Coin";
        public const string Projectile = "Projectile";
        public const string ParallaxBackground = "ParallaxBackground";

        public const string TagPlayer = "Player";
        public const string TagEnemy = "Enemy";
        public const string TagCoin = "Coin";
        public const string TagHazard = "Hazard";

        public static int Id(string layerName) => LayerMask.NameToLayer(layerName);

        public static bool Exists(string layerName) => LayerMask.NameToLayer(layerName) >= 0;

        public static int PlayerId => Id(Player);
        public static int EnemyId => Id(Enemy);
        public static int GroundId => Id(Ground);
        public static int ObstacleId => Id(Obstacle);
        public static int CoinId => Id(Coin);
        public static int ProjectileId => Id(Projectile);

        /// <summary>Capas que cuentan como suelo pisable (suelo solido y obstaculos).</summary>
        public static LayerMask GroundMask => LayerMask.GetMask(Ground, Obstacle);

        /// <summary>Mascara vacia: usa para deteccion propia sin depender de layers.</summary>
        public static LayerMask EverythingMask => ~0;

        /// <summary>Lista de layers que la Etapa 0 debe haber creado.</summary>
        public static readonly string[] AllLayers =
        {
            Player, Enemy, Ground, Obstacle, Coin, Projectile, ParallaxBackground
        };

        /// <summary>Lista de tags que la Etapa 0 debe haber creado.</summary>
        public static readonly string[] AllTags =
        {
            TagPlayer, TagEnemy, TagCoin, TagHazard
        };
    }
}
