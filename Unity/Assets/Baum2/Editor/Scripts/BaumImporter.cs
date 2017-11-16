using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using OnionRing;

namespace Baum2.Editor
{
    public sealed class Importer : AssetPostprocessor
    {
        public override int GetPostprocessOrder() { return 1000; }

        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var changed = false;

            // Create Directory
            foreach (var asset in importedAssets)
            {
                if (!asset.Contains(EditorUtil.ImportDirectoryPath)) continue;
                if (!string.IsNullOrEmpty(Path.GetExtension(asset))) continue;
                CreateSpritesDirectory(asset);
                changed = true;
            }

            // Slice Sprite
            foreach (var asset in importedAssets)
            {
                if (!asset.Contains(EditorUtil.ImportDirectoryPath)) continue;
                if (!asset.EndsWith(".png", System.StringComparison.Ordinal)) continue;
                SliceSprite(asset);
                changed = true;
            }

            if (changed) AssetDatabase.Refresh();

            EditorApplication.delayCall += () =>
            {
                // Delete Directory
                foreach (var asset in importedAssets)
                {
                    if (!asset.Contains(EditorUtil.ImportDirectoryPath)) continue;
                    if (!string.IsNullOrEmpty(Path.GetExtension(asset))) continue;
                    Debug.LogFormat("[Baum2] Delete Directory: {0}", EditorUtil.ToUnityPath(asset));
                    AssetDatabase.DeleteAsset(EditorUtil.ToUnityPath(asset));
                    changed = true;
                }

                // Create Prefab
                foreach (var asset in importedAssets)
                {
                    if (!asset.Contains(EditorUtil.ImportDirectoryPath)) continue;
                    if (!asset.EndsWith(".layout.txt", System.StringComparison.Ordinal)) continue;

                    var name = Path.GetFileName(asset).Replace(".layout.txt", "");
                    var spriteRootPath = EditorUtil.ToUnityPath(Path.Combine(EditorUtil.GetBaumSpritesPath(), name));
                    var fontRootPath = EditorUtil.ToUnityPath(EditorUtil.GetBaumFontsPath());
                    var creator = new PrefabCreator(spriteRootPath, fontRootPath, asset);
                    var go = creator.Create();
                    var savePath = EditorUtil.ToUnityPath(Path.Combine(EditorUtil.GetBaumPrefabsPath(), name + ".prefab"));
                    Object originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
                    if (originalPrefab == null) originalPrefab = PrefabUtility.CreateEmptyPrefab(savePath);
                    PrefabUtility.ReplacePrefab(go, originalPrefab, ReplacePrefabOptions.ReplaceNameBased);
                    GameObject.DestroyImmediate(go);
                    Debug.LogFormat("[Baum2] Create Prefab: {0}", savePath);

                    AssetDatabase.DeleteAsset(EditorUtil.ToUnityPath(asset));
                }
            };
        }

        private static void CreateSpritesDirectory(string asset)
        {
            var directoryName = Path.GetFileName(Path.GetFileName(asset));
            var directoryPath = EditorUtil.GetBaumSpritesPath();
            var directoryFullPath = Path.Combine(directoryPath, directoryName);
            if (Directory.Exists(directoryFullPath))
            {
                // Debug.LogFormat("[Baum2] Delete Exist Sprites: {0}", EditorUtil.ToUnityPath(directoryFullPath));
                foreach (var filePath in Directory.GetFiles(directoryFullPath, "*.png", SearchOption.TopDirectoryOnly)) File.Delete(filePath);
            }
            else
            {
                // Debug.LogFormat("[Baum2] Create Directory: {0}", EditorUtil.ToUnityPath(directoryPath) + "/" + directoryName);
                AssetDatabase.CreateFolder(EditorUtil.ToUnityPath(directoryPath), Path.GetFileName(directoryFullPath));
            }
        }

        private static void SliceSprite(string asset)
        {
            var directoryName = Path.GetFileName(Path.GetDirectoryName(asset));
            var directoryPath = Path.Combine(EditorUtil.GetBaumSpritesPath(), directoryName);
            var fileName = Path.GetFileName(asset);
            var newPath = Path.Combine(directoryPath, fileName);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            var slicedTexture = TextureSlicer.Slice(texture);
            if (PreprocessTexture.SlicedTextures == null) PreprocessTexture.SlicedTextures = new Dictionary<string, SlicedTexture>();
            PreprocessTexture.SlicedTextures[fileName] = slicedTexture;
            byte[] pngData = slicedTexture.Texture.EncodeToPNG();
            File.WriteAllBytes(newPath, pngData);
            Object.DestroyImmediate(slicedTexture.Texture);

            // Debug.LogFormat("[Baum2] Slice: {0} -> {1}", EditorUtil.ToUnityPath(asset), EditorUtil.ToUnityPath(newPath));
        }
    }
}