using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Warner 
	{

	public class LoadingScreen : MonoBehaviour
		{
		#region MEMBER FIELDS

		public Text loadingText;

		public static LoadingScreen instance;

		private CanvasGroup canvasGroup;

		public const float fadeInDuration = 0.5f;
		public const float fadeOutDuration = 0.5f;
		
		#endregion
		


		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			gameObject.SetActive(false);
			canvasGroup = GetComponent<CanvasGroup>();
			}
			
		#endregion


		
		#region OPEN STUFF

		public void show()
			{
			canvasGroup.alpha = 0;
			loadingText.DOFade(0f ,0f);
			gameObject.SetActive(true);

			canvasGroup.DOFade(1f, fadeInDuration);
			loadingText.DOFade(1f, fadeInDuration).SetDelay(fadeInDuration*0.25f);
			}
			
		#endregion



		#region CLOSE STUFF

		public void close()
			{
			loadingText.DOFade(0f, fadeOutDuration*0.1f);
			canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(onFadeOutEnded);
			}


		private void onFadeOutEnded()
			{
			gameObject.SetActive(false);
			}

		#endregion		
		}

	}