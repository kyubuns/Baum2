using UnityEngine;
using UnityEngine.Assertions;

namespace Baum2
{
    public class UIRoot : MonoBehaviour
    {
        private Cache cache;

        public void Awake()
        {
            cache = GetComponent<Cache>();
        }

        public GameObject Get(string gameObjectName)
        {
            var go = cache.Get(gameObjectName);
            Assert.IsNotNull(go, string.Format("[Baum2] \"{0}\" is not found", gameObjectName));
            return go;
        }

        public T Get<T>(string gameObjectName, bool noError = false) where T : Component
        {
            var go = cache.Get(gameObjectName);
            if (!noError) Assert.IsNotNull(go, string.Format("[Baum2] \"{0}\" is not found", gameObjectName));
            if (go == null) return null;

            var t = GetComponent<T>(go);
            if (!noError) Assert.IsNotNull(t, string.Format("[Baum2] \"{0}<{1}>\" is not found", gameObjectName, typeof(T).Name));
            return t;
        }

        private static T GetComponent<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            return go.GetComponent<T>();
        }

        public void Reload()
        {
            cache.List.Clear();
            cache.CreateCache(transform);
        }
    }
}