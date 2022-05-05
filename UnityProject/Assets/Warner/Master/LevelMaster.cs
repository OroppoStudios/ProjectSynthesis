using UnityEngine;
using System;
using Warner;
using System.Collections.Generic;
using DG.Tweening;

namespace Warner
    {
    public class LevelMaster: MonoBehaviour
        {
        #region MEMBER FIELDS

        public string levelName;
        public GameObject worldObject;
        public AudioClip ambienceBgm;
        public Transform stageItemsUI;
        public Layers layers;
        public SortingLayers sortingLayers;

        public delegate void EventsHandler();

        public event EventsHandler onLevelLoaded;
		public event EventsHandler onLevelReady;
        public event EventsHandler onLevelClosed;

        [NonSerialized] public bool loadingLevel;
        [NonSerialized] public bool levelLoaded;


        [Serializable]
        public struct WorldContainers
            {
            public Transform nonVisuals;
            public Transform relativePositioned;
            public Transform fixedPositioned;
            }


        [Serializable]
        public struct Layers
            {
            public LayerMask player;
            public LayerMask ground;
            public LayerMask enemiesGround;
            public LayerMask vaultingGround;
            public LayerMask enemies;
            }

        [Serializable]
        public struct SortingLayers
        	{
			public SortingLayer deadCharacters;
			public SortingLayer vfxBehindPlayersBehindEnemies;
			public SortingLayer playersBehindEnemies;
			public SortingLayer vfxInFrontPlayersBehindEnemies;
			public SortingLayer enemies;
            public SortingLayer platformsBehindCharacters;
            public SortingLayer vfxBehindPlayers;
			public SortingLayer players;
            public SortingLayer platformsInFrontCharacters;
            public SortingLayer vfxInFrontPlayers;            
        	}

        [Serializable]
        public struct PoolObjectData
            {
            public GameObject prefab;
            public Transform poolContainer;
            public int count;
            }

        public static LevelMaster instance;

        #endregion


        
        #region INIT STUFF

        private void Awake()
            {
            instance = this;    
            }


        private void OnEnable()
        	{
			DebugConsole.instance.onToggle += onDebugConsoleToggle;
        	}


        private void Start()
            {
            //gameObject.SetActive(false);
            }

        #endregion



        #region DESTROY

        private void OnDisable()
        	{
			DebugConsole.instance.onToggle -= onDebugConsoleToggle;
        	}

		#endregion



		#region EVENTS HANDLERS

		private void onDebugConsoleToggle(bool active)
			{
			TimeManager.instance.paused = active;		
			}

		#endregion



        #region LEVEL LOADING

		public Sprite loadSpriteBackground(string containerName, string spriteName)
            {
            return Resources.Load<Sprite>("Backgrounds/"+
            	LevelMaster.instance.levelName+"/Chunks/"+containerName+"/"+spriteName);        
            }


		private IEnumerator<float> preloadObjects()
			{
			//we preload all the prefabs we use just one time 
			//this doesnt have anything to do with the pool stuff
			//we dont instantiate a pool/count of objects because we dont know 
			//what  the target parent container will be
			//but having unity already instantiated a prefab does help a LOT
			//(the first instantiation is the expensivest one)
			//But later we have TODO an implementation to specify in the editor how many objects to create on each pool

			const float waitToDestroy = 2f;
			//TODO we need a way to actually know the correct holdPosition time to destroy
			//we need to let the vfx play and when they did a full run (so that their events trigger)
			//then destroy it, but they need to report back at us or us have a way
			//of knowing when they all autodestroyed so that we can yield til then and procceed with loading
			//of the level

			Vector2 position = CameraController.instance.cam.ScreenToWorldPoint(
				new Vector3(Screen.width*0.5f, Screen.height*0.5f, 0f));

			GameObject spawnedObject;

			for (int i = 0; i<Director.instance.characters.Length; i++)
				{
				for (int j = 0; j < Director.instance.characters[i].prePoolCount; j++)
					{
					spawnedObject = PoolManager.instantiate(Director.instance.characters[i].prefab, position);
					PoolManager.Destroy(spawnedObject, waitToDestroy);	
					}				
				}

			for (int i = 0; i<VfxManager.instance.prefabs.Length; i++)
				{
				spawnedObject = PoolManager.instantiate(VfxManager.instance.prefabs[i], position);
				PoolManager.Destroy(spawnedObject, waitToDestroy);
				}

			yield return Timing.waitForSeconds(waitToDestroy);		
			yield return 0;
            }


        public void loadLevel()
            {
            Timing.run(initLevel());
            }


        private IEnumerator <float> initLevel()
            {
			loadingLevel = true;
            LoadingScreen.instance.show();
            yield return Timing.waitForSeconds(LoadingScreen.fadeInDuration);

            worldObject.SetActive(true);

            LevelMaster.instance.spawnPlayer(new Vector2(0f, 2f));
            WorldTilingVertical.instance.init();	


            DOTween.Clear();
            GC.Collect();

            yield return Timing.waitForSeconds(1f);

			if (onLevelReady!=null)
				onLevelReady();

            LoadingScreen.instance.close();	
			

            loadingLevel = false;
            levelLoaded = true;

            if (onLevelLoaded!=null)
                onLevelLoaded();
            }


        #endregion



        #region PLAYERS STUFF

        public Character getSinglePlayerCharacter()
        	{
        	if (GameMaster.instance.players.Count==0)
				return null;

			return GameMaster.instance.players[0].character;
        	}

        public void spawnPlayer(Vector2 position)
			{
			GameMaster.Player player = new GameMaster.Player();
			player.character = Director.instance.spawnCharacter(0, position);

			CameraController.instance.init(player.character.movements);
			GameMaster.instance.players.Add(player);
			}


        public Vector2 getGroundSpawnPosition(GameObject prefab, float horizontalReference)
            {                   
            SpriteRenderer spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
            float colliderOffset = spriteRenderer.sprite.bounds.min.y*-0.5f;

			Vector2 spawnPoint = new Vector2(horizontalReference, 10f);

            RaycastHit2D hit = Physics2D.Raycast(spawnPoint, Vector2.down, Mathf.Infinity, 
                layers.ground);

            if (hit.collider!=null)
                return hit.point+new Vector2(0, colliderOffset);
                else
                {
                Debug.Log("Could not find a suitable spawn position, using default");
                return spawnPoint;
                }
            }

        #endregion



        #region LEVEL CLOSE

        public void closeLevel()
            {
//            AudioManager.instance.stopLoopedAudio(AudioManager.instance.ambienceAudioSource, 0);
            LevelMaster.instance.gameObject.SetActive(false);
            Timing.killAll();
            PoolManager.clear();
            WorldTiling.instance.clear();
            WeatherManager.instance.clear();
            AudioManager.instance.clear();
            TimeManager.instance.paused = false;
            levelLoaded = false;
            Invoke("levelCleared",0.1f);
            }


        private void levelCleared()
            {
            if (onLevelClosed!=null)
                onLevelClosed();
            }
            
        #endregion
        }
    }