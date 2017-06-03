using UnityEngine;
using UnityEngine.UI;
using Baum2;

namespace Baum2.Sample
{
	public class Sample : MonoBehaviour
	{
		[SerializeField]
		private GameObject UIPrefab;
		private UIRoot UI;

		public void Start()
		{
			UI = BaumUI.Instantiate(gameObject, UIPrefab);
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
			UI.Get("Image1").SetActive(false);
			UI.Get("Image2").SetActive(false);
		}

		public void ButtonSample()
		{
			var welcomeText = UI.Get<Text>("Welcome/Text");
			var list = UI.Get<List>("PiyoList");

			UI.Get<Button>("HogeButton").onClick.AddListener(() =>
			{
				welcomeText.text = "Welcome to Hoge!";
				UI.Get("Image1").SetActive(true);
				UI.Get("Image2").SetActive(false);
				list.Add();
			});

			UI.Get<Button>("FugaButton").onClick.AddListener(() =>
			{
				welcomeText.text = "Welcome to Fuga!";
				UI.Get("Image1").SetActive(false);
				UI.Get("Image2").SetActive(true);
				list.Add();
			});
		}

		public void ListSample()
		{
			var list = UI.Get<List>("PiyoList");
			list.Spacing = 10;
			list.Init(10,
				(int index) =>
				{
					return index % 2 == 0 ? "Item1" : "Item2";
				},
				(UIRoot ui, int index) =>
				{
					ui.Get<Text>("ListItemText").text = string.Format("Piyo: {0}", index);
				}
			);
		}

		public void SliderSample()
		{
			UI.Get<Slider>("HPSlider").value = Mathf.Clamp01(Time.time % 1.0f);
		}
	}
}