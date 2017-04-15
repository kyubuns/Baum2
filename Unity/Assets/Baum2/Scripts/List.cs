using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Baum2
{
	public class List : MonoBehaviour
	{
		[SerializeField] private GameObject itemSource;
		public GameObject ItemSource { private get { return itemSource; } set { itemSource = value; } }

		private GameObject contentCache;
		public GameObject Content
		{
			get
			{
				if (contentCache == null) contentCache = gameObject.transform.Find("Content").gameObject;
				return contentCache;
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

		public float Spacing
		{
			get
			{
				if (LayoutGroup is HorizontalLayoutGroup) return ((HorizontalLayoutGroup)LayoutGroup).spacing;
				else if (LayoutGroup is VerticalLayoutGroup) return ((VerticalLayoutGroup)LayoutGroup).spacing;
				else throw new Exception("LayoutGroup not found");
			}
			set
			{
				if (LayoutGroup is HorizontalLayoutGroup) ((HorizontalLayoutGroup)LayoutGroup).spacing = value;
				else if (LayoutGroup is VerticalLayoutGroup) ((VerticalLayoutGroup)LayoutGroup).spacing = value;
				else throw new Exception("LayoutGroup not found");
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
		private RectTransform cachedRectTransform;
		private Action<UIRoot, int> itemFactory;
		private List<UIRoot> items;
		
		private UIRoot AddItem()
		{
			var item = Instantiate(ItemSource);
			item.transform.SetParent(GetComponent<ScrollRect>().content.transform);
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
		
		public void Init(int size, Action<UIRoot, int> factory)
		{
			items = new List<UIRoot>();
			itemFactory = factory;
			for(int i=0;i<size;++i)
			{
				var item = AddItem();
				itemFactory(item, i);
			}
		}

		public void Add()
		{
			var item = AddItem();
			itemFactory(item, items.Count - 1);
		}

		public void Resize(int size)
		{
			throw new NotImplementedException();
		}

		public void UpdateItem(int index)
		{
			itemFactory(items[index], index);
		}

		public void UpdateAll()
		{
			for (int i = 0; i < items.Count; ++i)
			{
				itemFactory(items[i], i);
			}
		}
		
		public void LateUpdate()
		{
			if(!updateSize) return;
			updateSize = false;
			
			// サイズ調整
			if(cachedRectTransform == null) cachedRectTransform = Content.GetComponent<RectTransform>();

			var axis = 1;
			if (LayoutGroup is VerticalLayoutGroup) { axis = 1; }
			else if (LayoutGroup is HorizontalLayoutGroup) { axis = 0; }

			var scrollSize = cachedRectTransform.sizeDelta;
			scrollSize[axis] = LayoutUtility.GetPreferredSize(cachedRectTransform, axis);
			cachedRectTransform.sizeDelta = scrollSize;

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
		}
	}
}
