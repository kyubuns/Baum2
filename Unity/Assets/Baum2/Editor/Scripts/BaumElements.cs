using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Baum2.Editor
{
    public static class ElementFactory
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, Element, Element>> Generator = new Dictionary<string, Func<Dictionary<string, object>, Element, Element>>()
        {
            { "Root", (d, p) => new RootElement(d, p) },
            { "Image", (d, p) => new ImageElement(d, p) },
            { "Mask", (d, p) => new MaskElement(d, p) },
            { "Group", (d, p) => new GroupElement(d, p) },
            { "Text", (d, p) => new TextElement(d, p) },
            { "Button", (d, p) => new ButtonElement(d, p) },
            { "List", (d, p) => new ListElement(d, p) },
            { "Slider", (d, p) => new SliderElement(d, p) },
            { "Scrollbar", (d, p) => new ScrollbarElement(d, p) },
            { "Toggle", (d, p) => new ToggleElement(d, p) },
        };

        public static Element Generate(Dictionary<string, object> json, Element parent)
        {
            var type = json.Get("type");
            Assert.IsTrue(Generator.ContainsKey(type), "[Baum2] Unknown type: " + type);
            return Generator[type](json, parent);
        }
    }

    public abstract class Element
    {
        public string name;
        protected string pivot;
        protected bool stretchX;
        protected bool stretchY;
        protected Element parent;

        public abstract GameObject Render(Renderer renderer);
        public abstract Area CalcArea();

        protected Element(Dictionary<string, object> json, Element parent)
        {
            this.parent = parent;
            name = json.Get("name");
            if (json.ContainsKey("pivot")) pivot = json.Get("pivot");
            if (json.ContainsKey("stretchxy") || json.ContainsKey("stretchx") || (parent != null ? parent.stretchX : false)) stretchX = true;
            if (json.ContainsKey("stretchxy") || json.ContainsKey("stretchy") || (parent != null ? parent.stretchY : false)) stretchY = true;
        }

        protected GameObject CreateUIGameObject(Renderer renderer)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            return go;
        }

        protected void SetPivot(GameObject root, Renderer renderer)
        {
            if (string.IsNullOrEmpty(pivot)) pivot = "none";

            var rect = root.GetComponent<RectTransform>();
            var pivotMin = rect.anchorMin;
            var pivotMax = rect.anchorMax;
            var sizeDelta = rect.sizeDelta;

            if (pivot.Contains("bottom"))
            {
                pivotMin.y = 0.0f;
                pivotMax.y = 0.0f;
                sizeDelta.y = CalcArea().Height;
            }
            else if (pivot.Contains("top"))
            {
                pivotMin.y = 1.0f;
                pivotMax.y = 1.0f;
                sizeDelta.y = CalcArea().Height;
            }
            else if (pivot.Contains("middle"))
            {
                pivotMin.y = 0.5f;
                pivotMax.y = 0.5f;
                sizeDelta.y = CalcArea().Height;
            }
            if (pivot.Contains("left"))
            {
                pivotMin.x = 0.0f;
                pivotMax.x = 0.0f;
                sizeDelta.x = CalcArea().Width;
            }
            else if (pivot.Contains("right"))
            {
                pivotMin.x = 1.0f;
                pivotMax.x = 1.0f;
                sizeDelta.x = CalcArea().Width;
            }
            else if (pivot.Contains("center"))
            {
                pivotMin.x = 0.5f;
                pivotMax.x = 0.5f;
                sizeDelta.x = CalcArea().Width;
            }

            rect.anchorMin = pivotMin;
            rect.anchorMax = pivotMax;
            rect.sizeDelta = sizeDelta;
        }

        protected void SetStretch(GameObject root, Renderer renderer)
        {
            if (!stretchX && !stretchY) return;

            var parentSize = parent != null ? parent.CalcArea().Size : renderer.CanvasSize;
            var rect = root.GetComponent<RectTransform>();
            var pivotPosMin = new Vector2(0.5f, 0.5f);
            var pivotPosMax = new Vector2(0.5f, 0.5f);
            var sizeDelta = rect.sizeDelta;

            if (stretchX)
            {
                pivotPosMin.x = 0.0f;
                pivotPosMax.x = 1.0f;
                sizeDelta.x = CalcArea().Width - parentSize.x;
            }

            if (stretchY)
            {
                pivotPosMin.y = 0.0f;
                pivotPosMax.y = 1.0f;
                sizeDelta.y = CalcArea().Height - parentSize.y;
            }

            rect.anchorMin = pivotPosMin;
            rect.anchorMax = pivotPosMax;
            rect.sizeDelta = sizeDelta;
        }
    }

    public class GroupElement : Element
    {
        protected readonly List<Element> elements;
        private Area areaCache;

        public GroupElement(Dictionary<string, object> json, Element parent, bool resetStretch = false) : base(json, parent)
        {
            elements = new List<Element>();
            var jsonElements = json.Get<List<object>>("elements");
            foreach (var jsonElement in jsonElements)
            {
                var x = stretchX;
                var y = stretchY;
                if (resetStretch)
                {
                    stretchX = false;
                    stretchY = false;
                }
                elements.Add(ElementFactory.Generate(jsonElement as Dictionary<string, object>, this));
                stretchX = x;
                stretchY = y;
            }
            elements.Reverse();
            areaCache = CalcAreaInternal();
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            RenderChildren(renderer, go);

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        protected virtual GameObject CreateSelf(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            var area = CalcArea();
            rect.sizeDelta = area.Size;
            rect.anchoredPosition = renderer.CalcPosition(area.Min, area.Size);

            SetMaskImage(renderer, go);
            return go;
        }

        protected void SetMaskImage(Renderer renderer, GameObject go)
        {
            var maskSource = elements.Find(x => x is MaskElement);
            if (maskSource == null) return;

            elements.Remove(maskSource);
            var maskImage = go.AddComponent<Image>();
            maskImage.raycastTarget = false;

            var dummyMaskImage = maskSource.Render(renderer);
            dummyMaskImage.transform.SetParent(go.transform);
            dummyMaskImage.GetComponent<Image>().CopyTo(maskImage);
            GameObject.DestroyImmediate(dummyMaskImage);

            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = false;
        }

        protected void RenderChildren(Renderer renderer, GameObject root, Action<GameObject, Element> callback = null)
        {
            foreach (var element in elements)
            {
                var go = element.Render(renderer);
                var rectTransform = go.GetComponent<RectTransform>();
                var sizeDelta = rectTransform.sizeDelta;
                go.transform.SetParent(root.transform, true);
                rectTransform.sizeDelta = sizeDelta;
                rectTransform.localScale = Vector3.one;
                if (callback != null) callback(go, element);
            }
        }

        private Area CalcAreaInternal()
        {
            var area = Area.None();
            foreach (var element in elements) area.Merge(element.CalcArea());
            return area;
        }

        public override Area CalcArea()
        {
            return areaCache;
        }
    }

    public class RootElement : GroupElement
    {
        private Vector2 sizeDelta;

        public RootElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        protected override GameObject CreateSelf(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            sizeDelta = renderer.CanvasSize;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;

            SetMaskImage(renderer, go);

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        public override Area CalcArea()
        {
            return new Area(-sizeDelta / 2.0f, sizeDelta / 2.0f);
        }
    }

    public class ImageElement : Element
    {
        private string spriteName;
        private Vector2 canvasPosition;
        private Vector2 sizeDelta;
        private float opacity;

        public ImageElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
            spriteName = json.Get("image");
            canvasPosition = json.GetVector2("x", "y");
            sizeDelta = json.GetVector2("w", "h");
            opacity = json.GetFloat("opacity");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = renderer.CalcPosition(canvasPosition, sizeDelta);
            rect.sizeDelta = sizeDelta;

            var image = go.AddComponent<Image>();
            image.sprite = renderer.GetSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.color = new Color(1.0f, 1.0f, 1.0f, opacity / 100.0f);

            SetStretch(go, renderer);
            SetPivot(go, renderer);

            return go;
        }

        public override Area CalcArea()
        {
            return Area.FromPositionAndSize(canvasPosition, sizeDelta);
        }
    }

    public sealed class MaskElement : ImageElement
    {
        public MaskElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }
    }

    public sealed class TextElement : Element
    {
        private string message;
        private string font;
        private float fontSize;
        private string align;
        private float virtualHeight;
        private Color fontColor;
        private Vector2 canvasPosition;
        private Vector2 sizeDelta;
        private bool enableStroke;
        private int strokeSize;
        private Color strokeColor;
        private string type;

        public TextElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
            message = json.Get("text");
            font = json.Get("font");
            fontSize = json.GetFloat("size");
            align = json.Get("align");
            type = json.Get("textType");
            if (json.ContainsKey("strokeSize"))
            {
                enableStroke = true;
                strokeSize = json.GetInt("strokeSize");
                strokeColor = EditorUtil.HexToColor(json.Get("strokeColor"));
            }
            fontColor = EditorUtil.HexToColor(json.Get("color"));
            sizeDelta = json.GetVector2("w", "h");
            canvasPosition = json.GetVector2("x", "y");
            virtualHeight = json.GetFloat("vh");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = renderer.CalcPosition(canvasPosition, sizeDelta);
            rect.sizeDelta = sizeDelta;

            var raw = go.AddComponent<RawData>();
            raw.Info["font_size"] = fontSize;
            raw.Info["align"] = align;

            var text = go.AddComponent<Text>();
            text.text = message;
            text.font = renderer.GetFont(font);
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.color = fontColor;

            bool middle = true;
            if (type == "point")
            {
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                middle = true;
            }
            else if (type == "paragraph")
            {
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                middle = !message.Contains("\n");
            }
            else
            {
                Debug.LogError("unknown type " + type);
            }

            var fixedPos = rect.anchoredPosition;
            switch (align)
            {
                case "left":
                    text.alignment =　middle ? TextAnchor.MiddleLeft : TextAnchor.UpperLeft;
                    rect.pivot = new Vector2(0.0f, 0.5f);
                    fixedPos.x -= sizeDelta.x / 2.0f;
                    break;

                case "center":
                    text.alignment =　middle ? TextAnchor.MiddleCenter : TextAnchor.UpperCenter;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;

                case "right":
                    text.alignment =　middle ? TextAnchor.MiddleRight : TextAnchor.UpperRight;
                    rect.pivot = new Vector2(1.0f, 0.5f);
                    fixedPos.x += sizeDelta.x / 2.0f;
                    break;
            }
            rect.anchoredPosition = fixedPos;

            var d = rect.sizeDelta;
            d.y = virtualHeight;
            rect.sizeDelta = d;

            if (enableStroke)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = strokeColor;
                outline.effectDistance = new Vector2(strokeSize / 2.0f, -strokeSize / 2.0f);
                outline.useGraphicAlpha = false;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        public override Area CalcArea()
        {
            return Area.FromPositionAndSize(canvasPosition, sizeDelta);
        }
    }

    public sealed class ButtonElement : GroupElement
    {
        public ButtonElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            Graphic lastImage = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                if (lastImage == null && element is ImageElement) lastImage = g.GetComponent<Image>();
            });

            var button = go.AddComponent<Button>();
            if (lastImage != null)
            {
                button.targetGraphic = lastImage;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class ListElement : GroupElement
    {
        private string scroll;

        public ListElement(Dictionary<string, object> json, Element parent) : base(json, parent, true)
        {
            if (json.ContainsKey("scroll")) scroll = json.Get("scroll");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);
            var content = new GameObject("Content");
            content.AddComponent<RectTransform>();
            content.transform.SetParent(go.transform);

            SetupScroll(go, content);
            SetMaskImage(renderer, go, content);

            var items = CreateItems(renderer, go);
            SetupList(go, items, content);

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        private void SetupScroll(GameObject go, GameObject content)
        {
            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.content = content.GetComponent<RectTransform>();

            var layoutGroup = content.AddComponent<ListLayoutGroup>();
            if (scroll == "vertical")
            {
                scrollRect.vertical = true;
                scrollRect.horizontal = false;
                layoutGroup.Scroll = Scroll.Vertical;
            }
            else if (scroll == "horizontal")
            {
                scrollRect.vertical = false;
                scrollRect.horizontal = true;
                layoutGroup.Scroll = Scroll.Horizontal;
            }
        }

        private void SetMaskImage(Renderer renderer, GameObject go, GameObject content)
        {
            var maskImage = go.AddComponent<Image>();

            var dummyMaskImage = CreateDummyMaskImage(renderer);
            dummyMaskImage.transform.SetParent(go.transform);
            go.GetComponent<RectTransform>().CopyTo(content.GetComponent<RectTransform>());
            content.GetComponent<RectTransform>().localPosition = Vector3.zero;
            dummyMaskImage.GetComponent<Image>().CopyTo(maskImage);
            GameObject.DestroyImmediate(dummyMaskImage);

            maskImage.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            go.AddComponent<RectMask2D>();
        }

        private GameObject CreateDummyMaskImage(Renderer renderer)
        {
            var maskElement = elements.Find(x => (x is ImageElement && x.name.Equals("Area", StringComparison.OrdinalIgnoreCase)));
            if (maskElement == null) throw new Exception(string.Format("{0} Area not found", name));
            elements.Remove(maskElement);

            var maskImage = maskElement.Render(renderer);
            maskImage.SetActive(false);
            return maskImage;
        }

        private List<GameObject> CreateItems(Renderer renderer, GameObject go)
        {
            var items = new List<GameObject>();
            foreach (var element in elements)
            {
                var item = element as GroupElement;
                if (item == null) throw new Exception(string.Format("{0}'s element {1} is not group", name, element.name));

                var itemObject = item.Render(renderer);
                itemObject.transform.SetParent(go.transform);

                var rect = itemObject.GetComponent<RectTransform>();
                var originalPosition = rect.anchoredPosition;
                if (scroll == "vertical")
                {
                    rect.anchorMin = new Vector2(0.5f, 1.0f);
                    rect.anchorMax = new Vector2(0.5f, 1.0f);
                    rect.anchoredPosition = new Vector2(originalPosition.x, -rect.rect.height / 2f);
                }
                else if (scroll == "horizontal")
                {
                    rect.anchorMin = new Vector2(0.0f, 0.5f);
                    rect.anchorMax = new Vector2(0.0f, 0.5f);
                    rect.anchoredPosition = new Vector2(rect.rect.width / 2f, originalPosition.y);
                }

                items.Add(itemObject);
            }
            return items;
        }

        private void SetupList(GameObject go, List<GameObject> itemSources, GameObject content)
        {
            var list = go.AddComponent<List>();
            list.ItemSources = itemSources;
            list.LayoutGroup = content.GetComponent<ListLayoutGroup>();
        }
    }

    public sealed class SliderElement : GroupElement
    {
        public SliderElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            RectTransform fillRect = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                var image = element as ImageElement;
                if (fillRect != null || image == null) return;

                g.GetComponent<Image>().raycastTarget = false;
                if (element.name.Equals("Fill", StringComparison.OrdinalIgnoreCase)) fillRect = g.GetComponent<RectTransform>();
            });

            var slider = go.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            if (fillRect != null)
            {
                fillRect.localScale = Vector2.zero;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = Vector2.zero;
                fillRect.localScale = Vector3.one;
                slider.fillRect = fillRect;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class ScrollbarElement : GroupElement
    {
        public ScrollbarElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            RectTransform handleRect = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                var image = element as ImageElement;
                if (handleRect != null || image == null) return;
                if (element.name.Equals("Handle", StringComparison.OrdinalIgnoreCase)) handleRect = g.GetComponent<RectTransform>();
                g.GetComponent<Image>().raycastTarget = false;
            });

            var scrollbar = go.AddComponent<Scrollbar>();
            var handleImage = handleRect == null ? null : handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                handleRect.anchoredPosition = Vector2.zero;
                handleRect.anchorMin = new Vector2(0.0f, 0.0f);
                handleRect.anchorMax = new Vector2(1.0f, 0.0f);

                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scrollbar.value = 1.0f;
                scrollbar.targetGraphic = handleImage;
                scrollbar.handleRect = handleRect;

                handleRect.sizeDelta = Vector2.zero;
            }

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class ToggleElement : GroupElement
    {
        public ToggleElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            Graphic lastImage = null;
            Graphic checkImage = null;
            RenderChildren(renderer, go, (g, element) =>
            {
                var image = element as ImageElement;
                if (image == null) return;
                if (lastImage == null) lastImage = g.GetComponent<Image>();
                if (element.name.Contains("Check") || element.name.Contains("check")) checkImage = g.GetComponent<Image>();
            });

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = lastImage;
            toggle.graphic = checkImage;

            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }
    }

    public sealed class NullElement : Element
    {
        public NullElement(Dictionary<string, object> json, Element parent) : base(json, parent)
        {
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateUIGameObject(renderer);
            SetStretch(go, renderer);
            SetPivot(go, renderer);
            return go;
        }

        public override Area CalcArea()
        {
            return Area.None();
        }
    }
}
