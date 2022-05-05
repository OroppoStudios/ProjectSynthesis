using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Warner;

namespace Warner 
	{

	public class LanguagesCarousel: MonoBehaviour
		{
		#region MEMBER FIELDS

		public GameObject itemPrefab;

		public static LanguagesCarousel instance;

		private Buddy centerBuddy;
		private Buddy leftBuddy;
		private Buddy leftBuddy2;
		private Buddy leftBuddy3;
		private Buddy farLeftBuddy;
		private Buddy rightBuddy;
		private Buddy rightBuddy2;
		private Buddy rightBuddy3;
		private Buddy farRightBuddy;
		private Vector2 centerBuddyDefaultPosition;
		private Vector2 leftBuddyDefaultPosition;
		private Vector2 leftBuddy2DefaultPosition;
		private Vector2 leftBuddy3DefaultPosition;
		private Vector2 farLeftBuddyDefaultPosition;
		private Vector2 rightBuddyDefaultPosition;
		private Vector2 rightBuddy2DefaultPosition;
		private Vector2 rightBuddy3DefaultPosition;
		private Vector2 farRightBuddyDefaultPosition;
		private int index = 0;
		private bool movingRight = true;
		private string initialLanguage;

		private const int boxSeparation = 10;
		private const float secondaryScale = 0.7f;
		private const float secondaryScale2 = 0.7f;
		private const float secondaryScale3 = 0.7f;
		private const int mainBuddySeparation = -24;
		private const int farBuddyOneAndTwoSeparationOffset = -182;
		private const int farBuddy3SeparationOffset = -182;
		private const float transitionSpeed = 0.26f;

		private class Buddy
			{
			public RectTransform rectTransform;
			public Text textComponent;
			public Image image;
			public string language;

			public Buddy(GameObject prefab,Vector2 position,Transform parent)
				{
				GameObject buddyObject = Instantiate(prefab,position,Quaternion.identity) as GameObject;
				buddyObject.transform.SetParent(parent,false);
				rectTransform = buddyObject.GetComponent<RectTransform>();
				textComponent = buddyObject.transform.Find("Text").GetComponent<Text>();
				image = buddyObject.transform.Find("Image").GetComponent<Image>();
				}


			public void update(Languages.LanguageItem item)
				{
				textComponent.text = item.text;
				image.sprite = item.sprite;
				language = item.name;
				}

			public void moveInHierarchy(int i)
				{
				rectTransform.SetSiblingIndex(i);
				}
			
			}

		#endregion


		
		#region INIT STUFF


		private void Awake()
			{
			instance = this;
			}


		private void Start()
			{
			populateBox();
			}


		private void OnEnable()
			{
			InputManager.onDirectionChange += onDirectionChange;
			initialLanguage = GlobalSettings.settings.language;
			}


		public int getLanguageIndex(string language)
			{
			for (int i=0;i<Languages.instance.languages.Length;i++)
				if (Languages.instance.languages[i].name.ToLower()==language.ToLower())
					return i;

			return -1;
			}
														

		private void populateBox()
			{
			index = getLanguageIndex(GlobalSettings.settings.language);

			Vector2 position = Vector2.zero;

			centerBuddyDefaultPosition = position;
			centerBuddy = new Buddy(itemPrefab,position,transform);

			position.x -= centerBuddy.rectTransform.rect.width+boxSeparation+farBuddyOneAndTwoSeparationOffset;
			leftBuddyDefaultPosition = position;
			leftBuddy = new Buddy(itemPrefab,position,transform);
					
			position.x -= leftBuddy.rectTransform.rect.width+boxSeparation+farBuddyOneAndTwoSeparationOffset;
			leftBuddy2DefaultPosition = position;
			leftBuddy2 = new Buddy(itemPrefab,position,transform);

			position.x -= leftBuddy2.rectTransform.rect.width+boxSeparation+mainBuddySeparation;
			leftBuddy3DefaultPosition = position;
			leftBuddy3 = new Buddy(itemPrefab,position,transform);

			farLeftBuddyDefaultPosition = leftBuddy3DefaultPosition-new Vector2(-50,0);//we put it on same position so it looks like a 3d carousel
			farLeftBuddy = new Buddy(itemPrefab,position,transform);

			position = Vector2.zero;
			position.x += centerBuddy.rectTransform.rect.width+boxSeparation+farBuddyOneAndTwoSeparationOffset;
			rightBuddyDefaultPosition = position;
			rightBuddy = new Buddy(itemPrefab,position,transform);

			position.x += rightBuddy.rectTransform.rect.width+boxSeparation+farBuddyOneAndTwoSeparationOffset;
			rightBuddy2DefaultPosition = position;
			rightBuddy2 = new Buddy(itemPrefab,position,transform);

			position.x += rightBuddy2.rectTransform.rect.width+boxSeparation+farBuddy3SeparationOffset;
			rightBuddy3DefaultPosition = position;
			rightBuddy3 = new Buddy(itemPrefab,position,transform);

			farRightBuddyDefaultPosition = rightBuddy3DefaultPosition;
			farRightBuddy = new Buddy(itemPrefab,position,transform);


			//put the index starting from the far left buddy (so our selected language will be at the far left)
			farLeftBuddy.update(Languages.instance.languages[index]);
			updateIndex();
			leftBuddy3.update(Languages.instance.languages[index]);
			updateIndex();
			leftBuddy2.update(Languages.instance.languages[index]);
			updateIndex();
			leftBuddy.update(Languages.instance.languages[index]);
			updateIndex();
			centerBuddy.update(Languages.instance.languages[index]);
			updateIndex();
			rightBuddy.update(Languages.instance.languages[index]);
			updateIndex();
			rightBuddy2.update(Languages.instance.languages[index]);
			updateIndex();
			rightBuddy3.update(Languages.instance.languages[index]);
			updateIndex();
			farRightBuddy.update(Languages.instance.languages[index]);		

			moveLeft(true);
			}


		public void goBackToDefault()
			{	
			while(true)
				{
				moveLeft(true);
				if (leftBuddy3.language==initialLanguage)
					return;
				}
			}


		public bool isDefaultSelected()
			{
			return initialLanguage==leftBuddy3.language;
			}


		private void updateIndex(string side = "right")
			{
			if (side=="right")
				index++;
				else
				index--;

			if (index>Languages.instance.languages.Length-1)
				index = 0;

			if (index<0)
				index = Languages.instance.languages.Length-1;
			}


		private void updateHierarchys(string side)
			{
			if (side=="right")
				{
				farLeftBuddy.moveInHierarchy(0);
				leftBuddy3.moveInHierarchy(1);
				}
				else
				{
				farLeftBuddy.moveInHierarchy(1);
				leftBuddy3.moveInHierarchy(0);
				}
			}
		
		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{
			InputManager.onDirectionChange -= onDirectionChange;
			}

		#endregion



		#region MOVEMENT STUFF	


		private void moveRight(bool forceImmediate = false)
			{
			DOTween.Kill("FlagsMovement");

			float movementSpeed = (forceImmediate) ? 0 : transitionSpeed;

			Buddy leftTempBuddy = leftBuddy;

			leftBuddy = centerBuddy;
			leftBuddy.rectTransform.DOAnchorPos(leftBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			leftBuddy.rectTransform.DOScale(secondaryScale,movementSpeed).SetId("FlagsMovement");

			centerBuddy = rightBuddy;
			centerBuddy.rectTransform.DOAnchorPos(centerBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			centerBuddy.rectTransform.DOScale(secondaryScale,movementSpeed).SetId("FlagsMovement");

			rightBuddy = rightBuddy2;
			rightBuddy.rectTransform.DOAnchorPos(rightBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			rightBuddy.rectTransform.DOScale(secondaryScale,movementSpeed).SetId("FlagsMovement");


			rightBuddy2 = rightBuddy3;
			rightBuddy2.rectTransform.DOAnchorPos(rightBuddy2DefaultPosition,movementSpeed).SetId("FlagsMovement");
			rightBuddy2.rectTransform.DOScale(secondaryScale2,movementSpeed).SetId("FlagsMovement");

			rightBuddy3 = farRightBuddy;
			rightBuddy3.rectTransform.DOAnchorPos(rightBuddy3DefaultPosition,movementSpeed).SetId("FlagsMovement");
			rightBuddy3.rectTransform.DOScale(secondaryScale3,movementSpeed).SetId("FlagsMovement");

			Buddy leftTempBuddy2 = leftBuddy2;
			Buddy leftTempBuddy3 = leftBuddy3;
			Buddy leftTempBuddy4 = farLeftBuddy;

			leftBuddy2 = leftTempBuddy;
			leftBuddy2.rectTransform.DOAnchorPos(leftBuddy2DefaultPosition,movementSpeed).SetId("FlagsMovement");
			leftBuddy2.rectTransform.DOScale(secondaryScale2,movementSpeed).SetId("FlagsMovement");

			leftBuddy3 = leftTempBuddy2;
			leftBuddy3.rectTransform.DOAnchorPos(leftBuddy3DefaultPosition,movementSpeed).SetId("FlagsMovement");
			leftBuddy3.rectTransform.DOScale(1,movementSpeed).SetId("FlagsMovement");

			farLeftBuddy = leftTempBuddy3;
			farLeftBuddy.rectTransform.DOAnchorPos(farLeftBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			farLeftBuddy.rectTransform.DOScale(0,movementSpeed).SetId("FlagsMovement");

			farRightBuddy = leftTempBuddy4;
			farRightBuddy.rectTransform.DOAnchorPos(farRightBuddyDefaultPosition,0).SetId("FlagsMovement");
			farRightBuddy.rectTransform.DOScale(0,0).SetId("FlagsMovement");

			//since we need to put a new far right buddy adding the index, we need to set our index to
			//the far left buddy, so we add the 9 positions to our index
			if (!movingRight)
				{
				for (int i=0;i<8;i++)
					updateIndex("right");
				movingRight = true;
				}

			updateIndex("right");
			farRightBuddy.update(Languages.instance.languages[index]);

//			if (!forceImmediate)
//				GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.itemHorizontalToggle);

			Languages.instance.switchToLanguage(leftBuddy3.language);

			updateHierarchys("right");
			}


		private void moveLeft(bool forceImmediate = false)
			{
			float movementSpeed = (forceImmediate) ? 0 : transitionSpeed;

			DOTween.Kill("FlagsMovement");

			Buddy rightTempBuddy = rightBuddy;

			rightBuddy = centerBuddy;
			rightBuddy.rectTransform.DOAnchorPos(rightBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			rightBuddy.rectTransform.DOScale(secondaryScale,movementSpeed).SetId("FlagsMovement");

			centerBuddy = leftBuddy;
			centerBuddy.rectTransform.DOAnchorPos(centerBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			centerBuddy.rectTransform.DOScale(secondaryScale2,movementSpeed).SetId("FlagsMovement");

			leftBuddy = leftBuddy2;
			leftBuddy.rectTransform.DOAnchorPos(leftBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			leftBuddy.rectTransform.DOScale(secondaryScale,movementSpeed).SetId("FlagsMovement");


			leftBuddy2 = leftBuddy3;
			leftBuddy2.rectTransform.DOAnchorPos(leftBuddy2DefaultPosition,movementSpeed).SetId("FlagsMovement");
			leftBuddy2.rectTransform.DOScale(secondaryScale2,movementSpeed).SetId("FlagsMovement");

			leftBuddy3 = farLeftBuddy;
			leftBuddy3.rectTransform.DOAnchorPos(leftBuddy3DefaultPosition,movementSpeed).SetId("FlagsMovement");
			leftBuddy3.rectTransform.DOScale(1,movementSpeed).SetId("FlagsMovement");

			Buddy rightTempBuddy2 = rightBuddy2;
			Buddy rightTempBuddy3 = rightBuddy3;
			Buddy rightTempBuddy4 = farRightBuddy;

			rightBuddy2 = rightTempBuddy;
			rightBuddy2.rectTransform.DOAnchorPos(rightBuddy2DefaultPosition,movementSpeed).SetId("FlagsMovement");
			rightBuddy2.rectTransform.DOScale(secondaryScale2,movementSpeed).SetId("FlagsMovement");

			rightBuddy3 = rightTempBuddy2;
			rightBuddy3.rectTransform.DOAnchorPos(rightBuddy3DefaultPosition,movementSpeed).SetId("FlagsMovement");
			rightBuddy3.rectTransform.DOScale(secondaryScale2,movementSpeed).SetId("FlagsMovement");

			farRightBuddy = rightTempBuddy3;
			farRightBuddy.rectTransform.DOAnchorPos(farRightBuddyDefaultPosition,movementSpeed).SetId("FlagsMovement");
			farRightBuddy.rectTransform.DOScale(0,movementSpeed).SetId("FlagsMovement");

			farLeftBuddy = rightTempBuddy4;
			farLeftBuddy.rectTransform.DOAnchorPos(farLeftBuddyDefaultPosition,0).SetId("FlagsMovement");
			farLeftBuddy.rectTransform.DOScale(0,0).SetId("FlagsMovement");

			//since we need to put a new far right buddy adding the index, we need to set our index to
			//the far left buddy, so we add the 9 positions to our index
			if (movingRight)
				{
				for (int i=0;i<8;i++)
					updateIndex("left");
				movingRight = false;
				}

			updateIndex("left");
			farLeftBuddy.update(Languages.instance.languages[index]);

//			if (!forceImmediate)
//				GameAudioManager.instance.playSfx(GameAudioManager.instance.uiSounds.itemHorizontalToggle);

			Languages.instance.switchToLanguage(leftBuddy3.language);

			updateHierarchys("left");
			}

		#endregion



		#region EVENTS HANDLER STUFF

		private void onDirectionChange(InputDirection direction)
			{
			switch (direction)
				{
				case InputDirection.Left:
					moveLeft();
				break;
				case InputDirection.Right:
					moveRight();
				break;
				}
			}

		#endregion
		}

	}