using UnityEngine;

namespace PixelIsland.Core
{
    /// <summary>
    /// Patron Singleton. Expone una unica instancia global y la marca
    /// DontDestroyOnLoad para que sobreviva a los cambios de escena.
    /// Se usara en GameManager (Etapa 3) y LevelManager (Etapa 8).
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        static T s_instance;

        /// <summary>True si ya hay una instancia primaria viva.</summary>
        public static bool HasInstance => s_instance != null;

        /// <summary>
        /// Instancia primaria. Busca una en la escena la primera vez que se pide
        /// para que el orden de arranque de los scripts no importa.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (s_instance != null)
                {
                    return s_instance;
                }

                s_instance = FindObjectOfType<T>();
                if (s_instance == null)
                {
                    Debug.LogError("[Singleton] No hay ninguna instancia de " + typeof(T).Name +
                                   " en la escena cargada.");
                }

                return s_instance;
            }
        }

        protected virtual void Awake()
        {
            if (s_instance != null && !ReferenceEquals(s_instance, this))
            {
                Debug.LogWarning("[Singleton] " + name + " se descarta: ya existe " +
                                 s_instance.name + ".");
                Destroy(gameObject);
                return;
            }

            // GetComponent en vez de un cast: T no es derivable de Singleton<T>
            // en tiempo de compilacion, pero el componente vive en este mismo
            // GameObject, asi que siempre devuelve esta misma instancia.
            T self = GetComponent<T>();
            if (self == null)
            {
                Debug.LogError("[Singleton] " + name + " no puede resolver su propia instancia de " +
                               typeof(T).Name + ".");
                return;
            }

            s_instance = self;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (ReferenceEquals(s_instance, this))
            {
                s_instance = null;
            }
        }
    }
}
