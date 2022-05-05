using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using Warner;
using System;
using Warner.AnimationTool;

namespace Warner 
	{

	public class SettingsMenu: MonoBehaviour
		{
		#region MEMBER FIELDS

		public Transform selectedItemBackgroundLeft;
		public Transform selectedItemBackgroundRight;
		public Transform itemsNamesParent;
		public GameObject bindableItemNamePrefab;
		public GameObject bindableItemValuePrefab;
		public Transform keyboardValuesParent;
		public Transform gamePadValuesParent;
		public Transform buttonsParent;
		public Color itemDefaultFontColor;
		public Transform generalSettingsNamesParent;
		public Transform generalSettingsValuesParent;
		public ScrollRect scrollRect;
		public Color itemValueNotBoundColor;
		public Color buttonDefaultColor;
		public Color buttonSelectedColor;
		public int originalFontSize;

		public static SettingsMenu instance;

		public static class itemNames
			{
			public const string masterVolume = "MasterVolume";
			public const string soundEffectsVolume = "SoundEffectsVolume";			
			public const string musicVolume = "MusicVolume";
			public const string windowMode = "WindowMode";
			public const string resolution = "Resolution";
			public const string textureQuality = "TextureQuality";
			public const string gamePadVibration = "GamePadVibration";
			public const string screenShake = "ScreenShake";
			}

		private int globalSelectedIndex = -1;
		private int selectedBindableItemIndex = -1;
		private int selectedGeneralItemIndex = -1;
		private int selectedButtonIndex = -100;
		private Text selectedItem;
		private CanvasGroup canvasGroup;
		private RectTransform contentRT;
		private RectTransform scrollRectRT;
		private bool reBinding;
		private bool releasingRebinding;
		private float timeWeStartedReleasingReBinding;
		private string keyboardRebindingItemOldValue;
		private string gamePadRebindingItemOldValue;
		private bool settingsLoaded;
		private UICircularButton activeCircularButton;
		private UICircularButton selectedCircularButton;
		private bool lastToggleWasWithMouse;
		private bool changingOption;
		private string currentChangingOption;
		private float reBindTime;


		private const string reBindKeyboardText = "<Press any key>";
		private const string reBindGamepadText = "<Press any button>";
		private const string notBoundText = "-------------";
		private const int selectedFontSizeIncrease = 2;
		private const string valueString = "Value";

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			gameObject.SetActive(false);
			canvasGroup = GetComponent<CanvasGroup>();					
			contentRT = scrollRect.content.GetComponent<RectTransform>();	
			scrollRectRT = scrollRect.GetComponent<RectTransform>();

			createBindableItems();

			float generalSettingsHeight = generalSettingsNamesParent.GetComponent<RectTransform>().rect.height;
			GridLayoutGroup itemNamesParentRT = itemsNamesParent.GetComponent<GridLayoutGroup>();
			float itemSize = itemNamesParentRT.cellSize.y + itemNamesParentRT.spacing.y;
			float bindableItemsHeight = itemSize * itemsNamesParent.childCount;

			contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bindableItemsHeight + generalSettingsHeight);
			resetLastSelectedItem();
			}



		private void OnEnable()
			{
			InputManager.onButtonsHold += onButtonsPressed;
			InputManager.onAcceptPressed += onAcceptPressed;
			InputManager.onCancelPressed += onCancelPressed;
			InputManager.onDirectionChange += onInputDirectionChange;
			}


		public void open()
			{
			if (gameObject.activeSelf)
				return;

//			GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.windowOpen);

			putSettingsValues();

			gameObject.SetActive(true);
			canvasGroup.alpha = 0;
			canvasGroup.DOFade(1, GameMaster.menusFadeTransitionDuration);
			}


		private void createBindableItems()
			{
			for (int i = 0;i<GlobalSettings.settings.playerControls[0].actions.Length;i++)
				{
				createBindableItemName(GlobalSettings.settings.playerControls[0].actions[i].name);
				createBindableItemValue(i, GlobalSettings.settings.playerControls[0].actions[i].name);
				createBindableItemValue(i, GlobalSettings.settings.playerControls[0].actions[i].name, true);
				}
			}



		private void createBindableItemName(string id)
			{
			GameObject itemGameObject = (GameObject) Instantiate(bindableItemNamePrefab, itemsNamesParent);
			UIText uiText = itemGameObject.GetComponent<UIText>();
            
			itemGameObject.name = id;
			uiText.text = TextExtensions.lowerFirst(id, false);
			}


		private void createBindableItemValue(int index, string id, bool isGamePad = false)
			{
			GameObject itemGameObject = (GameObject) Instantiate(bindableItemValuePrefab, (isGamePad) ? gamePadValuesParent : keyboardValuesParent);
			itemGameObject.name = id+"Value";
			itemGameObject.GetComponent<Button>().onClick.AddListener(() => reBindFromClick(index));
			}

		#endregion



		#region CLOSE STUFF

		public void close(bool playSound = true, bool controlToMainMenu = true)
			{
			InputManager.onButtonsPressed -= onButtonsPressed;
			InputManager.onAcceptPressed -= onAcceptPressed;
			InputManager.onCancelPressed -= onCancelPressed;	
			InputManager.onDirectionChange -= onInputDirectionChange;					

//			if (playSound)
//				GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.windowClose);

			MainMenu.instance.gamePadControlEnabled = controlToMainMenu;
			canvasGroup.DOFade(0, GameMaster.menusFadeTransitionDuration).OnComplete(() => fadeOutEnded(controlToMainMenu));
			}


		private void fadeOutEnded(bool controlToMainMenu)
			{
			resetLastSelectedItem();

			globalSelectedIndex = -1;
			selectedBindableItemIndex = -1;
			selectedGeneralItemIndex = -1;

			gameObject.SetActive(false);
			}

		#endregion



		#region LOADING AND SAVING STUFF

		public void restoreDefaultSettings()
			{			
			settingsLoaded = false;
			GlobalSettings.init(false);			
			putSettingsValues();
			updateKeyBindingsSettingsAndSave();
			}


		public void putSettingsValues(bool resetScrollPosition = true)
			{
			Text text;
			Transform item;

			for (int i = 0; i < GlobalSettings.settings.playerControls[0].actions.Length; i++)
				{
				item = keyboardValuesParent.Find(GlobalSettings.settings.playerControls[0].actions[i].name + "Value");
				if (item != null)
					{
					text = item.GetComponent<Text>();
					if (text != null)
						{
						originalFontSize = text.fontSize;
						if (GlobalSettings.settings.playerControls[0].actions[i].key == notBoundText)
							text.color = itemValueNotBoundColor;
							else
							text.color = itemDefaultFontColor;

						text.text = GlobalSettings.settings.playerControls[0].actions[i].key;
						}
					}


				item = gamePadValuesParent.Find(GlobalSettings.settings.playerControls[0].actions[i].name + "Value");
				if (item != null)
					{
					text = item.GetComponent<Text>();
					if (text != null)
						{
						if (GlobalSettings.settings.playerControls[0].actions[i].button.ToString() == notBoundText)
							text.color = itemValueNotBoundColor;
							else
							text.color = itemDefaultFontColor;
						
						text.text = GlobalSettings.settings.playerControls[0].actions[i].button.ToString();
						}
					}
				}


			generalSettingsValuesParent.Find(itemNames.masterVolume + valueString).GetComponent<Slider>().value = GlobalSettings.settings.masterVolume;
			generalSettingsValuesParent.Find(itemNames.soundEffectsVolume+valueString).GetComponent<Slider>().value = GlobalSettings.settings.soundEffectsVolume;			
			generalSettingsValuesParent.Find(itemNames.musicVolume+valueString).GetComponent<Slider>().value = GlobalSettings.settings.musicVolume;

			generalSettingsValuesParent.Find(itemNames.windowMode+valueString).GetChild(0).GetComponent<UICircularButton>().
				init(new []{ "FullScreen", "Window" }, GlobalSettings.settings.windowMode.ToString());


			List<string> options = new List<string>();

			if (Application.isEditor)
				options.Add("640x480");
			else
				for (int i = 0;i<Screen.resolutions.Length;i++)
					options.Add(Screen.resolutions[i].width+"x"+Screen.resolutions[i].height);

			generalSettingsValuesParent.Find(itemNames.resolution+valueString).GetChild(0).GetComponent<UICircularButton>().
			init(options.ToArray(), GlobalSettings.settings.resolution);                

			if (resetScrollPosition)
				contentRT.anchoredPosition = Vector2.zero;

			settingsLoaded = true;
			}


		public void updateKeyBindingsSettingsAndSave()
			{
			Transform item;
			string settingName;
			for (int i = 0;i<keyboardValuesParent.childCount;i++)
				{
				item = keyboardValuesParent.GetChild(i);
				settingName = item.name.Replace("Value", "");
				for (int x = 0; x < GlobalSettings.settings.playerControls[0].actions.Length; x++)
					{
					if (GlobalSettings.settings.playerControls[0].actions[x].name == settingName)
						{
						GlobalSettings.settings.playerControls[0].actions[x].key = item.GetComponent<Text>().text;
						break;
						}
					}
				}

			
			for (int i = 0;i<gamePadValuesParent.childCount;i++)
				{
				item = gamePadValuesParent.GetChild(i);
				settingName = item.name.Replace("Value", "");

				for (int x = 0; x < GlobalSettings.settings.playerControls[0].actions.Length; x++)
					{
					if (GlobalSettings.settings.playerControls[0].actions[x].name == settingName)
						{
						try
							{
							GlobalSettings.settings.playerControls[0].actions[x].button = Misc.parseEnum<JoystickButton>(item.GetComponent<Text>().text);
							}
						catch (Exception e)
							{
							GlobalSettings.settings.playerControls[0].actions[x].button = JoystickButton.Invalid;
							}
						break;
						}
					}		
				}
			
			

			GlobalSettings.settings.masterVolume = generalSettingsValuesParent.Find(itemNames.masterVolume + valueString).GetComponent<Slider>().value;
			GlobalSettings.settings.soundEffectsVolume = generalSettingsValuesParent.Find(itemNames.soundEffectsVolume+valueString).GetComponent<Slider>().value;			
			GlobalSettings.settings.musicVolume = generalSettingsValuesParent.Find(itemNames.musicVolume+valueString).GetComponent<Slider>().value;
			GlobalSettings.saveSettings();
			}

		#endregion



		#region ITEM NAVIGATION STUFF


		private void onInputDirectionChange(InputDirection direction)
			{
			if (reBinding)
				return;		

			if (direction==InputDirection.Left || direction==InputDirection.Right)
				{
				checkForDirectionalOptions(direction);
				return;
				}

			if (changingOption)
				return;

			switch (direction)
				{
				case InputDirection.Up:
					globalSelectedIndex--;
					if (selectedButtonIndex!=-100)
						selectedButtonIndex--;
					break;
				case InputDirection.Down:
					globalSelectedIndex++;
					if (selectedButtonIndex!=-100)
						selectedButtonIndex++;
					break;		
				}		

			resetLastSelectedItem();

			int totalOptions = generalSettingsNamesParent.childCount+keyboardValuesParent.childCount;
			const int dummyButtonsCount = 1;


			if (selectedButtonIndex!=-100)//for buttons we use -100 for knowing its not beeing used
				{
				if (selectedButtonIndex>dummyButtonsCount-1)
					{
					globalSelectedIndex = 0;
					selectedButtonIndex = -1;
					}
					else
					if (selectedButtonIndex<0)
						{
						globalSelectedIndex = totalOptions-1;
						selectedButtonIndex = -1;
						}
				}


			if (globalSelectedIndex<generalSettingsNamesParent.childCount)
				{
				selectedGeneralItemIndex = globalSelectedIndex;
				selectedBindableItemIndex = -1;
				selectedButtonIndex = -100;
				}
				else
				{
				selectedBindableItemIndex = globalSelectedIndex-generalSettingsNamesParent.childCount;
				selectedGeneralItemIndex = -1;
				selectedButtonIndex = -100;
				}							


			if (direction==InputDirection.Down && globalSelectedIndex>totalOptions-1)//we reached the end of all of our options
				{				
				selectedButtonIndex = 0;
				globalSelectedIndex = -1;
				selectedBindableItemIndex = -1;
				selectedGeneralItemIndex = -1;	
				}


			if (direction==InputDirection.Up && globalSelectedIndex<0)
				{				
				selectedButtonIndex = 0;
				globalSelectedIndex = -1;
				selectedBindableItemIndex = -1;
				selectedGeneralItemIndex = -1;	
				}			


			if (selectedButtonIndex!=-100)
				{
				Image buttonImage = buttonsParent.GetChild(selectedButtonIndex).GetComponent<Image>();
				buttonImage.color = buttonSelectedColor;
				return;
				}
				else
				{
				Image buttonImage;
				for (int i = 0;i<buttonsParent.childCount;i++)
					{
					buttonImage = buttonsParent.GetChild(i).GetComponent<Image>();
					buttonImage.color = buttonDefaultColor;
					}
				}


			//if we are not on buttons then we can select one of the items

			Transform item = null;
			if (selectedGeneralItemIndex!=-1)//moving in the general options
				item = generalSettingsNamesParent.GetChild(selectedGeneralItemIndex);


			if (selectedBindableItemIndex!=-1)//moving in the bindable option	
				item = itemsNamesParent.GetChild(selectedBindableItemIndex);

			selectSettingsItemName(item);			

			RectTransform itemRT = item.GetComponent<RectTransform>();
			const float offset = 58;
			float itemPosition = globalSelectedIndex*itemRT.rect.height;
			float boxRelativeHeight = scrollRectRT.rect.height+contentRT.anchoredPosition.y;

	
			if (globalSelectedIndex==0)
				scrollRect.verticalScrollbar.value = 1;
				else
				if (globalSelectedIndex==totalOptions-1)
					scrollRect.verticalScrollbar.value = 0;
				else
					if ((itemPosition+offset>boxRelativeHeight) || (itemPosition-offset<contentRT.anchoredPosition.y))
						scrollRect.verticalScrollbar.value = 1f-globalSelectedIndex/(totalOptions-1f);
		
			}



		private void checkForDirectionalOptions(InputDirection direction)
			{			
			if (activeCircularButton!=null)
				activeCircularButton.toggleSelectedOption((direction==InputDirection.Right) ? 1 : -1);	

			if (selectedGeneralItemIndex==-1)
				return;

			checkForSliderChanges(direction);
			}


		private void resetLastSelectedItem()
			{
			Vector3 position = selectedItemBackgroundLeft.position;
			position.y = -2000;
			selectedItemBackgroundLeft.position = position;

			position.x = selectedItemBackgroundRight.position.x;
			selectedItemBackgroundRight.position = position;


			if (selectedCircularButton!=null)
				{
				selectedCircularButton.selected = false;
				selectedCircularButton = null;
				}


			if (selectedItem==null)
				return;

			selectedItem.fontSize = originalFontSize;

			if (selectedBindableItemIndex!=-1)
				{
				keyboardValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().fontSize = originalFontSize;
				gamePadValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().fontSize = originalFontSize;
				}
				else
				if (selectedGeneralItemIndex!=-1)
					{
					Text valueText = generalSettingsValuesParent.GetChild(selectedGeneralItemIndex).GetComponent<Text>();

					if (valueText!=null)
						valueText.fontSize = originalFontSize;
					}

			GameMaster.instance.eventSystem.SetSelectedGameObject(null);
			selectedItem = null;
			}



		public void selectSettingsItemName(Transform item, bool playSound = true, bool activateCircularButton = false)
			{
			if (item==null)
				return;

			Text text = item.GetComponent<Text>();
			selectedItem = text;
			text.fontSize = originalFontSize+selectedFontSizeIncrease;


			if (selectedBindableItemIndex!=-1)
				{
				keyboardValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().fontSize = originalFontSize+selectedFontSizeIncrease;
				gamePadValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().fontSize = originalFontSize+selectedFontSizeIncrease;
				}
			else
				if (selectedGeneralItemIndex!=-1)
					{
					Transform valuesItem = generalSettingsValuesParent.GetChild(item.GetSiblingIndex());

					selectedCircularButton = valuesItem.GetChild(0).GetComponent<UICircularButton>();

					if (selectedCircularButton!=null)
						{
						selectedCircularButton.selected = true;

						if (activateCircularButton)
							{
							selectedCircularButton.active = true;
//							GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.acceptCancel);
							}
						}
					}

//			if (playSound)
//				GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.itemVerticalToggle);

			/*POSITION THE ITEM SELECTED BACKGROUNDS*/

			//we preserve the X position defined in the editor so the resolution changes dont affect us
			//if we try to follow an items X position wich is centered instead of starting at the far left
			Vector3 position = selectedItemBackgroundLeft.position;
			position.y = item.position.y+1;
			selectedItemBackgroundLeft.position = position;

			position.x = selectedItemBackgroundRight.position.x;
			selectedItemBackgroundRight.position = position;
			}

		#endregion



		#region SLIDER STUFF

		private void checkForSliderChanges(InputDirection direction)
			{
			Slider slider = generalSettingsValuesParent.GetChild(selectedGeneralItemIndex).GetComponent<Slider>();
			if (slider!=null)
				{
				if (direction==InputDirection.Right)
					slider.value += 0.1f;
					else
					slider.value -= 0.1f;

//				GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.itemHorizontalToggle);
				return;
				}
			}


		public void onSliderChange()
			{		
			if (settingsLoaded)
				updateKeyBindingsSettingsAndSave();
			}

		#endregion



		#region BUTTON HANDLERS STUFF

		public void onAcceptPressed()
			{
			if (reBinding)
				return;		

			if (executeOption())
				return;

			
			if (selectedItem!=null && selectedBindableItemIndex!=-1)//hiting an actual item and its a bindable one
				{
				startReBinding();				
				return;
				}


			if (selectedItem!=null && selectedGeneralItemIndex!=-1)//hiting a general item
				{
				Transform item = generalSettingsNamesParent.GetChild(selectedGeneralItemIndex);				
				executeCircularOption(item);				
				return;
				}

			//we are hitting a button
			if ((globalSelectedIndex==-1 || globalSelectedIndex==itemsNamesParent.childCount) && selectedButtonIndex!=-100)
				{
				switch (buttonsParent.GetChild(selectedButtonIndex).name)
					{
					case "DefaultsBtn":
						//restoreDefaultSettings();
					break;
					}				
				}
			}


		public void onCancelPressed()
			{
			if (reBinding)
				return;

			if (executeOption(true))
				return;

			close();
			}


		private void onButtonsPressed(List<InputButton> buttons)
			{			
			if (reBinding)
				{
				for (int i = 0; i < buttons.Count; i++)
					if (InputManager.passesMainAxisDeadZone(buttons[i]))
						{
						if (buttons[i].device == InputDevice.Joystick)
							reBindToThis(InputManagerExtensions.buttonToJoystickGenericButton(buttons[i]).ToString(), true);
							else	
							reBindToThis(buttons[i].rawName);
						}
				}
			}

		#endregion



		#region REBIND KEY STUFF


		public void reBindFromClick(int index)
			{
			if (areWeChangingAnOption())
				return;				

			resetLastSelectedItem();

			selectedGeneralItemIndex = -1;
			selectedBindableItemIndex = index;
			lastToggleWasWithMouse = true;
			globalSelectedIndex = selectedBindableItemIndex+generalSettingsNamesParent.childCount;

			selectSettingsItemName(itemsNamesParent.GetChild(index), false);
			startReBinding();
			}


		private void startReBinding()
			{
//			GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.acceptCancel);
			reBinding = true;			
			releasingRebinding = false;
			Text gamePadItemText = gamePadValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>();
			Text keyboardItemText = keyboardValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>();
			keyboardRebindingItemOldValue = keyboardItemText.text;
			gamePadRebindingItemOldValue = gamePadItemText.text;
			gamePadItemText.text = reBindGamepadText;
			keyboardItemText.text = reBindKeyboardText;

			if (reBindGamepadText!=notBoundText)
				gamePadItemText.color = itemDefaultFontColor;

			if (reBindKeyboardText!=notBoundText)
				keyboardItemText.color = itemDefaultFontColor;

			reBindTime = Time.time;
			}


		private void reBindToThis(string button, bool isGamePad = false)
			{
			if (Time.time - reBindTime < 0.1f)
				return;

			if (button=="Escape")
				{
				putSettingsValues(false);
				endRebinding();
				return;
				}


			Transform itemsParent = null;

			if (isGamePad)
				{
				itemsParent = gamePadValuesParent;
				gamePadValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().text = button;
				keyboardValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().text = keyboardRebindingItemOldValue;
				}
			else
				{
				itemsParent = keyboardValuesParent;
				keyboardValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().text = button;
				gamePadValuesParent.GetChild(selectedBindableItemIndex).GetComponent<Text>().text = gamePadRebindingItemOldValue;
				}

			//check for duplicates and if found unbound it
			Text text = null;
			for (int i = 0;i<itemsParent.childCount;i++)
				if (i!=selectedBindableItemIndex)
					{
					text = itemsParent.GetChild(i).GetComponent<Text>();
					if (text.text==button)
						{
						text.text = notBoundText;
						text.color = itemValueNotBoundColor;
						}
					}

			updateKeyBindingsSettingsAndSave();		
			endRebinding();
			}


		private void endRebinding()
			{
			if (releasingRebinding)
				return;

			releasingRebinding = true;
			timeWeStartedReleasingReBinding = Time.unscaledTime;
			cancelToggle();
			}

		#endregion



		#region FRAME UPDATE

		private void Update()
			{
			if (releasingRebinding && Time.unscaledTime-timeWeStartedReleasingReBinding>0.25f)
				{
				reBinding = false;
				releasingRebinding = false;
				}
			}

		#endregion



		#region TOGGLE STUFF

		private void cancelToggle()
			{
			changingOption = false;

			if (lastToggleWasWithMouse)
				resetLastSelectedItem();						

			lastToggleWasWithMouse = false;
//			GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.acceptCancel);			
			}


		private bool areWeChangingAnOption()
			{
			return GameMaster.instance.eventSystem.currentSelectedGameObject==null || reBinding || changingOption;
			}

		#endregion



		#region CIRCULAR BUTTONS STUFF

		public void activateCircularButtonByClick()
			{
			if (executeOption())
				return;

			int index = GameMaster.instance.eventSystem.currentSelectedGameObject.transform.GetSiblingIndex();
			resetLastSelectedItem();
			Transform item = generalSettingsNamesParent.GetChild(index);
			selectSettingsItemName(item, false, true);	
			executeCircularOption(item);
			}


		private void resetCircularButton()
			{
			if (activeCircularButton==null)
				return;

			activeCircularButton.active = false;
			activeCircularButton = null;
			}


		private void executeCircularOption(Transform item)
			{
			Transform circularButtonTransform = generalSettingsValuesParent.Find(item.name+valueString).GetChild(0);
			activeCircularButton = circularButtonTransform.GetComponent<UICircularButton>();

			if (activeCircularButton==null)
				return;

			changingOption = true;
			currentChangingOption = item.name;
			activeCircularButton.active = true;

//			GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.acceptCancel);
			}

		#endregion



		#region OPTIONS EXECUTE


		private bool executeOption(bool isCancel = false)
			{
			if (activeCircularButton==null)
				{
				if (selectedCircularButton!=null && selectedCircularButton.active)
					activeCircularButton = selectedCircularButton;
				else
					return false;
				}

			string[] selectedResolution;
			int width;
			int height;

			switch (currentChangingOption)
				{
				case itemNames.windowMode:					
					if (isCancel)
						{
						activeCircularButton.selectOption(GlobalSettings.settings.windowMode.ToString());
						break;
						}

					bool fullScreen = activeCircularButton.getText()==WindowMode.FullScreen.ToString();

					selectedResolution = GlobalSettings.settings.resolution.Split('x');
					int.TryParse(selectedResolution[0], out width);
					int.TryParse(selectedResolution[1], out height);

					Screen.SetResolution(width, height, fullScreen);

					GlobalSettings.settings.windowMode = (fullScreen) ? WindowMode.FullScreen : WindowMode.Window;
					break;
				case itemNames.resolution:
					if (isCancel)
						{
						activeCircularButton.selectOption(GlobalSettings.settings.resolution);
						break;
						}					

					selectedResolution = activeCircularButton.getText().Split('x');
					int.TryParse(selectedResolution[0], out width);
					int.TryParse(selectedResolution[1], out height);

					Screen.SetResolution(width, height, GlobalSettings.settings.windowMode==WindowMode.FullScreen);

					GlobalSettings.settings.resolution = activeCircularButton.getText();						
					break;
				case itemNames.gamePadVibration:
					if (isCancel)
						{
						activeCircularButton.selectOption(GlobalSettings.settings.gamePadVibration ? "On" : "Off");
						break;
						}

					GlobalSettings.settings.gamePadVibration = activeCircularButton.getText()=="On";
					break;
				}

			if (!isCancel)
				GlobalSettings.saveSettings();

//			GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.acceptCancel);

			resetCircularButton();

			changingOption = false;
			return true;
			}


		#endregion
		}

	}