using UnityEngine;
using UnityEngine.UI;

namespace Baum2.Sample
{
    public class Sample : MonoBehaviour
    {
        [SerializeField]
        private GameObject uiPrefab;

        private UIRoot ui;
        private int listSize;

        public void Start()
        {
            ui = BaumUI.Instantiate(gameObject, uiPrefab);
            listSize = 10;
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
                list.Resize(++listSize);
            });

            ui.Get<Button>("FugaButton").onClick.AddListener(() =>
            {
                welcomeText.text = "Welcome to Fuga!";
                ui.Get("Image1").SetActive(false);
                ui.Get("Image2").SetActive(true);
                list.Resize(--listSize);
            });
        }

        public void ListSample()
        {
            var list = ui.Get<List>("PiyoList");
            list.Scrollbar = ui.Get<Scrollbar>("PiyoScrollbar");
            list.Spacing = 10;
            list.UISelector = index => index % 2 == 0 ? "Item1" : "Item2";
            list.UIFactory = (listUI, index) =>
            {
                listUI.gameObject.name = "ListItem" + index;
                listUI.Get<Text>("ListItemText").text = string.Format("Piyo: {0}", index);

                var button = listUI.Get<Button>("ItemButton", true);
                if (button != null)
                {
                    button.onClick.AddListener(() => Debug.Log(index));
                }
            };
            list.Resize(listSize);
        }

        public void SliderSample()
        {
            ui.Get<Slider>("HPSlider").value = Mathf.Clamp01(Time.time % 1.0f);
        }
    }
}