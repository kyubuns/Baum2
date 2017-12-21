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
        private static readonly char[] SplitChar = { '/' };

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
            var elements = path.Split(SplitChar);
            Array.Reverse(elements);
            Assert.AreNotEqual(elements.Length, 0, "Baum2.Cache.Get path.Length != 0");

            var cand = List.FindAll(x => x.Name == elements[0]);
            for (var i = cand.Count - 1; i >= 0; --i)
            {
                var pass = true;
                for (var j = 1; j < elements.Length; ++j)
                {
                    if (cand[i].Path.Length > j - 1 && cand[i].Path[j - 1] == elements[j]) continue;
                    pass = false;
                    break;
                }
                if (!pass) cand.RemoveAt(i);
            }
            if (cand.Count == 0) return null;
            Assert.AreEqual(cand.Count, 1, string.Format("[Baum2] \"{0}\" is not unique", path));
            return cand[0].GameObject;
        }
    }
}