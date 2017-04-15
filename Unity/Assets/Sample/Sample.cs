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

			UI.Get("Image1").SetActive(false);
			UI.Get("Image2").SetActive(false);

			var welcomeText = UI.Get<Text>("Welcome/Text");
			UI.Get<Button>("HogeButton").onClick.AddListener(() =>
			{
				welcomeText.text = "Welcome to Hoge!";
				UI.Get("Image1").SetActive(true);
				UI.Get("Image2").SetActive(false);
			});

			UI.Get<Button>("FugaButton").onClick.AddListener(() =>
			{
				welcomeText.text = "Welcome to Fuga!";
				UI.Get("Image1").SetActive(false);
				UI.Get("Image2").SetActive(true);
			});

			var list = UI.Get<List>("PiyoList");
			list.Spacing = 10;
			list.Init(10, (UIRoot ui, int index) =>
			{
				ui.Get<Text>("ListItemText").text = string.Format("Piyo: {0}", index);
			});
		}

		public void Update()
		{
			UI.Get<Slider>("HPSlider").value = Mathf.Clamp01(Time.time % 1.0f);
		}
	}
}