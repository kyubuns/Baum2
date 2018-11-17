using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Baum2
{
    public class ListLayoutGroup : MonoBehaviour
    {
        [SerializeField]
        public Scroll Scroll;

        public RectOffset Padding;
        public float Spacing;

        private bool Initialized;
        private bool SizeUpdatedFlag;
        private int SaveScrollToIndex = -1;

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

        public void RequestUpdate()
        {
            SizeUpdatedFlag = true;
        }

        public void LateUpdate()
        {
            if (SizeUpdatedFlag)
            {
                UpdateSize();
                SizeUpdatedFlag = false;
            }
            if (SaveScrollToIndex > -1)
            {
                ScrollToIndexInternal(SaveScrollToIndex);
                SaveScrollToIndex = -1;
            }
        }

        private void UpdateSize()
        {
            if (Scroll == Scroll.Vertical)
            {
                var beforeSize = RectTransform.sizeDelta.y;
                var position = RectTransform.anchoredPosition;
                UpdateSize(1, -1, Padding.top, Padding.bottom);
                var afterSize = RectTransform.sizeDelta.y;
                position.y = -(afterSize / 2f - beforeSize / 2f - position.y);
                RectTransform.anchoredPosition = position;
            }
            else if (Scroll == Scroll.Horizontal)
            {
                UpdateSize(0, 1, Padding.left, Padding.right);
            }
        }

        private void UpdateSize(int axis, int vector, float paddingStart, float paddingEnd)
        {
            var size = 0.0f;
            size += paddingStart;
            for (var i = 0; i < RectTransform.childCount; ++i)
            {
                var childTransform = RectTransform.GetChild(i);
                var child = childTransform as RectTransform;
                if (!childTransform.gameObject.activeSelf || child == null) continue;

                var a = child.anchoredPosition;
                a[axis] = (size + (axis == 0 ? child.rect.width : child.rect.height) / 2.0f) * vector;
                child.anchoredPosition = a;
                size += child.sizeDelta[axis];
                if (i != RectTransform.childCount - 1) size += Spacing;
            }
            size += paddingEnd;

            var totalSize = RectTransform.sizeDelta;
            var parentRect = RectTransform.parent.GetComponent<RectTransform>().rect;
            totalSize[axis] = Mathf.Max(size, axis == 0 ? parentRect.width : parentRect.height);
            RectTransform.sizeDelta = totalSize;

            if (!Initialized)
            {
                ResetScroll();
                Initialized = true;
            }
        }

        public void ScrollToIndex(int index)
        {
            SaveScrollToIndex = index;
        }

        private void ScrollToIndexInternal(int index)
        {
            if (Scroll == Scroll.Vertical)
            {
                var contentHeight = RectTransform.sizeDelta.y;
                var y = -contentHeight / 2.0f + transform.parent.GetComponent<RectTransform>().rect.height / 2.0f;

                var size = 0.0f;
                size += Padding.top;
                for (var i = 0; i < index; ++i)
                {
                    var child = RectTransform.GetChild(i) as RectTransform;
                    if (child == null) continue;

                    size += child.sizeDelta.y;
                    size += Spacing;
                }

                RectTransform.anchoredPosition = new Vector2(RectTransform.anchoredPosition.x, y + size);
            }
            else if (Scroll == Scroll.Horizontal)
            {
                var contentWidth = RectTransform.sizeDelta.x;
                var x = contentWidth / 2.0f - transform.parent.GetComponent<RectTransform>().rect.width / 2.0f;
                RectTransform.anchoredPosition = new Vector2(x, RectTransform.anchoredPosition.y);
            }
        }

        public void ResetScroll()
        {
            if (Scroll == Scroll.Vertical)
            {
                var contentHeight = RectTransform.sizeDelta.y;
                var y = -contentHeight / 2.0f + transform.parent.GetComponent<RectTransform>().rect.height / 2.0f;
                RectTransform.anchoredPosition = new Vector2(RectTransform.anchoredPosition.x, y);
            }
            else if (Scroll == Scroll.Horizontal)
            {
                var contentWidth = RectTransform.sizeDelta.x;
                var x = contentWidth / 2.0f - transform.parent.GetComponent<RectTransform>().rect.width / 2.0f;
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
