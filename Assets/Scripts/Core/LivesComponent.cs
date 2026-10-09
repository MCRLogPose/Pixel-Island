using System;
using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Puente MonoBehaviour para <see cref="Lives"/>. Va en el Explorador y da a los
    /// demas sistemas (HUD, hazards) un sitio unico donde leer y restar vidas.
    ///
    /// El modelo se resuelve de forma perezosa porque Awake no corre al abrir una
    /// escena en edicion (ver AGENTS.md), y asi el campo de vidas tambien responde
    /// a las herramientas de editor y a los tests de EditMode.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Pixel Island/Core/Lives Component")]
    public sealed class LivesComponent : MonoBehaviour
    {
        [Tooltip("Vidas con las que arranca la partida.")]
        [SerializeField] int _startingLives = 3;

        [Tooltip("Segundos de invulnerabilidad tras recibir dano, para que un solo " +
                 "toque (o varios enemigos a la vez) no drenen las vidas de golpe.")]
        [SerializeField] float _invulnerabilitySeconds = 1f;

        /// <summary>Se reemite el cambio del modelo para que el HUD solo escuche aqui.</summary>
        public event Action<Lives> Changed;

        Lives _lives;
        float _invulnerableUntil;

        public Lives Value
        {
            get
            {
                if (_lives == null)
                {
                    _lives = new Lives(Mathf.Max(1, _startingLives));
                }

                return _lives;
            }
        }

        public int CurrentLives => Value.Current;

        public int MaxLives => Value.Max;

        /// <summary>Resta una vida. Devuelve false si ya no quedaban.</summary>
        public bool LoseLife() => Value.LoseLife();

        /// <summary>True mientras dure la ventana de invulnerabilidad tras un dano.</summary>
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        /// <summary>
        /// Aplica dano con ventana de invulnerabilidad. Devuelve cuantas vidas se
        /// restaron de verdad (0 si estaba invulnerable o ya sin vidas). Es el punto
        /// de entrada de los enemigos y hazards; el vacio usa <see cref="SetLives"/>.
        /// </summary>
        public int TakeDamage(int amount)
        {
            if (amount <= 0 || IsInvulnerable || Value.IsEmpty)
            {
                return 0;
            }

            _invulnerableUntil = Time.time + Mathf.Max(0f, _invulnerabilitySeconds);

            int applied = 0;
            for (int i = 0; i < amount; i++)
            {
                if (!Value.LoseLife())
                {
                    break;
                }

                applied++;
            }

            return applied;
        }

        /// <summary>Fija las vidas actuales (recortadas a [0, Max]). 0 = perder todas.</summary>
        public void SetLives(int lives) => Value.Set(lives);

        /// <summary>Deja las vidas al maximo (reinicio de partida).</summary>
        public void ResetLives() => Value.Refill();

        void OnEnable()
        {
            Value.Changed += HandleChanged;
        }

        void OnDisable()
        {
            Value.Changed -= HandleChanged;
        }

        void HandleChanged(Lives lives) => Changed?.Invoke(lives);
    }
}
