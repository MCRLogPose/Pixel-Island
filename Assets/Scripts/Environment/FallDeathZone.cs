using PixelIsland.Core;
using UnityEngine;

namespace PixelIsland.Environment
{
    /// <summary>
    /// Muerte por caida al vacio. Cuando el Explorador baja del plano de muerte le
    /// vacia las vidas. El reinicio NO vive aqui: lo dispara <see cref="PlayerDeath"/>
    /// al ver las vidas a 0, de modo que todas las causas de muerte comparten el
    /// mismo reinicio (y no hay dos coroutine compitiendo por recargar la escena).
    ///
    /// El plano es una altura en y y no un collider: con islas a distintas alturas
    /// basta con ponerlo por debajo del tile mas bajo, sin depender de layers ni de
    /// triggers ni de que el hueco tenga suelo fisico.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/Environment/Fall Death Zone")]
    public sealed class FallDeathZone : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("Objeto a vigilar. Vacio = el objeto con el tag Player.")]
        [SerializeField] Transform _target;

        [Tooltip("Altura (y) por debajo de la cual se considera que el objetivo cayo al vacio.")]
        [SerializeField] float _killPlaneY = -16f;

        FallDeath _death;

        FallDeath Death
        {
            get
            {
                if (_death == null)
                {
                    _death = new FallDeath(_killPlaneY);
                }

                return _death;
            }
        }

        /// <summary>Altura del plano de muerte.</summary>
        public float KillPlaneY
        {
            get => _killPlaneY;
            set => _killPlaneY = value;
        }

        void Update()
        {
            Transform target = ResolveTarget();
            if (target == null)
            {
                return;
            }

            if (Death.Check(target.position.y))
            {
                HandleFall(target);
            }
        }

        Transform ResolveTarget()
        {
            if (_target != null)
            {
                return _target;
            }

            GameObject tagged = GameObject.FindGameObjectWithTag(GameLayers.TagPlayer);
            if (tagged != null)
            {
                _target = tagged.transform;
            }

            return _target;
        }

        void HandleFall(Transform target)
        {
            LivesComponent lives = target.GetComponent<LivesComponent>();
            if (lives != null)
            {
                lives.SetLives(0);
            }
        }

        void OnDisable()
        {
            _death?.Rearm();
        }
    }
}
