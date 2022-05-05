using UnityEngine;
using UnityEngine.UI;

namespace Warner 
	{
	public class UICircularButton: MonoBehaviour
		{
		#region MEMBER FIELDS

		public bool useInternalClickEvents = true;

		private RectTransform rectTransform;
		private Text textObj;
		private Button button;
		private Button leftArrowButton;
		private Button rightArrowButton;
		private GameObject leftArrow;
		private GameObject rightArrow;
		private bool _active;
		private bool _selected;
		private string[] options;
		private int selectedOption;
		private int originalFontSize;
		private bool referencesReady;

		private const float activePadding = -30;
		private const float leftArrowPadding = -16;
		private const float rightArrowPadding = 8;
		private const int selectedFontSizeIncrease = 6;

		#endregion


		
		#region INIT STUFF

		private void getReferences()
			{
			if (referencesReady)
				return;

			rectTransform = GetComponent<RectTransform>();
			textObj = GetComponent<Text>();
			button = GetComponent<Button>();

			leftArrow = transform.Find("LeftArrow").gameObject;
			leftArrowButton = leftArrow.GetComponent<Button>();
			leftArrow.GetComponent<RectTransform>().anchoredPosition = new Vector2(leftArrowPadding,0);

			rightArrow = transform.Find("RightArrow").gameObject;
			rightArrowButton = rightArrow.GetComponent<Button>();
			rightArrow.GetComponent<RectTransform>().anchoredPosition = new Vector2(rightArrowPadding,0);


			if (useInternalClickEvents)
				{
				button.onClick.AddListener(toggleActive);
				textObj.raycastTarget = true;
				}
				else
				textObj.raycastTarget = false;

			leftArrowButton.onClick.AddListener(()=>toggleSelectedOption(-1));
			rightArrowButton.onClick.AddListener(()=>toggleSelectedOption(1));
			originalFontSize = textObj.fontSize;

			referencesReady = true;
			}


		public void init(string[] options,string selected)
			{
			getReferences();

			leftArrow.SetActive(false);
			rightArrow.SetActive(false);

			this.options = options;
			for (int i=0;i<options.Length;i++)
				if (options[i]==selected)
					{
					selectedOption = i;
					break;
					}

			setText(options[selectedOption]);
			}

		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{
			if (referencesReady && active)
				toggleActive();
			}

		#endregion



		#region TEXT STUFF

		private void resizeTransform()
			{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,LayoutUtility.GetPreferredWidth(rectTransform));
			}


		public string getText()
			{
			return textObj.text;
			}


		private void setText(string text)
			{
			textObj.text = text;
			Invoke("resizeTransform",0.001f);		
			}


		public void selectOption(string text)
			{
			for (int i=0;i<options.Length;i++)
				if (options[i]==text)
					{
					selectedOption = i;
					setText(text);
					}
			}


		public bool active
			{
			get
				{
				return _active;
				}
			set 
				{
				_active = value;

				if (_active)
					{
					rectTransform.anchoredPosition = new Vector2(activePadding,0);
					textObj.resizeTextForBestFit = true;
					resizeTransform();
					leftArrow.SetActive(true);
					rightArrow.SetActive(true);
					}
					else
					{
					rectTransform.anchoredPosition = new Vector2(0,0);
					leftArrow.SetActive(false);
					rightArrow.SetActive(false);
					}
				}
			}


		public bool selected
			{
			get
				{
				return _selected;
				}

			set
				{
				_selected = value;

				float parentWidth = transform.parent.GetComponent<RectTransform>().rect.width;
				textObj.resizeTextForBestFit = false;
				rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,parentWidth);

				if (!_selected)
					active = false;

				updateFontSize(_selected);
				}
			}



		private void updateFontSize(bool increase)
			{
			if (increase)
				textObj.fontSize = originalFontSize + selectedFontSizeIncrease;
				else
				textObj.fontSize = originalFontSize;
			}

		#endregion



		#region LISTENER STUFF

		private void toggleActive()
			{
			active = !active;

			if (active)
				{
				UICircularButton[] buttons = FindObjectsOfType<UICircularButton>();
				for (int i=0;i<buttons.Length;i++)
					if (buttons[i].GetInstanceID()!=GetInstanceID())
						buttons[i].active = false;
				}

			}


		public void toggleSelectedOption(int side)
			{
			selectedOption += side;

			if (selectedOption>=options.Length)
				selectedOption = 0;
				else
				if (selectedOption<0)
					selectedOption = options.Length-1;

			setText(options[selectedOption]);

//			GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.itemHorizontalToggle);
			}

		#endregion
		}

	}