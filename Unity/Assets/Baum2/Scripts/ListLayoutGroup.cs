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
        private readonly Dictionary<string, Vector2> ElementSize = new Dictionary<string, Vector2>();
        public Vector2 MaxElementSize { get; private set; }
        public List<Vector2> ElementPositions { get; private set; }

        public void Initialize(List root)
        {
            Root = root;
            foreach (var itemSource in Root.ItemSources)
            {
                var child = itemSource.transform as RectTransform;
                if (child != null) ElementSize[itemSource.name] = new Vector2(child.rect.width, child.rect.height);
            }
            MaxElementSize = new Vector2(ElementSize.Values.Max(s => s.x), ElementSize.Values.Max(s => s.y));

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
                var beforeSize = RectTransform.sizeDelta.x;
                var position = RectTransform.anchoredPosition;
                RecalcSizeInternal(0, +1, Padding.left, Padding.right);
                var afterSize = RectTransform.sizeDelta.x;
                position.x = -(afterSize / 2f - beforeSize / 2f - position.x);
                RectTransform.anchoredPosition = position;
                if (!Initialized)
                {
                    Initialized = true;
                    ResetScroll();
                }
            }
        }

        private void RecalcSizeInternal(int axis, int vector, float paddingStart, float paddingEnd)
        {
            if (ElementPositions == null) ElementPositions = new List<Vector2>();
            ElementPositions.Clear();

            var size = Vector2.zero;
            size[axis] += paddingStart;
            for (var i = 0; i < Root.Count; ++i)
            {
                var select = Root.UISelector(i);
                var elementSize = ElementSize[select];
                if (axis == 0)
                {
                    size[1] = Mathf.Max(size[1], elementSize[1]);
                }
                else
                {
                    size[0] = Mathf.Max(size[0], elementSize[0]);
                }
                size[axis] += elementSize[axis];
                if (i != 0 && SpecialPadding.ContainsKey(select))
                {
                    size[axis] += SpecialPadding[select];
                }
                var posx = (axis == 0)? size.x * vector - elementSize.x / 2f * vector : elementSize.x / 2f * vector;
                var posy = (axis == 1)? size.y * vector - elementSize.y / 2f * vector : elementSize.y / 2f * vector;
                ElementPositions.Add(new Vector2(posx, posy));
                if (i != Root.Count - 1) size[axis] += Spacing;
            }
            size[axis] += paddingEnd;

            var totalSize = RectTransform.sizeDelta;
            var parentRect = RectTransformParent.rect;
            size[axis] = Mathf.Max(size[axis], axis == 0 ? parentRect.width : parentRect.height);
            totalSize = size;
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
