using UnityEngine;

namespace OrbWanderer.Utils
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Inherit from this to avoid boilerplate singleton code.
    /// Usage: public class MyManager : Singleton<MyManager> { }
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object lockObj = new object();
        private static bool isQuitting;

        public static T Instance
        {
            get
            {
                if (isQuitting)
                    return null;

                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = FindFirstObjectByType<T>();

                        if (instance == null)
                        {
                            var go = new GameObject(typeof(T).Name);
                            instance = go.AddComponent<T>();
                        }
                    }
                    return instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit()
        {
            isQuitting = true;
        }
    }
}
