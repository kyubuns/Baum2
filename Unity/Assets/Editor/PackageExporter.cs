using System.IO;
using UnityEngine;
using UnityEditor;

public static class PackageExporter
{
    [MenuItem("Export/Export Unity Package")]
    public static void ExportUnityPackage()
    {
        var libraryDirectories = new[] {
            "Assets/Baum2"
        };

        var outputFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "../Baum2.unitypackage");

        Debug.LogFormat("Export yokan package to {0}", outputFilePath);

        AssetDatabase.ExportPackage(libraryDirectories, outputFilePath, ExportPackageOptions.Recurse);

        Debug.LogFormat("Success");
    }
}