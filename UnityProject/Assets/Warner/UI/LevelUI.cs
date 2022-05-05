using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Warner
	{
	public class LevelUI: MonoBehaviour
		{
		#region MEMBER FIELDS

		public Image healthBar;
		public ComboData combo;

		[Serializable]
		public struct ComboData
			{
			public Text count;	
			public Image text;
			public Text phrases;
			public ComboPhrase[] comboPhrases;
			[NonSerializedAttribute] public CanvasGroup countCanvasGroup;
			[NonSerializedAttribute] public CanvasGroup textCanvasGroup;
			[NonSerializedAttribute] public CanvasGroup phrasesCanvasGroup;
			[NonSerializedAttribute] public Vector3 phrasesOriginalPosition;
			[NonSerializedAttribute] public Vector3 phrasesOriginalScale;
			[NonSerializedAttribute] public Vector3 countOriginalScale;
			[NonSerializedAttribute] public Vector3 textOriginalScale;
			}

		[Serializable]
		public struct ComboPhrase
			{
			public string textKey;
			public Vector2 comboRange;
			}

		public static LevelUI instance;

		private CanvasGroup canvasGroup;

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			canvasGroup = GetComponent<CanvasGroup>();
			canvasGroup.alpha = 0;
			initComboContainers();
			}



		private void Start()
			{
			gameObject.SetActive(false);
			LevelMaster.instance.onLevelReady += show;
			LevelMaster.instance.onLevelClosed += close;
			}

		#endregion



		#region DESTROY

		public void OnDestroy()
			{
			LevelMaster.instance.onLevelReady -= show;
			LevelMaster.instance.onLevelClosed -= close;
			}

		#endregion



		#region OPEN STUFF

		public void show()
			{
			gameObject.SetActive(true);
			canvasGroup.DOFade(1, GameMaster.menusFadeTransitionDuration);
			}

		#endregion



		#region CLOSE STUFF


		public void close()
			{
			canvasGroup.DOFade(0,GameMaster.menusFadeTransitionDuration).OnComplete(onFadeOutEnded);
			}

		private void onFadeOutEnded()
			{
			gameObject.SetActive(false);
			}

		#endregion




		#region HEALTHBAR

		public void updateHealth(int health)
			{
			healthBar.fillAmount = health/100f;
			}

		#endregion



		#region COMBO

		private void initComboContainers()
			{
			combo.countOriginalScale = combo.count.rectTransform.localScale;
			combo.textOriginalScale = combo.text.rectTransform.localScale;

			combo.countCanvasGroup = combo.count.gameObject.AddComponent<CanvasGroup>();
			combo.countCanvasGroup.alpha = 0;
			combo.countCanvasGroup.interactable = false;
			combo.countCanvasGroup.blocksRaycasts = false;
			combo.countCanvasGroup.DOFade(0f, 0f);

			combo.textCanvasGroup = combo.text.gameObject.AddComponent<CanvasGroup>();
			combo.textCanvasGroup.alpha = 0;
			combo.textCanvasGroup.interactable = false;
			combo.textCanvasGroup.blocksRaycasts = false;
			combo.textCanvasGroup.DOFade(0f, 0f);

			combo.phrasesCanvasGroup = combo.phrases.gameObject.AddComponent<CanvasGroup>();
			combo.phrasesCanvasGroup.alpha = 0;
			combo.phrasesCanvasGroup.interactable = false;
			combo.phrasesCanvasGroup.blocksRaycasts = false;
			combo.phrasesOriginalPosition = combo.phrases.rectTransform.localPosition;
			combo.phrasesOriginalScale = combo.phrases.rectTransform.localScale;
			combo.phrasesCanvasGroup.DOFade(0f, 0f);
			}

		public void updateCombo(int count)
			{
			DOTween.Kill("ComboCountTextIn");
			DOTween.Kill("ComboTextIn");
			DOTween.Kill("ComboCountTextOut");
			DOTween.Kill("ComboTextOut");
			DOTween.Kill("ComboPhrase");
			DOTween.Kill("ComboPhraseOut");
			DOTween.Kill("ComboPhraseScale");

			const float inSpeed = 0.08f;
			const float inSizeScalar = 1.5f;
			const float staticTextInScalar = 0.4f;
			const float holdTime = 1.5f;
			const float outSpeed = 0.15f;
			const float phraseOffset = 0.4f;
			const float phraseHoldTime = 0.75f;
			const float phraseInSpeed = 0.15f;
			const float phraseOutSpeed = 0.15f;

			combo.count.text = count.ToString();

			combo.count.DOFade(1, 0f);
			combo.count.rectTransform.DOScale(combo.countOriginalScale*inSizeScalar, 0);
			combo.count.rectTransform.DOScale(combo.countOriginalScale, inSpeed).SetId("ComboCountTextIn");

			combo.countCanvasGroup.DOFade(1, 0f);
			combo.textCanvasGroup.DOFade(1, inSpeed).SetDelay(inSpeed*staticTextInScalar);

			if (count==2)
				{
				combo.text.rectTransform.DOScale(combo.textOriginalScale*inSizeScalar, 0);
				combo.text.rectTransform.DOScale(combo.textOriginalScale, inSpeed).SetDelay(inSpeed*staticTextInScalar).SetId("ComboTextIn");
				}										

			combo.countCanvasGroup.DOFade(0f, outSpeed).SetId("ComboCountTextOut").SetDelay(holdTime);
			combo.textCanvasGroup.DOFade(0f, outSpeed).SetId("ComboTextOut").SetDelay(holdTime);

			combo.phrases.rectTransform.DOLocalMove(combo.phrasesOriginalPosition, 0f);
			combo.phrases.rectTransform.DOScale(combo.phrasesOriginalScale, 0f);


			combo.phrases.rectTransform.DOScale(combo.phrasesOriginalScale*0.5f, phraseOutSpeed)
				.SetId("ComboPhraseScale");
			combo.phrasesCanvasGroup.DOFade(0f, phraseOutSpeed).SetId("ComboPhrase").OnComplete(()=>
				{
				string key = getPhraseKey(count);
				if (string.IsNullOrEmpty(key))
					return;

				combo.phrases.text = Languages.instance.getText(key)+"!";

				combo.phrases.rectTransform.DOScale(combo.phrasesOriginalScale, phraseOutSpeed).SetId("ComboPhraseScale");
				combo.phrasesCanvasGroup.DOFade(1f, phraseInSpeed).SetId("ComboPhrase").SetDelay(phraseOffset).OnComplete(()=>
					{
					const float fadeOutDelay = phraseOutSpeed+phraseInSpeed+phraseHoldTime;

					combo.phrases.rectTransform.DOLocalMove(combo.phrasesOriginalPosition-new Vector3(20f, -1f, 0f),
						phraseOutSpeed).SetId("ComboPhrase").SetDelay(fadeOutDelay);
					combo.phrasesCanvasGroup.DOFade(0f, phraseOutSpeed*0.8f).SetId("ComboPhraseOut").SetDelay(fadeOutDelay);
					combo.phrases.rectTransform.DOScale(combo.phrasesOriginalScale*0.5f, phraseOutSpeed).SetId("ComboPhraseScale").SetDelay(fadeOutDelay);
					});	
				});			
			}

		private string getPhraseKey(int comboCount)
			{
			for (int i = 0; i<combo.comboPhrases.Length; i++)
				if (comboCount>=combo.comboPhrases[i].comboRange.x
				    && comboCount<=combo.comboPhrases[i].comboRange.y)
					return combo.comboPhrases[i].textKey;

			return string.Empty;
			}

		#endregion
		}
	}
