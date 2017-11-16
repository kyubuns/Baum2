using UnityEngine;
using UnityEngine.UI;
using Baum2.Editor;

namespace Baum2.Sample
{
    public class Postprocessor : BaumPostprocessor
    {
        public static void OnPostprocessPrefab(GameObject go)
        {
            Debug.Log("Sample Postprocess");

            foreach (var button in go.GetComponentsInChildren<Button>())
            {
                var nav = new Navigation {mode = Navigation.Mode.None};
                button.navigation = nav;
            }
        }
    }
}