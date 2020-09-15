using UnityEngine;
using Baum2.Editor;
using AnKuchen.Map;

namespace Baum2.Sample
{
    public class Postprocessor : BaumPostprocessor
    {
        public static void OnPostprocessPrefab(GameObject go)
        {
            Debug.Log("Sample Postprocess");

            var uiCache = go.AddComponent<UICache>();
            uiCache.CreateCache();
        }
    }
}
