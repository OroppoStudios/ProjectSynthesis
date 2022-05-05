using UnityEngine;
using System;
using System.Collections.Generic;


namespace Warner
	{
	public class BackgroundAssetSpawner: MonoBehaviour
		{
		#region MEMBER FIELDS

        public Transform layersContainer;
		public List<LayerData> layers = new List<LayerData>();

		[Serializable]
		public struct AssetData
			{
			public int index;
			public Vector2 position;
			public bool isSpawned;
			}
            
        public enum AssetType {Static}

		[Serializable]
		public class LayerData
			{
			public string name;
			public bool enabled = true;
            public AssetType assetType;
            public SortingLayer sortingLayer;
            public int sortingOrder;
            public float parallax;
			public bool startOnScreen;
			public Vector2 verticalOffset;
            public bool alignWithGround;
			public Vector2 distances;
			public AutoMove.Side movingSide;
			public float movingSpeed;
			[Range (0f, 1f)] public float blur;
			[Range (1, 1000)] public int toSpawnCount = 1;           
            [NonSerialized] public Transform transform; 
			[NonSerialized] public AssetData[] assets;
			[NonSerialized] public List<Sprite> sprites = new List<Sprite>();
			[NonSerialized] public Material material;
			}

		public static BackgroundAssetSpawner instance;
		
		private GameObject assetPrefab;
		private bool ready;

		#endregion



		#region INIT


		private void Awake()
			{
			instance = this;	
			}

		private void OnEnable()
			{
			if (!ready)
				return;

            putOnScreenAssets();

			Timing.run(spawnCoRoutine());
			}


        private void putOnScreenAssets()
			{
			int startIndex;
			LayerData layerData;

			for (int i = 0; i<layers.Count; i++)
				{
				layerData = layers[i];

				if (!layerData.enabled || !layerData.startOnScreen)
					continue;

                startIndex = layerData.toSpawnCount/2;
                layerData.assets[startIndex].isSpawned = true;
                layerData.assets[startIndex].position.x = layerData.distances.getRandomFromX();
                spawnAsset(layerData, layerData.assets[startIndex], startIndex, true);
                }
            }


		public void init()
            {
			Timing.run(initRoutine());
            }


		public IEnumerator<float> initRoutine()
        	{
			createPrefab();
            createLayerContainers();
            yield return Timing.waitForRoutine(loadSpritesAndIndexes());
        	}


		private void createPrefab()
            {
			assetPrefab = new GameObject("BackgroundAssetPrefab");
			assetPrefab.gameObject.layer = layersContainer.gameObject.layer;
			assetPrefab.SetActive(false);
			assetPrefab.AddComponent<BackgroundAsset>();

			PoolObject poolObject = assetPrefab.AddComponent<PoolObject>();
            poolObject.offScreenDestroy = true;
            poolObject.offScreenOffsets = new Vector2(50, 9999);
            }


        private void createLayerContainers()
            {
            GameObject layerGameObject;

            for (int i = 0; i<layers.Count; i++)
                {
                layers[i].transform = layersContainer.transform.Find(layers[i].name);
				layers[i].material = new Material(Shader.Find("Warner/Sprites/Background"));

                if (layers[i].transform==null)
                    {
                    layerGameObject = new GameObject(layers[i].name);
                    layerGameObject.transform.SetParent(layersContainer, false);
                    layerGameObject.layer = layersContainer.gameObject.layer;
                    layers[i].transform = layerGameObject.transform;					
                    }

                if (layers[i].parallax!=0 && Parallax.instance!=null)
                    Parallax.instance.addLayer(layers[i].transform, layers[i].parallax);
                }
            }


		public IEnumerator<float> loadSpritesAndIndexes()
			{
			Sprite sprite;
			LayerData layerData;

			for (int i = 0; i<layers.Count; i++)
				{
				layerData = layers[i];

				for (int j = 0; j<10; j++)
					{
					sprite = Resources.Load<Sprite>
					("Backgrounds/"+
                            LevelMaster.instance.levelName+"/"+layers[i].assetType.ToString()+"/"+layerData.transform.name+"/"+(j+1));

					if (sprite!=null)
						layerData.sprites.Add(sprite);
					}

				if (layerData.sprites.Count==0)
					{
					layerData.enabled = false;
					continue;
					}

				yield return Timing.waitForRoutine(generateAssetIndexes(layerData));
				}	

			ready = true;	
			}



		private IEnumerator<float> generateAssetIndexes(LayerData layerData)
			{
			int startIndex = layerData.toSpawnCount/2;

			AssetData[] assetDatas = new AssetData[layerData.toSpawnCount];
			AssetData assetData;
			int lastIndex = -1;

			for (int j = 0; j<layerData.toSpawnCount; j++)
				{
				assetData = new AssetData();
				assetData.index = UnityEngine.Random.Range(0, layerData.sprites.Count);

				if (layerData.sprites.Count>1)
					while (assetData.index==lastIndex)
						{
						assetData.index = UnityEngine.Random.Range(0, layerData.sprites.Count);
						yield return 0;
						}

                lastIndex = assetData.index;
				assetDatas[j] = assetData;
				}

			Vector2 lastPosition = Vector2.zero;

			for (int i = startIndex; i<layerData.toSpawnCount; i++)
				{
				lastPosition.x += layerData.distances.getRandom();
				lastPosition.y = layerData.verticalOffset.getRandom();
				assetDatas[i].position = lastPosition;
				}			

			lastPosition = Vector2.zero;

			for (int i = startIndex-1; i>=0; i--)
				{
				lastPosition.x -= layerData.distances.getRandom();
				lastPosition.y = layerData.verticalOffset.getRandom();
				assetDatas[i].position = lastPosition;
				}		

			layerData.assets = assetDatas;
			}



		#endregion



		#region DESTROY

		private void OnDisable()
			{
			ready = false;
			}

		#endregion



		#region SPAWN

		private IEnumerator<float> spawnCoRoutine()
			{
			while(true)
				{
				yield return Timing.waitForSeconds(0.1f);

				LayerData layerData;

				for (int i = 0; i<layers.Count; i++)
					{
					layerData = layers[i];

					if (!layerData.enabled)
						continue;

					tryToSpawnNextAsset(layerData);
					}
				}
			}


		private void tryToSpawnNextAsset(LayerData layerData)
			{
			const float verticalPadding = 10f;

            Vector2 spawnAreaSize = new Vector2(CameraController.instance.worldSize.x/2, 
                CameraController.instance.worldSize.y+verticalPadding);

			Rect rightSpawnArea = new Rect(CameraController.instance.worldBoundaries.xMax, CameraController.instance.worldBoundaries.y-verticalPadding, 
                spawnAreaSize.x, spawnAreaSize.y);

			Rect leftSpawnArea = new Rect(CameraController.instance.worldBoundaries.x-CameraController.instance.worldSize.x, CameraController.instance.worldBoundaries.y-verticalPadding, 
                spawnAreaSize.x, spawnAreaSize.y);

            bool spawnLeft;
            bool spawnRight;

			for (int i = 0; i<layerData.assets.Length; i++)
				{
				if (layerData.assets[i].isSpawned)
					continue;

                spawnLeft = leftSpawnArea.Contains(layerData.assets[i].position);
                spawnRight = rightSpawnArea.Contains(layerData.assets[i].position);

                if (spawnLeft || spawnRight)
					{               
					spawnAsset(layerData, layerData.assets[i], i, spawnRight);
                    layerData.assets[i].isSpawned = true;
					}
				}
			}


		private void spawnAsset(LayerData layerData, AssetData assetData, int assetIndex, bool isRightSide)
			{
			Vector2 position = assetData.position;

			if (layerData.alignWithGround)
				{
				RaycastHit2D hit;
				Vector2 rayStartPoint = new Vector2(position.x,
					                                    CameraController.instance.worldBoundaries.yMin);

				int c = 0;
				while (true)
					{
					hit = Physics2D.Raycast(rayStartPoint, Vector2.up, Mathf.Infinity, 
						LevelMaster.instance.layers.ground);

					if (hit.collider!=null)
						break;
                                            
					rayStartPoint.x += isRightSide ? -0.1f : 0.1f;
					c++;

					//TODO change this so we dont block with a while loop
					}
                                    
				position.y = hit.point.y+assetData.position.y;
				}               

			GameObject asset = PoolManager.instantiate(assetPrefab, position, layerData.transform);

			if (asset!=null)
				{
				asset.gameObject.SetActive(true);
				asset.GetComponent<BackgroundAsset>().init(layerData, assetData.index, assetIndex);
				}
			}


		public void assetGotDeSpawned(LayerData layerData, int assetIndex)
			{
			for (int i = 0; i<layers.Count; i++)
				if (layers[i]==layerData)
					{
					layers[i].assets[assetIndex].isSpawned = false;
					return;
					}
			}


		#endregion
		}
	}