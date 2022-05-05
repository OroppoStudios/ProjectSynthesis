using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Warner;

namespace Warner
	{
	public class LogoScreen: MonoBehaviour
		{
		#region MEMBER FIELDS

		[NonSerialized] Camera cam;
		[NonSerialized] CanvasGroup canvasGroup;

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			cam = transform.Find("Camera").GetComponent<Camera>();
			canvasGroup = GetComponent<CanvasGroup>();
			}


		private void Start()
			{
			Invoke("fadeIn", 0.5f);
			}

		private void fadeIn()
			{
			canvasGroup.DOFade(1f, 1.5f).OnComplete(loadGame);
			}

		#endregion



		#region LOAD GAME


		private void loadGame()
			{
			Timing.run(loadGameCoRoutine());
			}	


		private IEnumerator <float> loadGameCoRoutine()
			{
			yield return Timing.waitForSeconds(2f);

			AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);			

			while(!asyncOperation.isDone)
				yield return 0;

			Destroy(cam);

			fadeOut();

			yield break;
			}


		private void fadeOut()
			{
			canvasGroup.DOFade(0, 0.5f).OnComplete(fadeOutEnded);
			}


		private void fadeOutEnded()
			{
			StartScreen.instance.canvasGroup.DOFade(1, 3f).SetDelay(0.35f);
			SceneManager.UnloadSceneAsync(0);
			}		

		#endregion
		}	
	}