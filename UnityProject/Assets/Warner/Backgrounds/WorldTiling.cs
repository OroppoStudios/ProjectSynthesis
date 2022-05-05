using System.Collections.Generic;
using UnityEngine;
using System;

namespace Warner
	{
	public class WorldTiling : MonoBehaviour
		{
		#region MEMBER FIELDS

        public Transform layersContainer;
		public LayerData[] layers;

		public delegate void EventsHandler();
		public event EventsHandler onWorldReady;

		[Serializable]
		public class LayerData
			{
			public string name;
			public bool enabled = true;
            public SortingLayer sortingLayer;
            public int sortingOrder;
            public Vector2 position;
            public float parallax;
            public bool randomize;
            public int chunksCount;
            public bool hasColliders;
            public float collidersOffset;
            public bool reverseScale;
            [Range (0f, 1f)] public float blur;
            [Range (0,32)] public int extrude;
            public AutoMove.Side movingSide;
            public float movingSpeed;
            [NonSerialized] public Transform transform;
			[NonSerialized] public Sprite[] sprites;
			[NonSerialized] public Sprite[] colliderSprites;
			[NonSerialized] public int currentChunkIndex;
			[NonSerialized] public int[] worldChunkIndexes;
			[NonSerialized] public string parentName;
			[NonSerialized] public Transform mainBuddy;
			[NonSerialized] public Material material;
			[NonSerialized] public SpriteRenderer mainBuddySpriteRenderer;
			[NonSerialized] public float spriteWidth;
			[NonSerialized] public Buddy rightBuddy;
			[NonSerialized] public Buddy leftBuddy;
			[NonSerialized] public Buddy centerBuddy;
			[NonSerialized] public Vector2[][] colliderPoints;
			[NonSerialized] public float _extrudedUnits = -1;

            private Vector2 originalPosition;

			public float extrudedUnits
				{
				get
					{
					if (_extrudedUnits==-1)
						_extrudedUnits = CameraController.instance.pixelsToUnits(extrude);

					return _extrudedUnits*0.1f;
					}
				}

            public void init(Vector2 thePosition)
                {
                originalPosition = thePosition;
                }

			public void resetPosition()
				{
                transform.position = new Vector3(originalPosition.x, originalPosition.y, transform.position.z);
				}

			public void clear()
				{
				resetPosition();

				if (enabled)
					{
					centerBuddy.destroy();
					leftBuddy.destroy();
					rightBuddy.destroy();
					}
				}
			}

		public enum BuddyType{Left, Center, Right}

		public class Buddy
			{
			public Transform transform;
			public SpriteRenderer spriteRenderer;
			public EdgeCollider2D collider;
			public bool isMain;
			
			public Buddy(Transform theTransform, BuddyType type, bool isMain = false)
				{
				this.isMain = isMain;
				this.transform = theTransform;
				this.type = type;
				spriteRenderer = theTransform.GetComponent<SpriteRenderer>();
				collider = theTransform.GetComponent<EdgeCollider2D>();
				}

			public BuddyType type
				{
				get
					{ 
					return _type;
					}
				set
					{
					_type = value;
					this.transform.name = value.ToString();
					}
				}

			public void destroy()
				{
				if (isMain)
					{
					this.transform.name = "Sprite";
					this.transform.localPosition = Vector3.zero;
					Destroy(collider);
					return;
					}

				Destroy(this.transform.gameObject);
				}

			private BuddyType _type;
			}

		public static WorldTiling instance;

		private bool ready;

		private const float OffsetX = 2f;

		#endregion



		#region INIT


		private void Awake()
			{
			instance = this;
			}


		public void init(bool createBuddies = true)
            {
            if (ready)
                return;

            if (layersContainer==null)
                {
                Debug.LogWarning("WorldTiling: Please assign a container");
                return;
                }

            createLayerContainers();
			loadSprites();
            addAutoMoveComponent();

			if (createBuddies)
				Timing.run(createBuddiesCoRoutine());
			}


        private void createLayerContainers()
			{
			GameObject layerGameObject;
			GameObject spriteGameObject;
			SpriteRenderer spriteRenderer;

			for (int i = 0; i<layers.Length; i++)
				{
				layers[i].transform = layersContainer.transform.Find(layers[i].name);

				if (layers[i].transform==null)
					{
					layerGameObject = new GameObject(layers[i].name);
					layerGameObject.transform.SetParent(layersContainer);
					layerGameObject.layer = layersContainer.gameObject.layer;
					layerGameObject.transform.localPosition = layers[i].position;
					layers[i].transform = layerGameObject.transform;

					spriteGameObject = new GameObject("Sprite");
					spriteGameObject.layer = layerGameObject.layer;
					spriteRenderer = spriteGameObject.AddComponent<SpriteRenderer>();

					layers[i].material = new Material(Shader.Find(
						"Warner/Sprites/Background"));
					layers[i].material.SetFloat("_BlurAmount", layers[i].blur*0.02f);

					spriteRenderer.sharedMaterial = layers[i].material;
                    spriteRenderer.sortingLayerName = layers[i].sortingLayer.name;
                    spriteRenderer.sortingOrder = layers[i].sortingOrder;
                    spriteGameObject.transform.SetParent(layers[i].transform, false);
                    layers[i].mainBuddy = spriteGameObject.transform;

                    if (layers[i].parallax!=0 && Parallax.instance!=null)
                        Parallax.instance.addLayer(layers[i].transform, layers[i].parallax);
                    }
                }
            }
          

        private void addAutoMoveComponent()
            {
            AutoMove autoMove;
            for (int i = 0; i<layers.Length; i++)
                {
                autoMove = layers[i].transform.gameObject.AddComponent<AutoMove>();
                autoMove.movingSide = layers[i].movingSide;
                autoMove.speed = layers[i].movingSpeed;
                }
            }         
		

		#endregion



		#region SPRITES

		private void loadSprites()
			{
			Sprite sprite;
			List<Sprite> spritesList = new List<Sprite>();
			List<Sprite> colliderSpritesList = new List<Sprite>();
			LayerData layerData;

			for (int i = 0; i<layers.Length; i++)
				{
				layerData = layers[i];

                layerData.init(layerData.transform.position);                    			

				spritesList.Clear();
				colliderSpritesList.Clear();

				for (int j = 1; j<10; j++)
					{
					sprite = LevelMaster.instance.loadSpriteBackground(layerData.transform.name, 
						j.ToString());						

					if (sprite==null)
						break;
					
					spritesList.Add(sprite);

					if (layerData.hasColliders)
						{
						sprite = LevelMaster.instance.loadSpriteBackground(layerData.transform.name, 
							j+"-Colliders");

						if (sprite!=null)
							colliderSpritesList.Add(sprite);
						}
					}
									
				layerData.sprites = spritesList.ToArray();
				layerData.colliderSprites = colliderSpritesList.ToArray();

				if (layerData.sprites.Length>0)
					{
					layerData.worldChunkIndexes = generateWorldChunkIndexes(layerData.sprites,
						layerData.chunksCount, layerData.randomize);

					layerData.currentChunkIndex = layerData.randomize ? layerData.chunksCount/2 
						: getNearestFirstFromSpawnIndexes(layerData.worldChunkIndexes, 
							layerData.chunksCount);
					}
				}
			}


		#endregion



		#region INDEXES

		private int[] generateWorldChunkIndexes(Sprite[] sprites, int toSpawnCount, bool random = false)
			{
			int linearIndex;

			int[] indexes = new int[toSpawnCount];
			linearIndex = 0;

			for (int j = 0; j<toSpawnCount; j++)
				{
				if (random)
					indexes[j] = sprites.getRandomIndex();
					else
					{
					indexes[j] = linearIndex;
					linearIndex++;
					if (linearIndex>sprites.Length-1)
						linearIndex = 0;
					}
				}

			return indexes;
			}


		public static int getNearestFirstFromSpawnIndexes(int[] spawnIndexes, int toSpawnCount)
			{
			if (spawnIndexes.Length==0)
				return 0;

			int toRightIndex = -1;
			int toRightDistance = 0;
			int toLeftIndex = -1;
			int toLeftDistance = 0;
			int middle = toSpawnCount/2;

			for (int i = middle; i<spawnIndexes.Length; i++)
				{
				if (spawnIndexes[i]==0)
					{
					toRightIndex = i;
					break;
					}
				toRightDistance++;
				}


			for (int i = middle; i>=0; i--)
				{
				if (spawnIndexes[i]==0)
					{
					toLeftIndex = i;
					break;
					}
				toLeftDistance++;
				}

			int index = 0;

			if (toLeftDistance<toRightIndex)
				index = toLeftIndex;
				else
				index = toRightIndex;

			//we substract 1 position so that 
			//the center chunk is the one with the first sprite instead of the left chunk
			return index-1;
			}

		#endregion



		#region BUDDIES		

		private IEnumerator<float> createBuddiesCoRoutine()
			{
			LayerData layerData;

			for (int i = 0; i<layers.Length; i++)
				{
				layerData = layers[i];

				if (!layerData.enabled)
					continue;
                    
				layerData.mainBuddySpriteRenderer = layerData.mainBuddy.GetComponent<SpriteRenderer>();
				layerData.mainBuddySpriteRenderer.sprite = layerData.sprites[0];//just assign here to get the sprite size
				layerData.spriteWidth = layerData.mainBuddySpriteRenderer.sprite.bounds.size.x
					-layerData.extrudedUnits;

				generateColliders(layerData);

				yield return 0;//we holdPosition one frame here so that unity updates the destroyed collider references when creating the duplicate buddys

				Vector3 leftPositionOffset = new Vector3(-layerData.spriteWidth, 
					layerData.mainBuddy.position.y, 
					layerData.mainBuddy.position.z);

				layerData.leftBuddy = new Buddy(layerData.mainBuddy, BuddyType.Left, true);		
				layerData.leftBuddy.transform.position = leftPositionOffset;

				Vector3 centerPositionOffset = new Vector3(layerData.leftBuddy.transform.position.x
					+layerData.spriteWidth, layerData.leftBuddy.transform.position.y, 
					layerData.leftBuddy.transform.position.z);

				layerData.centerBuddy = new Buddy(Instantiate(layerData.leftBuddy.transform, 
					centerPositionOffset, Quaternion.identity), BuddyType.Center);
				layerData.centerBuddy.transform.parent = layerData.transform;

				Vector3 rightPositionOffset = new Vector3(layerData.centerBuddy.transform.position.x
					+layerData.spriteWidth, layerData.centerBuddy.transform.position.y, layerData.centerBuddy.transform.position.z);

				layerData.rightBuddy = new Buddy(Instantiate(layerData.leftBuddy.transform, 
					rightPositionOffset, layerData.leftBuddy.transform.rotation), BuddyType.Right);
				layerData.rightBuddy.transform.parent = layerData.transform;

				updateSprites(layerData);
																							
				if (layerData.reverseScale)
					{
					if (UnityEngine.Random.value>0.5f)
						horizontalFlip(layerData.leftBuddy.transform);
					
					if (UnityEngine.Random.value>0.5f)
						horizontalFlip(layerData.rightBuddy.transform);
					}
				}

			CameraController.instance.onMove += onCameraMove;			

			ready = true;
			if (onWorldReady!=null)
				onWorldReady();
			}


		private void updateSprites(LayerData layerData)
			{
			updateSprite(layerData, layerData.leftBuddy);
			updateSprite(layerData, layerData.centerBuddy);
			updateSprite(layerData, layerData.rightBuddy);
			}


		private void updateSprite(LayerData layerData, Buddy buddy)
			{
			int chunkIndex = -1;

			switch (buddy.type)
				{
				case BuddyType.Left:
					chunkIndex = layerData.currentChunkIndex;
					break;
				case BuddyType.Center:
					chunkIndex = layerData.currentChunkIndex+1;
					break;
				case BuddyType.Right:
					chunkIndex = layerData.currentChunkIndex+2;
					break;
				}		

			if (chunkIndex>=layerData.worldChunkIndexes.Length || chunkIndex<0)
				{
				Debug.LogWarning("No more chunks on layer "+layerData.transform.name);
				return;
				}

			int spriteIndex = layerData.worldChunkIndexes[chunkIndex];

			buddy.spriteRenderer.sprite = layerData.sprites[spriteIndex];

			if (buddy.collider!=null)
				{
				buddy.collider.Reset();//Reset before assigning the new colliders, if not they would not update correctly on Mac
				buddy.collider.points = layerData.colliderPoints[spriteIndex];
				buddy.collider.edgeRadius = 0.1f;
				}

			if (layerData.reverseScale)
				if (UnityEngine.Random.value>0.5f)
					horizontalFlip(layerData.leftBuddy.transform);								
			}


		private void horizontalFlip(Transform theTransform)
			{
			theTransform.localScale = theTransform.localScale.multiplyX(-1);
			}


		#endregion



		#region COLLIDER GENERATION

		private void generateColliders(LayerData layerData)
			{
			if (layerData.colliderSprites.Length==0)
				return;

			List<List<Vector2>> colliderPointsList = new List<List<Vector2>>();
			layerData.mainBuddySpriteRenderer.gameObject.AddComponent<EdgeCollider2D>();

			for (int i=0;i<layerData.colliderSprites.Length;i++)
				colliderPointsList.Add(getColliderPoints(layerData.colliderSprites[i], layerData.collidersOffset));

			//now lets join the last collider point with the first collider point of the next chunk
			Vector2 newPoint;
			int nextIndex;
			for (int i=0; i<colliderPointsList.Count; i++)
				{
				nextIndex = (i<colliderPointsList.Count-1) ? i+1 : 0;

				newPoint = colliderPointsList[nextIndex][0];
				newPoint.x += layerData.spriteWidth;
				colliderPointsList[i].Add(newPoint);
				}

			
			List<Vector2[]> collidersFinalList = new List<Vector2[]>();
			for (int i=0; i<colliderPointsList.Count; i++)
				collidersFinalList.Add(colliderPointsList[i].ToArray());

			layerData.colliderPoints = collidersFinalList.ToArray();
			}


		private List<Vector2> getColliderPoints(Sprite sprite, float verticalOffset)
			{
			const int minPixelDistance = 80;

			Texture2D texture = sprite.texture;
			List<Vector2> points = new List<Vector2>();
			Vector2 coord = Vector2.zero;
			Vector2 lastStoredCoord = Vector2.zero;
			float unitsToPixels = sprite.rect.width/sprite.bounds.size.x*transform.localScale.x;
			Vector2 position;		

       		for(int x = 0; x<texture.width; x++)
	           	for(int y = 0; y<texture.height; y++)
	            	{
					if (texture.GetPixel(x,y).a!=0)
						{
						coord = new Vector2(x,y);

						if ((lastStoredCoord==Vector2.zero || Vector2.Distance(coord, lastStoredCoord)>minPixelDistance))
		         			{						
							lastStoredCoord = coord;
							position = transform.localPosition.to2()+(coord/unitsToPixels);
							position.x -= sprite.bounds.size.x*0.5f;
                            position.y += verticalOffset;
							points.Add(position);
		             		}
		             	}
	            	}
                
            return points;
			}

		#endregion



		#region DESTROY

		public void clear()
			{
			ready = false;
			for (int i = 0; i<layers.Length; i++)
				layers[i].clear();

			CameraController.instance.onMove -= onCameraMove;
			}

		#endregion



		#region UPDATE

		private void onCameraMove()
			{
			if (!ready)
				return;

			for (int i = 0; i<layers.Length; i++)
				{
				if (!layers[i].enabled)
					continue;

				//swap stuff
				float camHorizontalExtend = CameraController.instance.cam.orthographicSize*Screen.width/Screen.height;
				float edgeVisiblePositionRight = (layers[i].centerBuddy.transform.position.x+
					layers[i].spriteWidth+layers[i].spriteWidth/2)-camHorizontalExtend;
				float edgeVisiblePositionLeft = (layers[i].centerBuddy.transform.position.x
					-layers[i].spriteWidth-layers[i].spriteWidth/2)+camHorizontalExtend;

				if (CameraController.instance.cam.transform.position.x+OffsetX>=edgeVisiblePositionRight)
					{				
					Vector3 newPosition = new Vector3(layers[i].rightBuddy.transform.position.x+
						layers[i].spriteWidth, layers[i].mainBuddySpriteRenderer.transform.position.y, 
						layers[i].mainBuddySpriteRenderer.transform.position.z);
					layers[i].leftBuddy.transform.position = newPosition;		

					Buddy tempCenterBuddy = layers[i].centerBuddy;
					layers[i].centerBuddy = layers[i].rightBuddy;
					layers[i].rightBuddy = layers[i].leftBuddy;
					layers[i].leftBuddy = tempCenterBuddy;

					layers[i].centerBuddy.type = BuddyType.Center;
					layers[i].leftBuddy.type = BuddyType.Left;
					layers[i].rightBuddy.type = BuddyType.Right;

					layers[i].currentChunkIndex++;
					updateSprites(layers[i]);
					} 
					else
					{
					if (CameraController.instance.cam.transform.position.x-OffsetX<=edgeVisiblePositionLeft)
						{
						Vector3 newPosition = new Vector3(layers[i].leftBuddy.transform.position.x
							-layers[i].spriteWidth, layers[i].mainBuddySpriteRenderer.transform.position.y,
							layers[i].mainBuddySpriteRenderer.transform.position.z);
						layers[i].rightBuddy.transform.position = newPosition;

						Buddy tempCenterBuddy = layers[i].centerBuddy;
						layers[i].centerBuddy = layers[i].leftBuddy;
						layers[i].leftBuddy = layers[i].rightBuddy;
						layers[i].rightBuddy = tempCenterBuddy;

						layers[i].centerBuddy.type = BuddyType.Center;
						layers[i].leftBuddy.type = BuddyType.Left;
						layers[i].rightBuddy.type = BuddyType.Right;

						layers[i].currentChunkIndex--;
						updateSprites(layers[i]);
						}
					}		
				}			
			}

		#endregion
		}
	}