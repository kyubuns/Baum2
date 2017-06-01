using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace Baum2
{
	public class Cache : MonoBehaviour
	{
		[SerializeField]
		public List<CachedGameObject> List = new List<CachedGameObject>();
		private static readonly char[] split = { '/' };

		[Serializable]
		public class CachedGameObject
		{
			public CachedGameObject(string name, GameObject go, List<string> path)
			{
				Name = name;
				GameObject = go;
				Path = path.ToArray();
				Array.Reverse(Path);
			}

			public string Name;
			public GameObject GameObject;
			public string[] Path;
		}

		public void CreateCache(Transform root, List<string> route = null)
		{
			if (route == null) route = new List<string>();
			List.Add(new CachedGameObject(root.name, root.gameObject, route));
			route.Add(root.name);

			foreach (Transform t in root) CreateCache(t, route);
			route.RemoveAt(route.Count - 1);
		}

		public GameObject Get(string path)
		{
			var elements = path.Split(split);
			Array.Reverse(elements);
			Assert.AreNotEqual(elements.Length, 0, "Baum2.Cache.Get path.Length != 0");

			var cand = List.FindAll(x => x.Name == elements[0]);
			for (int i = cand.Count - 1; i >= 0; --i)
			{
				bool pass = true;
				for (int j = 1; j < elements.Length; ++j)
				{
					if (cand[i].Path.Length <= j - 1 || cand[i].Path[j - 1] != elements[j])
					{
						pass = false;
						break;
					}
				}
				if (!pass) cand.RemoveAt(i);
			}
			if (cand.Count == 0) return null;
			Assert.AreEqual(cand.Count, 1, string.Format("[Baum2] \"{0}\" is not unique", path));
			return cand[0].GameObject;
		}
	}
}