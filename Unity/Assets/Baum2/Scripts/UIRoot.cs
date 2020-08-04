using UnityEngine;
using UnityEngine.Assertions;

namespace Baum2
{
    public class UIRoot : MonoBehaviour, IMapper
    {
        private Cache cache;

        public Cache Raw
        {
            get { return cache; }
        }

        public void Awake()
        {
            cache = GetComponent<Cache>();
        }

        public GameObject Get()
        {
            return gameObject;
        }

        public GameObject Get(string gameObjectName, bool noError = false)
        {
            var go = cache.Get(gameObjectName);
            if (!noError) Assert.IsNotNull(go, string.Format("[Baum2] \"{0}\" is not found", gameObjectName));
            return go;
        }

        public T Get<T>(string gameObjectName) where T : Component
        {
            return Get<T>(gameObjectName, false);
        }

        public T Get<T>(string gameObjectName, bool noError) where T : Component
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

        public static UIRoot CreateWithCache(GameObject go)
        {
            Assert.IsNull(go.GetComponent<UIRoot>());

            var uiRoot = go.AddComponent<UIRoot>();
            var cache = go.AddComponent<Cache>();
            cache.CreateCache(go.transform);
            uiRoot.Awake();

            return uiRoot;
        }

        public static void SetupCache(GameObject go)
        {
            Assert.IsNull(go.GetComponent<UIRoot>());
            var uiRoot = go.AddComponent<UIRoot>();
            go.AddComponent<Cache>();
            uiRoot.Awake();
        }

        public static UIRoot UpdateCache(GameObject go)
        {
            Assert.IsNotNull(go.GetComponent<UIRoot>());

            var uiRoot = go.GetComponent<UIRoot>();
            var cache = go.GetComponent<Cache>();
            cache.ClearCache();
            cache.CreateCache(go.transform);
            uiRoot.Awake();

            return uiRoot;
        }

        public IMapper GetChildMap(string prefix)
        {
            return new UIMapper(this, prefix);
        }

        public void RecalculateBounds()
        {
            RecalculateBounds(new Vector2(0f, 0f));
        }

        public void RecalculateBounds(Vector2 margin)
        {
            var rootTransform = gameObject.GetComponent<RectTransform>();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rootTransform);
            rootTransform.sizeDelta = new Vector2(rootTransform.sizeDelta.x + margin.x, bounds.size.y + margin.y);
        }
    }

    public interface IMapper
    {
        GameObject Get();
        GameObject Get(string gameObjectName, bool noError = false);
        T Get<T>(string gameObjectName) where T : Component;
        T Get<T>(string gameObjectName, bool noError) where T : Component;
        IMapper GetChildMap(string prefix);
        T GetComponent<T>();
    }

    public class UIMapper : IMapper
    {
        private readonly UIRoot Root;
        private readonly string Prefix;

        public UIMapper(UIRoot root, string prefix)
        {
            Root = root;
            Prefix = prefix;
        }

        public GameObject Get()
        {
            return Root.Get($"{Prefix}");
        }

        public GameObject Get(string gameObjectName, bool noError = false)
        {
            return Root.Get($"{Prefix}/{gameObjectName}", noError);
        }

        public T Get<T>(string gameObjectName) where T : Component
        {
            return Root.Get<T>($"{Prefix}/{gameObjectName}");
        }

        public T Get<T>(string gameObjectName, bool noError) where T : Component
        {
            return Root.Get<T>($"{Prefix}/{gameObjectName}", noError);
        }

        public IMapper GetChildMap(string prefix)
        {
            return new UIMapper(Root, $"{Prefix}/{prefix}");
        }

        public T GetComponent<T>()
        {
            return Get().GetComponent<T>();
        }
    }
}
