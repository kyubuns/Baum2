using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable InconsistentNaming

namespace Baum2
{
    public class List : MonoBehaviour
    {
        [SerializeField]
        public List<GameObject> ItemSources;

        [SerializeField]
        public ListLayoutGroup LayoutGroup;

        private GameObject contentCache;
        public GameObject Content
        {
            get
            {
                if (contentCache != null) return contentCache;
                contentCache = gameObject.transform.Find("Content").gameObject;
                return contentCache;
            }
        }

        private RectTransform contentRectTransformCache;
        public RectTransform ContentRectTransform
        {
            get
            {
                if (contentRectTransformCache != null) return contentRectTransformCache;
                contentRectTransformCache = Content.GetComponent<RectTransform>();
                return contentRectTransformCache;
            }
        }

        private RectTransform rectTransformCache;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransformCache != null) return rectTransformCache;
                rectTransformCache = GetComponent<RectTransform>();
                return rectTransformCache;
            }
        }

        private ScrollRect scrollRectCache;
        public ScrollRect ScrollRect
        {
            get
            {
                if (scrollRectCache != null) return scrollRectCache;
                scrollRectCache = GetComponent<ScrollRect>();
                return scrollRectCache;
            }
        }

        public Scrollbar Scrollbar
        {
            set
            {
                if (LayoutGroup.Scroll == Scroll.Horizontal) ScrollRect.horizontalScrollbar = value;
                else if (LayoutGroup.Scroll == Scroll.Vertical) ScrollRect.verticalScrollbar = value;
                else throw new ApplicationException("LayoutGroup not found");
            }
        }

        public int Count
        {
            get
            {
                return Items.Count;
            }
        }

        private Func<int, string> uiSelector;
        public Func<int, string> UISelector
        {
            get
            {
                return uiSelector;
            }
            set
            {
                uiSelector = value;
                UpdateAll();
            }
        }

        private Action<string, UIRoot> uiFactory;
        public Action<string, UIRoot> UIFactory
        {
            get
            {
                return uiFactory;
            }
            set
            {
                uiFactory = value;
            }
        }

        private Action<UIRoot, int> uiUpdater;
        public Action<UIRoot, int> UIUpdater
        {
            get
            {
                return uiUpdater;
            }
            set
            {
                uiUpdater = value;
                UpdateAll();
            }
        }

        private readonly List<UIRoot> Items = new List<UIRoot>();

        private UIRoot AddItem(string sourceName)
        {
            var original = ItemSources.Find(x => x.name == sourceName);
            var item = Instantiate(original);
            item.transform.SetParent(Content.transform);
            item.transform.localPosition = original.transform.localPosition;
            item.transform.localScale = Vector3.one;
            item.SetActive(true);

            var uiRoot = item.AddComponent<UIRoot>();
            var cache = item.AddComponent<Cache>();
            cache.CreateCache(item.transform);
            uiRoot.Awake();

            Items.Add(uiRoot);
            LayoutGroup.RequestUpdate();

            return uiRoot;
        }

        public void Resize(int size)
        {
            foreach (Transform item in Content.transform)
            {
                Destroy(item.gameObject);
            }
            Items.Clear();

            for (var i = 0; i < size; ++i)
            {
                var sourceName = uiSelector(i);
                var item = AddItem(sourceName);
                if (uiFactory != null) uiFactory(sourceName, item);
                if (uiUpdater != null) uiUpdater(item, i);
            }
        }

        public void UpdateItem(int index)
        {
            if (uiUpdater != null) uiUpdater(Items[index], index);
        }

        public void UpdateAll()
        {
            if (uiUpdater == null) return;
            for (var i = 0; i < Items.Count; ++i)
            {
                uiUpdater(Items[i], i);
            }
        }
    }
}
