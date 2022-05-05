using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Warner
	{

	public class TextRenderer: MonoBehaviour
		{
		#region MEMBER FIELDS

		public Size size;
		public string defaultText = "Text here";
		public Color color;
		public float scale = 1f;
		public Vector2 offset;
		public GameObject charPrefab;
		public List<Sprite> spritesRegular = new List<Sprite>();
		public List<Sprite> spritesSmall = new List<Sprite>();

		[NonSerialized] public float characterSpacing = 0.005f;
		[NonSerialized] public float spaceSeparation = 0.1f;

		public enum Size {Regular,Small}

		private Transform container;

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			container = new GameObject("TextRendererContainer").transform;
			container.SetParent(transform);
			}

		private void Start()
			{

			}

		private void OnEnable()
			{
			clear();
			}


		public void clear()
			{
			if (container.childCount>0)
				{
				Transform pool = container.GetChild(0);
				for (int i = 0; i<pool.childCount; i++)
					PoolManager.Destroy(pool.GetChild(i).gameObject);
				}
			}

		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{

			}

		#endregion


	
		#region RENDERING THE TEXT STUFF

		public void renderText(string text)
			{
			clear();

			transform.localScale = Vector3.one;

			Vector2 nextCharacterPosition = transform.position.to2()+offset;
			SpriteRenderer spriteRenderer = null;
			bool found;
			float totalWidth = 0;

			List<Sprite> sprites = null;

			switch (size)
				{
				case Size.Regular:
				sprites = spritesRegular;
				break;
				case Size.Small:
				sprites = spritesSmall;
				break;
				}

			for (int i = 0; i<text.Length; i++)
				{
				found = false;
				for (int j = 0; j<sprites.Count; j++)
					{
					if (sprites[j].name.Equals(text[i].ToString()))
						{ 
						nextCharacterPosition.x += sprites[j].bounds.extents.x*scale;
						GameObject characterObject = PoolManager.instantiate(charPrefab, nextCharacterPosition, container, gameObject.name+"TextCharacter"+gameObject.GetInstanceID());
						characterObject.transform.localScale = new Vector3(scale, scale, characterObject.transform.localScale.z);
						spriteRenderer = characterObject.GetComponent<SpriteRenderer>();
						spriteRenderer.color = color;
						spriteRenderer.sprite = sprites[j];
						nextCharacterPosition.x += (sprites[j].bounds.extents.x*scale)+(characterSpacing*scale);//add again the current sprite extent so that the next one start calculating in the end of this char
						totalWidth += sprites[j].bounds.extents.x*scale+(sprites[j].bounds.extents.x*scale)+(characterSpacing*scale);
						found = true;
						break;
						}
					}

				if (!found)
					{
					GameObject characterObject = PoolManager.instantiate(charPrefab, nextCharacterPosition, container, gameObject.name+"TextCharacter"+gameObject.GetInstanceID());
					characterObject.transform.localScale = new Vector2(scale, scale);
					spriteRenderer = characterObject.GetComponent<SpriteRenderer>();
					spriteRenderer.sprite = null;
					nextCharacterPosition.x += spaceSeparation*scale;
					totalWidth += spaceSeparation*scale;
					}
				}

			//center the letters according to the width
			float minus = totalWidth/2;

			if (container.childCount>0)
				{
				Transform child = null;
				Vector2 position;

				Transform firstChild = container.GetChild(0);
				for (int i = 0; i<firstChild.childCount; i++)
					{
					child = firstChild.GetChild(i);
					if (child.gameObject.activeSelf)
						{
						position = child.transform.position;
						position.x -= minus;
						child.transform.position = position;
						}
					}
				}
			}

		#endregion
		}

	}