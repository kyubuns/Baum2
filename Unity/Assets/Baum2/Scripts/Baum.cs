using UnityEngine;

namespace Baum2
{
    public static class BaumUI
    {
        public static UIRoot Instantiate(GameObject parent, GameObject prefab)
        {
            var go = GameObject.Instantiate(prefab);
            var root = go.GetComponent<UIRoot>();
            root.Awake();

            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            return root;
        }
    }
}