using UnityEngine;
using DG.Tweening;
using Warner;
using System.Collections.Generic;
using System;

namespace Warner
	{
	public class StartScreen : MonoBehaviour
		{
		#region MEMBER FIELDS

		[NonSerialized] public CanvasGroup canvasGroup;

		public static StartScreen instance;

		private bool transitioning = true;

		#endregion

		
		
		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			canvasGroup = GetComponent<CanvasGroup>();
			}


		private void Start()
			{
//			GameAudioManager.instance.playLoopedAudio(GameAudioManager.instance.musicAudio1, 
//				GameAudioManager.instance.uiSounds.startScreenMusic.audioClip, 
//				GameAudioManager.instance.uiSounds.startScreenMusic.volume);

			StartScreen.instance.Invoke("open", 0.5f);
			}


		private void OnEnable()
			{
			InputManager.onButtonsPressed += onButtonsPressed;
			}

			
		public void open()
			{
			transitioning = true;
			canvasGroup.alpha = 0;
			gameObject.SetActive(true);
			canvasGroup.DOFade(1, GameMaster.menusFadeTransitionDuration).OnComplete(fadeInEnded);
//			GameAudioManager.instance.playLoopedAudio(GameAudioManager.instance.musicAudio1, 
//				GameAudioManager.instance.uiSounds.startScreenMusic.audioClip, GameAudioManager.instance.uiSounds.startScreenMusic.volume, GameMaster.menusFadeTransitionDuration);
			}


		private void fadeInEnded()
			{
			transitioning = false;
			}
					
		#endregion



		#region CLOSE STUFF

		public void close()
			{
			if (transitioning)
				return;

			transitioning = true;
			gameObject.SetActive(true);
			canvasGroup.DOFade(0, GameMaster.menusFadeTransitionDuration).OnComplete(fadeOutEnded);
//			AudioManager.instance.stopLoopedAudio(AudioManager.instance.musicAudioSource1, GameMaster.menusSoundTransitionDuration);
//			GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.startScreenToMainMenu);
			}


		private void OnDisable()
			{
			InputManager.onButtonsPressed -= onButtonsPressed;
			}


		private void fadeOutEnded()
			{
			transitioning = false;
			gameObject.SetActive(false);
			MainMenu.instance.gamePadControlEnabled = true;
			MainMenu.instance.open();
			}

		#endregion


		
		#region EVENT HANDLERS STUFF

		private void onButtonsPressed(List<InputButton> buttons)
			{					
			for (int i = 0;i<buttons.Count;i++)
				{
				if (buttons[i].rawName == "LeftArrow" || buttons[i].rawName == "RightArrow")
					continue;
				
				GlobalSettings.saveSettings();//save the settings also so we save the "selected" language
				close();
				}
			}

		#endregion
		}

	}