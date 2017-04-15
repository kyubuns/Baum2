using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace Baum2.Editor
{
	public static class EditorUtil
	{
		public static readonly string ImportDirectoryPath = "/Baum2/Import/";

		public static string ToUnityPath(string path)
		{
			path = path.Substring(path.IndexOf("Assets/", System.StringComparison.Ordinal));
			if (path.EndsWith("/", System.StringComparison.Ordinal)) return path.Substring(0, path.Length-1);
			return path;
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
			string path = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories)[0];
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