using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using Warner;

namespace Warner
	{
	public class MainMenu: MonoBehaviour
		{
		#region MEMBER FIELDS

		public float selectedBackgroundAlpha;

		public static MainMenu instance;
		
		[NonSerialized] public bool gamePadControlEnabled;

		private CanvasGroup canvasGroup;
		private Transform[] items;
		private int selectedMenuItemIndex = -1;
		private Transform itemsParent;
		private int originalFontSize;
		private bool transitioning;

		private MenuItems swappableItems;

		private struct MenuItems
			{
			public GameObject startGame;
			public GameObject exit;
			}

		private const int selectedFontSizeIncrease = 4;
		private const float soundFadeOutDuration = 1.25f;

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			gameObject.SetActive(false);
			instance = this;

			canvasGroup = GetComponent<CanvasGroup>();

			List<Transform> itemsList = new List<Transform>();
			itemsParent = transform.Find("MenuBox").Find("ItemsBox");

			for (int i = 0;i<itemsParent.childCount;i++)
				itemsList.Add(itemsParent.GetChild(i).GetChild(0));

			originalFontSize = itemsList[0].GetChild(0).GetComponent<Text>().fontSize;

			items = itemsList.ToArray();

			swappableItems.startGame = itemsParent.Find("StartGame").gameObject;
			swappableItems.exit = itemsParent.Find("ExitGame").gameObject;
			}


		private void OnEnable()
			{
			InputManager.onAcceptPressed += onAcceptPressed;
			InputManager.onCancelPressed += onCancelPressed;
			InputManager.onDirectionChange += onInputDirectionChange;
			}

		
		public void open(bool disableFadeIn = false)
			{
			swappableItems.startGame.SetActive(true);
			swappableItems.exit.SetActive(true);

			if (disableFadeIn)
				canvasGroup.alpha = 1;
				else
				{
				transitioning = true;
				canvasGroup.alpha = 0;
				canvasGroup.DOFade(1, GameMaster.menusFadeTransitionDuration).OnComplete(fadeInEnded);
				}

			gameObject.SetActive(true);
			gamePadControlEnabled = true;

//			AudioManager.instance.playLoopedAudio(AudioManager.instance.musicAudio2, 
//				GameAudioManager.instance.uiSounds.mainMenuMusic.audioClip, 
//				GameAudioManager.instance.uiSounds.mainMenuMusic.volume);
			}


		private void fadeInEnded()
			{
			transitioning = false;
			}

			
		#endregion


		
		#region CLOSE STUFF

		private void OnDisable()
			{
			InputManager.onAcceptPressed -= onAcceptPressed;
			InputManager.onCancelPressed -= onCancelPressed;
			InputManager.onDirectionChange -= onInputDirectionChange;
			}


		public void close(string action = "")
			{	
			if (transitioning)
				return;

			float fadeOutDuration = GameMaster.menusFadeTransitionDuration;

			if (LevelMaster.instance.gameObject.activeSelf)
				{
				GC.Collect();
				fadeOutDuration *= 0.25f;
				}

			transitioning = true;
			selectedMenuItemIndex = -1;
			selectItem(false);
			canvasGroup.DOFade(0, fadeOutDuration).OnComplete(() => fadeOutEnded(action));
//			AudioManager.instance.stopLoopedAudio(AudioManager.instance.musicAudioSource2, soundFadeOutDuration);
			}


		private void fadeOutEnded(string action)
			{
			switch (action)
				{
				case "startGame":
					LevelMaster.instance.loadLevel();
				break;
				case "startScreen":
					StartScreen.instance.open();
				break;
				}

			transitioning = false;
			gameObject.SetActive(false);
			}


		private void closeOtherWindows()
			{
			if (SettingsMenu.instance.gameObject.activeSelf)
				SettingsMenu.instance.close(false, false);		
			}

		#endregion



		#region ITEM NAVIGATION STUFF

		private void onInputDirectionChange(InputDirection direction)
			{
			if (!gamePadControlEnabled)
				return;

			switch (direction)
				{
				case InputDirection.Up:
					if (selectedMenuItemIndex==-1)
						selectedMenuItemIndex = items.Length;
					selectedMenuItemIndex--;
				break;
				case InputDirection.Down:
					selectedMenuItemIndex++;
				break;
				case InputDirection.Left:
				case InputDirection.Right:
					return;
				}

			if (selectedMenuItemIndex>items.Length-1)
				selectedMenuItemIndex = 0;
			else
			if (selectedMenuItemIndex<0)
					selectedMenuItemIndex = items.Length-1;

			if (!itemsParent.GetChild(selectedMenuItemIndex).gameObject.activeSelf)
				{
				onInputDirectionChange(direction);
				return;
				}
			
			selectItem();
			}

		public void selectItemFromClick()
			{
			if (GameMaster.instance.eventSystem.currentSelectedGameObject==null)
				return;

			closeOtherWindows();
				
			selectedMenuItemIndex = GameMaster.instance.eventSystem.currentSelectedGameObject.transform.parent.GetSiblingIndex();
			selectItem(false);
			goToSelected();
			}

			
		private void selectItem(bool playSound = true)
			{
			DOTween.Kill("MainMenuItemFade");

			Transform toSelect = null;
			if (selectedMenuItemIndex!=-1)
				toSelect = items[selectedMenuItemIndex].transform;

			Image image;
			Text text;
			Transform itemBox;
			Transform textBox;

			for (int i = 0;i<itemsParent.childCount;i++)
				{
				itemBox = itemsParent.GetChild(i).GetChild(0);
				textBox = itemBox.GetChild(0);
				image = itemBox.GetComponent<Image>();			
				text = textBox.GetComponent<Text>();		

				if (itemBox==toSelect)
					{
					image.DOFade(selectedBackgroundAlpha, 0).SetId("MainMenuItemFade");
					text.fontSize = originalFontSize+selectedFontSizeIncrease;
					}
				else
					{
					image.DOFade(0, 0).SetId("MainMenuItemFade");
					text.fontSize = originalFontSize;
					}
				}			

//			if (playSound)
//				AudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.itemVerticalToggle);	
			}


		private void goToSelected()
			{
			if (!LevelMaster.instance.loadingLevel && selectedMenuItemIndex!=-1)
				{
				gamePadControlEnabled = false;

				switch (items[selectedMenuItemIndex].parent.name)
					{
					case "StartGame":
						close("startGame");
//						GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.startScreenToMainMenu);
					break;
					case "Achievements":
						//Achievements.instance.open();
//						GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.acceptCancel);
						gamePadControlEnabled = true;
					break;
					case "Settings":
						SettingsMenu.instance.open();
					break;
					case "ExitGame":
						canvasGroup.DOFade(0, 0.5f).OnComplete(Application.Quit);
					break;
					}
				}
			}


			

			
		#endregion



		#region BUTTON HANDLERS STUFF


		public void onAcceptPressed()
			{
			if (!gamePadControlEnabled)
				return;

			goToSelected();
			}


		public void onCancelPressed()
			{
			if (!gamePadControlEnabled)
				return;

			if (!transitioning)
				{
				if (!LevelMaster.instance.gameObject.activeSelf)
					close("startScreen");
				}
			}

		#endregion
		}

	}