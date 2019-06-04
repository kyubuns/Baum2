using UnityEngine;
using UnityEngine.UI;

namespace Baum2.Sample
{
    public class Sample : MonoBehaviour
    {
        [SerializeField]
        private GameObject uiPrefab = null;

        private UIRoot ui;
        private System.Collections.Generic.List<string> listElements = new System.Collections.Generic.List<string>();

        public void Start()
        {
            ui = BaumUI.Instantiate(gameObject, uiPrefab);
            for (var i = 0; i < 1000; ++i)
            {
                listElements.Add(Random.Range(0, 2) + "_" + i);
            }
            ImageSample();
            ButtonSample();
            ListSample();
        }

        public void Update()
        {
            SliderSample();
        }

        public void ImageSample()
        {
            ui.Get("Image1").SetActive(false);
            ui.Get("Image2").SetActive(false);
        }

        public void ButtonSample()
        {
            var welcomeText = ui.Get<Text>("Welcome/Text");
            var list = ui.Get<List>("PiyoList");

            ui.Get<Button>("HogeButton").onClick.AddListener(() =>
            {
                welcomeText.text = "Welcome to Hoge!";
                ui.Get("Image1").SetActive(true);
                ui.Get("Image2").SetActive(false);
                listElements.Add("1_" + Random.Range(0, 1000));
                list.Resize(listElements.Count);
            });

            ui.Get<Button>("FugaButton").onClick.AddListener(() =>
            {
                welcomeText.text = "Welcome to Fuga!";
                ui.Get("Image1").SetActive(false);
                ui.Get("Image2").SetActive(true);
                listElements.Add("2_" + Random.Range(0, 1000));
                list.Resize(listElements.Count);
            });
        }

        public void ListSample()
        {
            var list = ui.Get<List>("PiyoList");
            list.Scrollbar = ui.Get<Scrollbar>("PiyoScrollbar");
            list.LayoutGroup.Spacing = 10;
            list.UISelector = (index) => listElements[index].Split('_')[0] == "1" ? "Item1" : "Item2";
            list.UIUpdater = (listUI, index) =>
            {
                listUI.Get<Text>("ListItemText").text = string.Format("{0}", listElements[index].Split('_')[1]);

                var button = listUI.Get<Button>("ItemButton", true);
                if (button != null)
                {
                    button.onClick.AddListener(() => Debug.Log(index));
                }
            };
            list.Resize(listElements.Count);
        }

        public void SliderSample()
        {
            ui.Get<Slider>("HPSlider").value = Mathf.Clamp01(Time.time % 1.0f);
        }
    }
}
