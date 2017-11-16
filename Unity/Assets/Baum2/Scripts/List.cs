using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Baum2
{
    public class List : MonoBehaviour
    {
        [SerializeField]
        public List<GameObject> ItemSources;

        public Action OnSizeChanged;

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

        private LayoutGroup layoutGroupCache;
        public LayoutGroup LayoutGroup
        {
            get
            {
                if (layoutGroupCache != null) return layoutGroupCache;
                layoutGroupCache = Content.GetComponent<LayoutGroup>();
                return layoutGroupCache;
            }
        }

        private Scrollbar scrollbar;
        public Scrollbar Scrollbar
        {
            get
            {
                return scrollbar;
            }
            set
            {
                scrollbar = value;

                if (LayoutGroup is HorizontalLayoutGroup) ScrollRect.horizontalScrollbar = scrollbar;
                else if (LayoutGroup is VerticalLayoutGroup) ScrollRect.verticalScrollbar = scrollbar;
                else throw new ApplicationException("LayoutGroup not found");
            }
        }

        public float Spacing
        {
            get
            {
                if (LayoutGroup is HorizontalLayoutGroup) return ((HorizontalLayoutGroup)LayoutGroup).spacing;
                if (LayoutGroup is VerticalLayoutGroup) return ((VerticalLayoutGroup)LayoutGroup).spacing;
                throw new ApplicationException("LayoutGroup not found");
            }
            set
            {
                if (LayoutGroup is HorizontalLayoutGroup) ((HorizontalLayoutGroup)LayoutGroup).spacing = value;
                else if (LayoutGroup is VerticalLayoutGroup) ((VerticalLayoutGroup)LayoutGroup).spacing = value;
                else throw new ApplicationException("LayoutGroup not found");
            }
        }

        public RectOffset Padding
        {
            get
            {
                return LayoutGroup.padding;
            }
            set
            {
                LayoutGroup.padding = value;
            }
        }

        public int Count
        {
            get
            {
                return items.Count;
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

        private Action<UIRoot, int> uiFactory;
        public Action<UIRoot, int> UIFactory
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

        private int itemSize;
        private bool updateSize;
        private readonly List<UIRoot> items = new List<UIRoot>();

        private UIRoot AddItem(string sourceName)
        {
            var item = Instantiate(ItemSources.Find(x => x.name == sourceName));
            item.transform.SetParent(Content.transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localScale = Vector3.one;
            item.SetActive(true);
            updateSize = true;

            var uiRoot = item.AddComponent<UIRoot>();
            var cache = item.AddComponent<Cache>();
            cache.CreateCache(item.transform);
            uiRoot.Awake();

            items.Add(uiRoot);

            return uiRoot;
        }

        public void Resize(int size)
        {
            itemSize = size;

            foreach (Transform item in Content.transform)
            {
                Destroy(item.gameObject);
            }
            items.Clear();

            for (var i = 0; i < size; ++i)
            {
                var item = AddItem(uiSelector(i));
                if (uiFactory != null) uiFactory(item, i);
                if (uiUpdater != null) uiUpdater(item, i);
            }
        }

        public void UpdateItem(int index)
        {
            if (uiUpdater != null) uiUpdater(items[index], index);
        }

        public void UpdateAll()
        {
            for (var i = 0; i < items.Count; ++i)
            {
                if (uiUpdater != null) uiUpdater(items[i], i);
            }
        }

        public void LateUpdate()
        {
            if (!updateSize) return;
            updateSize = false;

            // サイズ調整
            var axis = 1;
            if (LayoutGroup is VerticalLayoutGroup)
            {
                axis = 1;
            }
            else if (LayoutGroup is HorizontalLayoutGroup)
            {
                axis = 0;
            }

            var scrollSize = ContentRectTransform.sizeDelta;
            scrollSize[axis] = Mathf.Max(CalcContentSize(axis), RectTransform.sizeDelta[axis]);
            ContentRectTransform.sizeDelta = scrollSize;

            if (LayoutGroup is VerticalLayoutGroup)
            {
                ScrollRect.horizontalNormalizedPosition = 1.0f;
                ScrollRect.verticalNormalizedPosition = 1.0f;
            }
            else if (LayoutGroup is HorizontalLayoutGroup)
            {
                ScrollRect.horizontalNormalizedPosition = 0.0f;
                ScrollRect.verticalNormalizedPosition = 1.0f;
            }

            if (OnSizeChanged != null) OnSizeChanged();
        }

        private float CalcContentSize(int axis)
        {
            var result = Enumerable.Range(0, itemSize)
                .Select(i => UISelector(i))
                .Select(s => ItemSources.Find(x => x.name == s))
                .Sum(x => x.GetComponent<RectTransform>().sizeDelta[axis]);
            result += Spacing * (itemSize - 1);
            if (axis == 1) result += Padding.top + Padding.bottom;
            if (axis == 0) result += Padding.left + Padding.right;
            return result;
        }
    }
}
