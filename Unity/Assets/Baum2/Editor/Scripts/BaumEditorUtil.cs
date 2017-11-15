using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;

namespace Baum2.Editor
{
    public static class EditorUtil
    {
        public static readonly string ImportDirectoryPath = "/Baum2/Import/";

        public static string ToUnityPath(string path)
        {
            path = path.Substring(path.IndexOf("Assets", System.StringComparison.Ordinal));
            if (path.EndsWith("/", System.StringComparison.Ordinal) || path.EndsWith("\\", System.StringComparison.Ordinal)) path = path.Substring(0, path.Length - 1);
            return path.Replace("\\", "/");
        }

        public static string GetBaumSpritesPath()
        {
            return GetPath("BaumSprites");
        }

        public static string GetBaumPrefabsPath()
        {
            return GetPath("BaumPrefabs");
        }

        public static string GetBaumFontsPath()
        {
            return GetPath("BaumFonts");
        }

        public static string GetPath(string fileName)
        {
            var files = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories);
            if (files.Length > 1)
            {
                files = files.Where(x => !x.Contains("Baum2/Sample")).ToArray();
            }
            if (files.Length > 1)
            {
                Debug.LogErrorFormat("{0}ファイルがプロジェクト内に複数個存在します。", fileName);
            }
            if (files.Length == 0)
            {
                throw new System.ApplicationException(string.Format("{0}ファイルがプロジェクト内に存在しません。", fileName));
            }
            string path = files[0];
            return path.Substring(0, path.Length - fileName.Length);
        }

        public static Color HexToColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        public static RectTransform CopyTo(this RectTransform self, RectTransform to)
        {
            to.sizeDelta = self.sizeDelta;
            to.position = self.position;
            return self;
        }

        public static Image CopyTo(this Image self, Image to)
        {
            to.sprite = self.sprite;
            to.type = self.type;
            to.color = self.color;
            return self;
        }
    }
}