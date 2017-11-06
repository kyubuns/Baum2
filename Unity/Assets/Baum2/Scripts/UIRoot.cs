using UnityEngine;
using UnityEngine.Assertions;

namespace Baum2
{
	public class UIRoot : MonoBehaviour
	{
		Cache cache;

		public void Awake()
		{
			cache = GetComponent<Cache>();
		}

		public GameObject Get(string name)
		{
			var go = cache.Get(name);
			Assert.IsNotNull(go, string.Format("[Baum2] \"{0}\" is not found", name));
			return go;
		}

		public T Get<T>(string name, bool noError = false) where T : Component
		{
			var go = cache.Get(name);
			if (!noError) Assert.IsNotNull(go, string.Format("[Baum2] \"{0}\" is not found", name));
			if (go == null) return null;

			var t = GetComponent<T>(go);
			if (!noError) Assert.IsNotNull(t, string.Format("[Baum2] \"{0}<{1}>\" is not found", name, typeof(T).Name));
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