using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Warner;
using System;
using UnityEditor;

public class LevelEditor : MonoBehaviour
    {

	#region MEMBER FIELDS
	public Transform container;
	public GameObject platformPrefab;
	public GameObject enemySpawnerPrefab;
	public GameObject blockPrefab;
	public GameObject doorPrefab;

	public Vector2 offset = new Vector2(20.48f, 7f);

	private Config config;
	private int level = 0;
	private int roomCount = 0;
	private int roomIndex = 0;
	private int maxIterableRooms;
	private RoomData currentRoom;
	private float roomVerticalPosition = 0;
	private float roomVerticalSeparation = 5f;
	private List<RoomData> rooms = new List<RoomData>();
	private float startingYPosition;
	private RoomData initialRoom;
	private RoomData lastRoom;
	private Vector2 roomSize = new Vector2(0f, 15f);
	private float nextRoomAt;

	[Serializable]
	public enum ObjectType { Platform, Spawner, Block, Door };

	[Serializable]
	public enum EnemyType { AddEnemy, Clone, Sentry, Gunner};

	[Serializable]
	public class ObjectData
		{
		[SerializeField]
		public VectorData position = new VectorData(Vector2.zero);
		public VectorData scale = new VectorData(Vector2.one);
		public ObjectType type;
		}

	[Serializable]
	public class PlatformData : ObjectData
		{

		}

	[Serializable]
	public class BlockData : ObjectData
		{

		}


	[Serializable]
	public class DoorData : ObjectData
		{

		}

	[Serializable]
	public class EnemySpawnerData : ObjectData
		{
		public int count = 1;
		public List<SpawnableEnemyType> enemies = new List<SpawnableEnemyType>();
		}

	[Serializable]
	public class SpawnableEnemyType
		{
		public EnemyType enemy;
		public int chance = 100;
		}

	[Serializable]
	public class VectorData
		{
		public float x;
		public float y;

		public VectorData(Vector2 vector)
			{
			this.x = vector.x;
			this.y = vector.y;
			}

		public Vector2 getVector()
			{
			return new Vector2(this.x, this.y);
			}
		}

	[Serializable]
	public class RoomData
		{
		public RoomType type;
		public List<PlatformData> platforms = new List<PlatformData>();
		public List<BlockData> blocks = new List<BlockData>();
		public List<DoorData> doors = new List<DoorData>();
		public List<EnemySpawnerData> enemySpawners = new List<EnemySpawnerData>();
		}


	[Serializable]
	public enum RoomType {Regular, Initial, Last };


	[Serializable]
	public class LevelData
		{
		public int maxIterableRooms = 25;
		public List<RoomData> rooms = new List<RoomData>();
		}


	[Serializable]
	public class Config
		{
		public List<LevelData> levels = new List<LevelData>();
		}

	private const string dataFile = "LevelEditorData";	

    #endregion



    #region INIT

    private void Awake()
        {
        loadConfig();
        }

    private void loadConfig()
        {
        TextAsset configAsset = Misc.loadConfigAsset(dataFile);
        config = Misc.loadConfigFile<Config>(configAsset);
        }

	private void OnEnable()
		{
		//look for the initial and last room
		for (int i = config.levels[level].rooms.Count-1; i>=0; i--)
			{
			switch (config.levels[level].rooms[i].type)
				{
				case RoomType.Initial:
					initialRoom = config.levels[level].rooms[i];
				break;
				case RoomType.Last:
					lastRoom = config.levels[level].rooms[i];
				break;
				default: continue;
				}

			config.levels[level].rooms.RemoveAt(i);
			}

		maxIterableRooms = config.levels[level].maxIterableRooms;

		for (var i = 0; i < config.levels[level].rooms.Count; i++)
			rooms.Add(config.levels[level].rooms[i]);
		
		rooms.randomize();

		if (initialRoom != null)
			{
			Debug.Log("Adding initial room");
			rooms.Insert(0, initialRoom);
			maxIterableRooms++;
			}
		
		currentRoom = rooms[roomIndex];
		spawnRoom();
		}


	private void Start()
		{
		startingYPosition = CameraController.instance.cam.transform.position.y;
		nextRoomAt = startingYPosition + roomSize.y;
		}


	#endregion



	#region ROOM DATA

	private void spawnRoom()
		{
		roomVerticalPosition = (roomCount * roomSize.y);
		
		if (roomCount > 0)
			roomVerticalPosition += roomVerticalSeparation;

		spawnObjects<PlatformData>(currentRoom.platforms);
		spawnObjects<EnemySpawnerData>(currentRoom.enemySpawners);
		spawnObjects<BlockData>(currentRoom.blocks);
		spawnObjects<DoorData>(currentRoom.doors);		

		nextRoomAt += roomSize.y;
		roomCount++;
		}


	private void spawnObjects<T>(List<T> collection)
		{
		if (collection == null)
			return;

		Vector2 editorPos;
		Vector2 worldPos;

		for (int i = 0; i<collection.Count; i++)
			{
			editorPos = (collection[i] as ObjectData).position.getVector();
			editorPos.y = Mathf.Abs(editorPos.y-512)-(512*0.5f);

			worldPos = (editorPos*0.035f);
			worldPos.x -= offset.x;
			worldPos.y += offset.y+roomVerticalPosition;

			switch ((collection[i] as ObjectData).type)
				{
				case ObjectType.Platform:
					WorldPlatform platform = PoolManager.instantiate(platformPrefab, worldPos, container).GetComponent<WorldPlatform>();
					platform.scale = (collection[i] as ObjectData).scale.getVector();
				break;
				case ObjectType.Spawner:
					EnemySpawner enemySpawner = PoolManager.instantiate(enemySpawnerPrefab, worldPos, container).GetComponent<EnemySpawner>();
					enemySpawner.spawnCount = (collection[i] as EnemySpawnerData).count;
					enemySpawner.enemies = (collection[i] as EnemySpawnerData).enemies;
					enemySpawner.init();
				break;
				case ObjectType.Block:
					GameObject block = PoolManager.instantiate(blockPrefab, worldPos, container);
					block.transform.localScale = (collection[i] as ObjectData).scale.getVector();
				break;
				case ObjectType.Door:
					GameObject door = PoolManager.instantiate(doorPrefab, worldPos, container);
					door.transform.localScale = (collection[i] as ObjectData).scale.getVector();
				break;
				}
			}
		}

	#endregion



	#region UPDATE

	private void Update()
		{
		if (roomCount >= maxIterableRooms)//no more allowed rooms
			{
			if (lastRoom != null)
				{				
				currentRoom = lastRoom;
				spawnRoom();
				lastRoom = null;
				nextRoomAt += roomSize.y;
				}				

			return;
			}

		if (CameraController.instance.cam.transform.position.y > (nextRoomAt - 18f))
			{
			roomIndex++;

			if (roomIndex>=rooms.Count)//no more rooms, iterate again
				{
				//if there was an initial room remove it
				if (initialRoom != null)
					{
					rooms.RemoveAt(0);
					initialRoom = null;
					}

				roomIndex = 0;
				rooms.randomize();
				}

			currentRoom = rooms[roomIndex];
			spawnRoom();
			}
		}

	#endregion

	}
