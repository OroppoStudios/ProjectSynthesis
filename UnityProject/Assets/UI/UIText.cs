using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Warner;

namespace Warner
	{
	public class UIText : MonoBehaviour
		{
		#region MEMBER FIELDS

		public string text;
		public bool blinking;
		public float startBlinkingDelay = 0f;

		private Text textObject;

		private const float fadeOutDuration = 0.5f;
		private const float fadeInDuration = 0.8f;
		private const float restarBlinkingTime = 0.5f;

		#endregion


		
		#region INIT STUFF
		
		private void Awake()
			{
			textObject = GetComponent<Text>();

			if (blinking)
				textObject.DOFade(0f, 0f);
			}


		private void OnEnable()
			{
			cancelBlinking();
			Invoke("fadeIn", startBlinkingDelay);
			}

		private void Start()
			{
			Languages.instance.onLanguageSwitched += onLanguageSwitched;
			textObject.text = Languages.instance.getText(text);
			}
			
		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{
			cancelBlinking();
			}

		private void OnDestroy()
			{
			Languages.instance.onLanguageSwitched -= onLanguageSwitched;
			}

		#endregion



		#region EVENT HANDLERS STUFF

		private void onLanguageSwitched()
			{
			textObject.text = Languages.instance.getText(text);

			if (!blinking)
				return;

			cancelBlinking();

			Invoke("startFadeIn", startBlinkingDelay);

			Invoke("fadeOut",restarBlinkingTime+startBlinkingDelay);
			}


		private void startFadeIn()
			{
			textObject.DOFade(1,fadeInDuration);
			}

		#endregion
		
		
		
		#region BLINKING STUFF

		private void cancelBlinking()
			{
			CancelInvoke("fadeOut");
			CancelInvoke("fadeIn");

			DOTween.Kill(name+"TextBlinking");

			if (blinking)
				textObject.DOFade(0f, 0f);
			}


		private void fadeIn()
			{
			if (!blinking)
				return;

			textObject.DOFade(1,fadeInDuration).SetEase(Ease.InSine).SetId(name+"TextBlinking").OnComplete(fadeOut);
			}

		private void fadeOut()
			{
			if (!blinking)
				return;

			textObject.DOFade(0,fadeOutDuration).SetDelay(0.2f).SetId(name+"TextBlinking").OnComplete(fadeIn);
			}
			
		#endregion
		}

	}