using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace Baum2.Editor
{
    public sealed class PrefabCreator
    {
        private static readonly string[] Versions = { "0.5.0" };
        private readonly string spriteRootPath;
        private readonly string fontRootPath;
        private readonly string assetPath;

        public PrefabCreator(string spriteRootPath, string fontRootPath, string assetPath)
        {
            this.spriteRootPath = spriteRootPath;
            this.fontRootPath = fontRootPath;
            this.assetPath = assetPath;
        }

        public GameObject Create()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath).text;
            var json = MiniJSON.Json.Deserialize(text) as Dictionary<string, object>;
            var info = json.GetDic("info");
            Validation(info);

            var canvas = info.GetDic("canvas");
            var imageSize = canvas.GetDic("image");
            var canvasSize = canvas.GetDic("size");
            var baseSize = canvas.GetDic("base");
            var renderer = new Renderer(spriteRootPath, fontRootPath, imageSize.GetVector2("w", "h"), canvasSize.GetVector2("w", "h"), baseSize.GetVector2("x", "y"));
            var rootElement = Element.Generate(json.GetDic("root"));
            var root = rootElement.Render(renderer);
            root.AddComponent<UIRoot>();

            Postprocess(root);

            var cache = root.AddComponent<Cache>();
            cache.CreateCache(root.transform);

            return root;
        }

        private void Postprocess(GameObject go)
        {
            var methods = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.IsSubclassOf(typeof(BaumPostprocessor)))
                .Select(x => x.GetMethod("OnPostprocessPrefab"));
            foreach (var method in methods)
            {
                method.Invoke(null, new object[] { go });
            }
        }

        public void Validation(Dictionary<string, object> info)
        {
            var version = info.Get("version");
            if (!Versions.Contains(version)) throw new Exception(string.Format("version {0} is not supported", version));
        }

        public static GameObject CreateUIGameObject(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            return go;
        }
    }

    public class Renderer
    {
        private readonly string spriteRootPath;
        private readonly string fontRootPath;
        private readonly Vector2 imageSize;
        public Vector2 CanvasSize { get; private set; }
        private readonly Vector2 basePosition;

        public Renderer(string spriteRootPath, string fontRootPath, Vector2 imageSize, Vector2 canvasSize, Vector2 basePosition)
        {
            this.spriteRootPath = spriteRootPath;
            this.fontRootPath = fontRootPath;
            this.imageSize = imageSize;
            CanvasSize = canvasSize;
            this.basePosition = basePosition;
        }

        public Sprite GetSprite(string spriteName)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(spriteRootPath, spriteName) + ".png");
            Assert.IsNotNull(sprite, string.Format("[Baum2] sprite \"{0}\" is not found", spriteName));
            return sprite;
        }

        public Font GetFont(string fontName)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Path.Combine(fontRootPath, fontName) + ".ttf");
            Assert.IsNotNull(font, string.Format("[Baum2] font \"{0}\" is not found", fontName));
            return font;
        }

        public Vector2 CalcPosition(Vector2 position, Vector2 size)
        {
            return CalcPosition(position + size / 2.0f);
        }

        private Vector2 CalcPosition(Vector2 position)
        {
            var tmp = position - basePosition;
            tmp.y *= -1.0f;
            return tmp;
        }

        public Vector2[] GetFourCorners()
        {
            var corners = new Vector2[4];
            corners[0] = CalcPosition(Vector2.zero) + (imageSize - CanvasSize) / 2.0f;
            corners[2] = CalcPosition(imageSize) - (imageSize - CanvasSize) / 2.0f;
            return corners;
        }
    }

    public class Area
    {
        public bool Empty { get; private set; }
        public Vector2 Min { get; private set; }
        public Vector2 Max { get; private set; }
        public Vector2 Avg { get { return (Min + Max) / 2.0f; } }
        public float Width { get { return Mathf.Abs(Max.x - Min.x); } }
        public float Height { get { return Mathf.Abs(Max.y - Min.y); } }
        public Vector2 Size { get { return new Vector2(Width, Height); } }

        public Area()
        {
            Empty = true;
        }

        public Area(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
            Empty = false;
        }

        public static Area FromPositionAndSize(Vector2 position, Vector2 size)
        {
            return new Area(position, position + size);
        }

        public static Area None()
        {
            return new Area();
        }

        public void Merge(Area other)
        {
            if (other.Empty) return;
            if (Empty)
            {
                Min = other.Min;
                Max = other.Max;
                Empty = false;
                return;
            }

            if (other.Min.x < Min.x) Min = new Vector2(other.Min.x, Min.y);
            if (other.Min.y < Min.y) Min = new Vector2(Min.x, other.Min.y);
            if (other.Max.x > Max.x) Max = new Vector2(other.Max.x, Max.y);
            if (other.Max.y > Max.y) Max = new Vector2(Max.x, other.Max.y);
        }
    }

    public static class JsonExtensions
    {
        public static string Get(this Dictionary<string, object> json, string key)
        {
            return json[key] as string;
        }

        public static float GetFloat(this Dictionary<string, object> json, string key)
        {
            return (float)json[key];
        }

        public static int GetInt(this Dictionary<string, object> json, string key)
        {
            return (int)(float)json[key];
        }

        public static T Get<T>(this Dictionary<string, object> json, string key) where T : class
        {
            return json[key] as T;
        }

        public static Dictionary<string, object> GetDic(this Dictionary<string, object> json, string key)
        {
            return json[key] as Dictionary<string, object>;
        }

        public static Vector2 GetVector2(this Dictionary<string, object> json, string keyX, string keyY)
        {
            return new Vector2((float)json[keyX], (float)json[keyY]);
        }
    }
}