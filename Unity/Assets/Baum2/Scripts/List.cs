using System;
using System.Collections.Generic;
using System.Linq;
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

        private int count;
        public int Count
        {
            get
            {
                return count;
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

        private int RenderingMin = 0;
        private int RenderingMax = 0;
        private Dictionary<int, CurrentRenderingElement> CurrentRenderingElements = new Dictionary<int, CurrentRenderingElement>();
        private Dictionary<string, List<UIRoot>> Pool = new Dictionary<string, List<UIRoot>>();

        public struct CurrentRenderingElement
        {
            public string Item1;
            public UIRoot Item2;
        }

        public void Awake()
        {
            foreach (var itemSource in ItemSources)
            {
                itemSource.SetActive(false);
            }
            LayoutGroup.Initialize(this);
        }

        private UIRoot CreateObject(string sourceName)
        {
            if (Pool.ContainsKey(sourceName) && Pool[sourceName].Count > 0)
            {
                var e = Pool[sourceName][0];
                e.gameObject.SetActive(true);
                Pool[sourceName].RemoveAt(0);
                return e;
            }

            var original = ItemSources.Find(x => x.name == sourceName);
            var item = Instantiate(original, Content.transform, true);
            item.transform.localPosition = original.transform.localPosition;
            item.transform.localScale = Vector3.one;
            item.SetActive(true);

            var uiRoot = item.AddComponent<UIRoot>();
            var cache = item.AddComponent<Cache>();
            cache.CreateCache(item.transform);
            uiRoot.Awake();

            if (uiFactory != null) uiFactory(sourceName, uiRoot);
            return uiRoot;
        }

        private void AddItem(int index)
        {
            var sourceName = uiSelector(index);
            var item = CreateObject(sourceName);
            CurrentRenderingElements.Add(index, new CurrentRenderingElement{Item1 = sourceName, Item2 = item});
            if (RenderingMin >= index) RenderingMin = index - 1;
            if (RenderingMax <= index) RenderingMax = index + 1;
            if (uiUpdater != null) uiUpdater(item, index);

            var transform1 = item.transform;
            var p = transform1.localPosition;
            p = LayoutGroup.ElementPositions[index];
            transform1.localPosition = p;
        }

        private void ReturnObjectsToPool()
        {
            while (CurrentRenderingElements.Count > 0)
            {
                ToPool(CurrentRenderingElements.First().Key);
            }
            CurrentRenderingElements.Clear();
            RenderingMin = 0;
            RenderingMax = 0;
        }

        private void ToPool(int index)
        {
            var elementName = CurrentRenderingElements[index].Item1;
            var element = CurrentRenderingElements[index].Item2;
            CurrentRenderingElements.Remove(index);
            element.gameObject.SetActive(false);
            if (!Pool.ContainsKey(elementName)) Pool.Add(elementName, new List<UIRoot>());
            Pool[elementName].Add(element);

            if (RenderingMin + 1 == index) RenderingMin = index;
            if (RenderingMax - 1 == index) RenderingMax = index;
        }

        public UIRoot AddItemDirect(string sourceName)
        {
            return CreateObject(sourceName);
        }

        public void Resize(int size)
        {
            count = size;
            LayoutGroup.RequestUpdate();

            ReturnObjectsToPool();
        }

        private readonly Vector3[] fourCornersArray = new Vector3[4];
        private readonly Vector2[] worldCornersEdge = new Vector2[2];

        public void LateUpdate()
        {
            {
                RectTransform.GetLocalCorners(fourCornersArray);
                var localToWorldMatrix = RectTransform.localToWorldMatrix;
                fourCornersArray[0] = localToWorldMatrix.MultiplyPoint(fourCornersArray[0] - new Vector3(LayoutGroup.MaxElementSize.x, LayoutGroup.MaxElementSize.y));
                fourCornersArray[2] = localToWorldMatrix.MultiplyPoint(fourCornersArray[2] + new Vector3(LayoutGroup.MaxElementSize.x, LayoutGroup.MaxElementSize.y));
            }
            worldCornersEdge[0] = fourCornersArray[0];
            worldCornersEdge[1] = fourCornersArray[2];

            while (TryDelete(RenderingMin + 1))
            {
            }
            while (TryDelete(RenderingMax - 1))
            {
            }

            if (RenderingMin == RenderingMax && Count > 0)
            {
                // 1つも描画していないときはどこから描画するか考える
                var i = 0;
                var created = false;
                for (i = 0; i < Count; ++i)
                {
                    if (TryCreate(i))
                    {
                        created = true;
                        break;
                    }
                }
                if (created)
                {
                    RenderingMin = i - 1;
                    RenderingMax = i + 1;
                }
            }

            while (TryCreate(RenderingMin))
            {
            }
            while (TryCreate(RenderingMax))
            {
            }
        }

        private bool TryCreate(int index)
        {
            if (index < 0 || Count <= index) return false;
            var x = ContentRectTransform.localPosition.x + LayoutGroup.ElementPositions[index].x;
            var y = ContentRectTransform.localPosition.y + LayoutGroup.ElementPositions[index].y;
            var p = RectTransform.TransformPoint(new Vector3(x, y, 0f));
            if (worldCornersEdge[0].y < p.y && p.y < worldCornersEdge[1].y)
            {
                AddItem(index);
                return true;
            }
            return false;
        }

        private bool TryDelete(int index)
        {
            if (index <= RenderingMin || RenderingMax <= index) return false;
            var x = ContentRectTransform.localPosition.x + LayoutGroup.ElementPositions[index].x;
            var y = ContentRectTransform.localPosition.y + LayoutGroup.ElementPositions[index].y;
            var p = RectTransform.TransformPoint(new Vector3(x, y, 0f));
            if (!(worldCornersEdge[0].y < p.y && p.y < worldCornersEdge[1].y))
            {
                ToPool(index);
                return true;
            }
            return false;
        }

        public void UpdateItem(int index)
        {
            if (uiUpdater == null) return;
            if (RenderingMin >= index || index >= RenderingMax) return;
            uiUpdater(CurrentRenderingElements[index].Item2, index);
        }

        public void UpdateAll()
        {
            if (uiUpdater == null) return;
            foreach (var element in CurrentRenderingElements)
            {
                uiUpdater(element.Value.Item2, element.Key);
            }
        }

        public void ResetScroll()
        {
            ScrollRect.velocity = Vector2.zero;
            LayoutGroup.ResetScroll();
        }
    }
}

