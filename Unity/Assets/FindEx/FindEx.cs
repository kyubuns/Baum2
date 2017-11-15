using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace FindEx
{
    public static class TransformExtensions
    {
        public static Transform FindEx(this Transform transform, string path)
        {
            var elements = path.Split('/');
            var targets = new List<Transform> { transform };
            for (var i = 0; i < elements.Length; i++)
            {
                var result = new List<Transform>();
                foreach (var target in targets)
                {
                    result.AddRange(target.FindChildren(elements[i]));
                }
                targets = result;
            }

            Assert.IsFalse(targets.Count > 1, string.Format("対象のTransformが複数見つかりました。Path:{0}", path));
            if (targets.Count == 0)
            {
                return null;
            }
            return targets[0];
        }

        public static T FindEx<T>(this Transform transform, string path) where T : Component
        {
            return FindEx(transform, path).GetComponent<T>();
        }

        public static Transform FindEx(this Component component, string path)
        {
            return FindEx(component.transform, path);
        }

        public static T FindEx<T>(this Component component, string path) where T : Component
        {
            return FindEx(component.transform, path).GetComponent<T>();
        }

        public static List<Transform> FindChildren(this Transform transform, string name)
        {
            var result = new List<Transform>();
            for (var i = 0; i < transform.childCount; ++i)
            {
                var child = transform.GetChild(i);
                if (child.name == name)
                {
                    result.Add(child);
                    continue;
                }

                var found = child.FindChildren(name);
                if (found != null)
                {
                    result.AddRange(found);
                }
            }
            return result;
        }
    }
}
