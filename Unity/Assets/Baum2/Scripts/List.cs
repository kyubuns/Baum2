using System;
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
				if (contentCache == null) contentCache = gameObject.transform.Find("Content").gameObject;
				return contentCache;
			}
		}

		private RectTransform contentRectTransformCache;
		public RectTransform ContentRectTransform
		{
			get
			{
				if (contentRectTransformCache == null) contentRectTransformCache = Content.GetComponent<RectTransform>();
				return contentRectTransformCache;
			}
		}

		private RectTransform rectTransformCache;
		public RectTransform RectTransform
		{
			get
			{
				if (rectTransformCache == null) rectTransformCache = GetComponent<RectTransform>();
				return rectTransformCache;
			}
		}

		private ScrollRect scrollRectCache;
		public ScrollRect ScrollRect
		{
			get
			{
				if (scrollRectCache == null) scrollRectCache = GetComponent<ScrollRect>();
				return scrollRectCache;
			}
		}

		private LayoutGroup layoutGroupCache;
		public LayoutGroup LayoutGroup
		{
			get
			{
				if (layoutGroupCache == null) layoutGroupCache = Content.GetComponent<LayoutGroup>();
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
				else if (LayoutGroup is VerticalLayoutGroup) return ((VerticalLayoutGroup)LayoutGroup).spacing;
				else throw new ApplicationException("LayoutGroup not found");
			}
			set
			{
				if (LayoutGroup is HorizontalLayoutGroup) ((HorizontalLayoutGroup)LayoutGroup).spacing = value;
				else if (LayoutGroup is VerticalLayoutGroup) ((VerticalLayoutGroup)LayoutGroup).spacing = value;
				else throw new ApplicationException("LayoutGroup not found");
			}
		}

		public int Count
		{
			get
			{
				return items.Count;
			}
		}

		private bool updateSize;
		private Func<int, string> uiSelector;
		private Action<UIRoot, int> uiFactory;
		private List<UIRoot> items;

		private UIRoot AddItem(string sourceName)
		{
			var item = Instantiate(ItemSources.Find(x => x.name == sourceName));
			item.transform.SetParent(Content.transform);
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

		public void Init(int size, Func<int, string> uiSelector, Action<UIRoot, int> uiFactory)
		{
			items = new List<UIRoot>();
			this.uiSelector = uiSelector;
			this.uiFactory = uiFactory;
			for (int i = 0; i < size; ++i)
			{
				var item = AddItem(uiSelector(i));
				uiFactory(item, i);
			}
		}

		public void Resize(int size)
		{
			foreach (Transform item in Content.transform)
			{
				Destroy(item.gameObject);
			}
			Init(size, uiSelector, uiFactory);
		}

		public void UpdateItem(int index)
		{
			uiFactory(items[index], index);
		}

		public void UpdateAll()
		{
			for (int i = 0; i < items.Count; ++i)
			{
				uiFactory(items[i], i);
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
			LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRectTransform);
			scrollSize[axis] = Mathf.Max(LayoutUtility.GetPreferredSize(ContentRectTransform, axis), RectTransform.sizeDelta[axis]);
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
	}
}
