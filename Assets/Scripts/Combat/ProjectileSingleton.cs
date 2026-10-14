using System.Collections.Generic;
using PixelIsland.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelIsland.Combat
{
    /// <summary>
    /// Singleton que controla las instancias de proyectil: mantiene un pool, los
    /// presta a <see cref="PlayerShooter"/> y los recicla cuando el proyectil
    /// termina. Existe una sola instancia (ver <see cref="Singleton{T}"/>) para no
    /// crear proyectiles sin control: el pool se reutiliza y los que quedan en
    /// vuelo se recogen al cambiar de escena, de modo que no se acumulan objetos.
    ///
    /// Va en cada escena de juego con el prefab asignado; el patron Singleton
    /// descarta los duplicados y mantiene viva la primera instancia.
    /// </summary>
    [AddComponentMenu("Pixel Island/Combat/Projectile Singleton")]
    public sealed class ProjectileSingleton : Singleton<ProjectileSingleton>
    {
        [Tooltip("Prefab del proyectil. Debe tener el componente Projectile.")]
        [SerializeField] Projectile _projectilePrefab;

        [Tooltip("Cuantos proyectiles se crean de antemano.")]
        [SerializeField] int _prewarmCount = 6;

        [Tooltip("Tope del pool. Al superarlo los proyectiles reciclados se destruyen.")]
        [SerializeField] int _maxPooled = 40;

        readonly Stack<Projectile> _pool = new Stack<Projectile>();
        readonly List<Projectile> _active = new List<Projectile>();

        public Projectile Prefab => _projectilePrefab;

        /// <summary>Proyectiles en vuelo ahora mismo.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>Proyectiles disponibles en el pool.</summary>
        public int PooledCount => _pool.Count;

        protected override void Awake()
        {
            base.Awake();

            // Si otra instancia se quedo como primaria, este objeto ya fue destruido.
            if (!ReferenceEquals(Instance, this))
            {
                return;
            }

            Prewarm();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }

        /// <summary>
        /// Dispara un proyectil desde <paramref name="origin"/> hacia
        /// <paramref name="direction"/>. Devuelve null si no hay prefab asignado.
        /// </summary>
        public Projectile Fire(Vector3 origin, Vector2 direction, float speed = 0f, float lifetime = 0f)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError("[ProjectileSingleton] Falta asignar el prefab del proyectil.");
                return null;
            }

            Projectile projectile = Rent();
            projectile.transform.position = origin;
            projectile.Launch(direction, speed, lifetime);
            _active.Add(projectile);
            return projectile;
        }

        /// <summary>Devuelve un proyectil al pool. Lo invoca el propio proyectil.</summary>
        public void Release(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            _active.Remove(projectile);
            projectile.transform.SetParent(transform, false);

            if (_pool.Count < Mathf.Max(0, _maxPooled))
            {
                _pool.Push(projectile);
            }
            else
            {
                Destroy(projectile.gameObject);
            }
        }

        /// <summary>Recoge todos los proyectiles en vuelo (cambio de escena, reinicio).</summary>
        public void ReleaseAll()
        {
            // Copia porque Release muta la lista.
            Projectile[] snapshot = _active.ToArray();
            foreach (Projectile projectile in snapshot)
            {
                projectile.Despawn();
            }
        }

        Projectile Rent()
        {
            Projectile projectile;
            if (_pool.Count > 0)
            {
                projectile = _pool.Pop();
            }
            else
            {
                projectile = Instantiate(_projectilePrefab, transform);
                projectile.Setup(Release);
            }

            projectile.Setup(Release);
            return projectile;
        }

        void Prewarm()
        {
            if (_projectilePrefab == null)
            {
                return;
            }

            int count = Mathf.Max(0, _prewarmCount);
            for (int i = 0; i < count; i++)
            {
                Projectile projectile = Instantiate(_projectilePrefab, transform);
                projectile.Setup(Release);
                projectile.gameObject.SetActive(false);
                _pool.Push(projectile);
            }
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ReleaseAll();
        }
    }
}
