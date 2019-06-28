using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Baum2
{
    public class ListLayoutGroup : MonoBehaviour
    {
        [SerializeField]
        public Scroll Scroll;

        private bool Initialized;
        private List Root;
        private readonly Dictionary<string, float> ElementSize = new Dictionary<string, float>();
        public float MaxElementSize { get; private set; }
        public List<float> ElementPositions { get; private set; }

        public void Initialize(List root)
        {
            Root = root;
            foreach (var itemSource in Root.ItemSources)
            {
                var child = itemSource.transform as RectTransform;
                if (child != null) ElementSize[itemSource.name] = child.rect.height;
            }
            MaxElementSize = ElementSize.Values.Max();

            RectTransformParent = RectTransform.parent.GetComponent<RectTransform>();
            Parent = transform.parent.GetComponent<RectTransform>();
        }

        public RectOffset Padding;
        public float Spacing;
        public Dictionary<string, float> SpecialPadding = new Dictionary<string, float>();

        private RectTransform rectTransformCache;
        private RectTransform Parent;
        private RectTransform RectTransformParent;

        public RectTransform RectTransform
        {
            get
            {
                if (rectTransformCache != null) return rectTransformCache;
                rectTransformCache = GetComponent<RectTransform>();
                return rectTransformCache;
            }
        }

        public void RequestUpdate()
        {
            RecalcSize();
        }

        private void RecalcSize()
        {
            if (Scroll == Scroll.Vertical)
            {
                var beforeSize = RectTransform.sizeDelta.y;
                var position = RectTransform.anchoredPosition;
                RecalcSizeInternal(1, -1, Padding.top, Padding.bottom);
                var afterSize = RectTransform.sizeDelta.y;
                position.y = -(afterSize / 2f - beforeSize / 2f - position.y);
                RectTransform.anchoredPosition = position;
                if (!Initialized)
                {
                    Initialized = true;
                    ResetScroll();
                }
            }
            else if (Scroll == Scroll.Horizontal)
            {
                throw new NotImplementedException();
            }
        }

        private void RecalcSizeInternal(int axis, int vector, float paddingStart, float paddingEnd)
        {
            if (ElementPositions == null) ElementPositions = new List<float>();
            ElementPositions.Clear();

            var size = 0.0f;
            size += paddingStart;
            for (var i = 0; i < Root.Count; ++i)
            {
                var select = Root.UISelector(i);
                var elementSize = ElementSize[select];
                size += elementSize;
                if (i != 0 && SpecialPadding.ContainsKey(select))
                {
                    size += SpecialPadding[select];
                }
                ElementPositions.Add(size * vector - elementSize / 2f * vector);
                if (i != Root.Count - 1) size += Spacing;
            }
            size += paddingEnd;

            var totalSize = RectTransform.sizeDelta;
            var parentRect = RectTransformParent.rect;
            size = Mathf.Max(size, axis == 0 ? parentRect.width : parentRect.height);
            totalSize[axis] = size;
            RectTransform.sizeDelta = totalSize;

            for (var i = 0; i < ElementPositions.Count; ++i)
            {
                ElementPositions[i] -= (size / 2f * vector);
            }
        }

        public void ResetScroll()
        {
            if (Scroll == Scroll.Vertical)
            {
                var contentHeight = RectTransform.sizeDelta.y;
                var y = -contentHeight / 2.0f + Parent.rect.height / 2.0f;
                RectTransform.anchoredPosition = new Vector2(RectTransform.anchoredPosition.x, y);
            }
            else if (Scroll == Scroll.Horizontal)
            {
                var contentWidth = RectTransform.sizeDelta.x;
                var x = contentWidth / 2.0f - Parent.rect.width / 2.0f;
                RectTransform.anchoredPosition = new Vector2(x, RectTransform.anchoredPosition.y);
            }
        }
    }

    public enum Scroll
    {
        Vertical,
        Horizontal
    }
}
