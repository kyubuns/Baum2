using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Baum2.Editor
{
    public abstract class Element
    {
        public static readonly Dictionary<string, Func<Dictionary<string, object>, Element>> Generator = new Dictionary<string, Func<Dictionary<string, object>, Element>>()
        {
            { "Root", (d) => new RootElement(d)},
            { "Image", (d) => new ImageElement(d)},
            { "Mask", (d) => new MaskElement(d)},
            { "Group", (d) => new GroupElement(d)},
            { "Text", (d) => new TextElement(d)},
            { "Button", (d) => new ButtonElement(d)},
            { "List", (d) => new ListElement(d)},
            { "Slider", (d) => new SliderElement(d)},
            { "Scrollbar", (d) => new ScrollbarElement(d)},
        };

        public string Name;

        public static Element Generate(Dictionary<string, object> json)
        {
            var type = json.Get("type");
            Assert.IsTrue(Generator.ContainsKey(type), "[Baum2] Unknown type: " + type);
            return Generator[type](json);
        }

        public abstract GameObject Render(Renderer renderer);
        public abstract Area CalcArea();
    }

    public class GroupElement : Element
    {
        protected string pivot;
        protected readonly List<Element> elements;

        public GroupElement(Dictionary<string, object> json)
        {
            Name = json.Get("name");
            if (json.ContainsKey("pivot")) pivot = json.Get("pivot");

            elements = new List<Element>();
            var jsonElements = json.Get<List<object>>("elements");
            foreach (var jsonElement in jsonElements)
            {
                elements.Add(Generate(jsonElement as Dictionary<string, object>));
            }
            elements.Reverse();
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = CreateSelf(renderer);

            RenderChildren(renderer, go);

            return go;
        }

        protected virtual GameObject CreateSelf(Renderer renderer)
        {
            var go = PrefabCreator.CreateUIGameObject(Name);

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
                var size = go.GetComponent<RectTransform>().sizeDelta;
                go.transform.SetParent(root.transform, true);
                go.GetComponent<RectTransform>().sizeDelta = size;
                if (element is GroupElement) ((GroupElement)element).SetPivot(go, renderer);
                if (callback != null) callback(go, element);
            }
        }

        protected void SetPivot(GameObject root, Renderer renderer)
        {
            if (string.IsNullOrEmpty(pivot)) pivot = "none";

            var rect = root.GetComponent<RectTransform>();
            var pivotPos = new Vector2(0.5f, 0.5f);

            var originalPosition = root.GetComponent<RectTransform>().anchoredPosition;
            Vector2 canvasSize = renderer.CanvasSize;

            if (pivot.Contains("Bottom") || pivot.Contains("bottom"))
            {
                pivotPos.y = 0.0f;
                originalPosition.y = originalPosition.y + canvasSize.y / 2.0f;
            }
            else if (pivot.Contains("Top") || pivot.Contains("top"))
            {
                pivotPos.y = 1.0f;
                originalPosition.y = originalPosition.y - canvasSize.y / 2.0f;
            }
            if (pivot.Contains("Left") || pivot.Contains("left"))
            {
                pivotPos.x = 0.0f;
                originalPosition.x = originalPosition.x + canvasSize.x / 2.0f;
            }
            else if (pivot.Contains("Right") || pivot.Contains("right"))
            {
                pivotPos.x = 1.0f;
                originalPosition.x = originalPosition.x - canvasSize.x / 2.0f;
            }

            rect.anchorMin = pivotPos;
            rect.anchorMax = pivotPos;
            rect.anchoredPosition = originalPosition;
        }

        public override Area CalcArea()
        {
            var area = Area.None();
            foreach (var element in elements) area.Merge(element.CalcArea());
            return area;
        }
    }

    public class RootElement : GroupElement
    {
        public RootElement(Dictionary<string, object> json) : base(json)
        {
        }

        protected override GameObject CreateSelf(Renderer renderer)
        {
            var go = PrefabCreator.CreateUIGameObject(Name);

            var rect = go.GetComponent<RectTransform>();
            var area = CalcArea();
            rect.sizeDelta = area.Size;
            rect.anchoredPosition = Vector2.zero;

            SetMaskImage(renderer, go);
            return go;
        }
    }

    public class ImageElement : Element
    {
        private string spriteName;
        private Vector2 canvasPosition;
        private Vector2 sizeDelta;
        private float opacity;
        private bool background;

        public ImageElement(Dictionary<string, object> json)
        {
            Name = json.Get("name");
            spriteName = json.Get("image");
            canvasPosition = json.GetVector2("x", "y");
            sizeDelta = json.GetVector2("w", "h");
            opacity = json.GetFloat("opacity");
            if (json.ContainsKey("background")) background = (bool)json["background"];
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = PrefabCreator.CreateUIGameObject(Name);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = renderer.CalcPosition(canvasPosition, sizeDelta);
            rect.sizeDelta = sizeDelta;

            var image = go.AddComponent<Image>();
            image.sprite = renderer.GetSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.color = new Color(1.0f, 1.0f, 1.0f, opacity / 100.0f);

            if (background)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }

            return go;
        }

        public override Area CalcArea()
        {
            return Area.FromPositionAndSize(canvasPosition, sizeDelta);
        }
    }

    public sealed class MaskElement : ImageElement
    {
        public MaskElement(Dictionary<string, object> json) : base(json)
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

        public TextElement(Dictionary<string, object> json)
        {
            Name = json.Get("name");
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
            var go = PrefabCreator.CreateUIGameObject(Name);

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

            return go;
        }

        public override Area CalcArea()
        {
            return Area.FromPositionAndSize(canvasPosition, sizeDelta);
        }
    }

    public sealed class ButtonElement : GroupElement
    {
        public ButtonElement(Dictionary<string, object> json) : base(json)
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

            return go;
        }
    }

    public sealed class ListElement : GroupElement
    {
        private string scroll;

        public ListElement(Dictionary<string, object> json) : base(json)
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
            SetupList(go, items);

            return go;
        }

        private void SetupScroll(GameObject go, GameObject content)
        {
            var scrollRect = go.AddComponent<ScrollRect>();
            scrollRect.content = content.GetComponent<RectTransform>();

            if (scroll == "Vertical")
            {
                var layoutGroup = content.AddComponent<VerticalLayoutGroup>();
                scrollRect.vertical = true;
                scrollRect.horizontal = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;
            }
            else if (scroll == "Horizontal")
            {
                var layoutGroup = content.AddComponent<HorizontalLayoutGroup>();
                scrollRect.vertical = false;
                scrollRect.horizontal = true;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = true;
            }
        }

        private void SetMaskImage(Renderer renderer, GameObject go, GameObject content)
        {
            var maskImage = go.AddComponent<Image>();

            var dummyMaskImage = CreateDummyMaskImage(renderer);
            dummyMaskImage.transform.SetParent(go.transform);
            dummyMaskImage.GetComponent<RectTransform>().CopyTo(go.GetComponent<RectTransform>());
            dummyMaskImage.GetComponent<RectTransform>().CopyTo(content.GetComponent<RectTransform>());
            dummyMaskImage.GetComponent<Image>().CopyTo(maskImage);
            GameObject.DestroyImmediate(dummyMaskImage);

            maskImage.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            go.AddComponent<RectMask2D>();
        }

        private GameObject CreateDummyMaskImage(Renderer renderer)
        {
            var maskElement = elements.Find(x => (x is ImageElement && x.Name == "Area"));
            if (maskElement == null) throw new Exception(string.Format("{0} Area not found", Name));
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
                if (item == null) throw new Exception(string.Format("{0}'s element {1} is not group", Name, element.Name));

                var itemObject = item.Render(renderer);
                itemObject.transform.SetParent(go.transform);

                var globalPosition = itemObject.transform.position;
                var rect = itemObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.0f, 1.0f);
                rect.anchorMax = new Vector2(0.0f, 1.0f);
                itemObject.transform.position = globalPosition;

                var layout = itemObject.AddComponent<LayoutElement>();
                layout.minHeight = item.CalcArea().Height;
                layout.minWidth = item.CalcArea().Width;
                if (scroll == "Vertical")
                {
                    rect.sizeDelta = new Vector2(rect.anchoredPosition.x * 2.0f, rect.sizeDelta.y);
                    layout.minHeight = rect.anchoredPosition.x * 2.0f;
                }
                if (scroll == "Horizontal")
                {
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.anchoredPosition.y * 2.0f);
                    layout.minWidth = rect.anchoredPosition.y * 2.0f;
                }

                itemObject.SetActive(false);

                items.Add(itemObject);
            }
            return items;
        }

        private void SetupList(GameObject go, List<GameObject> itemSources)
        {
            var list = go.AddComponent<List>();
            list.ItemSources = itemSources;
        }
    }

    public sealed class SliderElement : GroupElement
    {
        public SliderElement(Dictionary<string, object> json) : base(json)
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
                if (element.Name == "Fill") fillRect = g.GetComponent<RectTransform>();
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

            return go;
        }
    }

    public sealed class ScrollbarElement : GroupElement
    {
        public ScrollbarElement(Dictionary<string, object> json) : base(json)
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
                if (element.Name == "Handle") handleRect = g.GetComponent<RectTransform>();
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

            return go;
        }
    }

    public sealed class NullElement : Element
    {
        public NullElement(Dictionary<string, object> json)
        {
            Name = json.Get("name");
        }

        public override GameObject Render(Renderer renderer)
        {
            var go = PrefabCreator.CreateUIGameObject(Name);
            return go;
        }

        public override Area CalcArea()
        {
            return Area.None();
        }
    }
}
