using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Warner.LevelEditor
	{
	public class LevelEditorWindow : EditorWindow
		{
		#region MEMBER FIELDS

		private Vector2 offset;
		private Vector2 drag;
		private int objectIndex;
		private Texture pointerTexture;
		private Texture platformTexture;
		private Texture enemySpawnerTexture;
		private Texture blockTexture;
		private Texture doorTexture;
		private Texture unknownTexture;		
		private bool texturesLoaded;
		private bool dragging;
		private global::LevelEditor.ObjectData draggedObject;
		private global::LevelEditor.ObjectData selectedObject;
		private global::LevelEditor.Config config;
		private string configPath;
		private int currentLevel = 0;
		private int currentRoom = 0;
		private Vector2 positionDifferenceOnSelect;
		private List<global::LevelEditor.PlatformData> platforms = new List<global::LevelEditor.PlatformData>();
		private List<global::LevelEditor.BlockData> blocks = new List<global::LevelEditor.BlockData>();
		private List<global::LevelEditor.DoorData> doors = new List<global::LevelEditor.DoorData>();
		private List<global::LevelEditor.EnemySpawnerData> enemySpawners = new List<global::LevelEditor.EnemySpawnerData>();
		private global::LevelEditor.EnemyType enemyType = global::LevelEditor.EnemyType.AddEnemy;

		private enum MouseStatus { None, Down, Hold, Up };		

		private static Vector2 cellSize = new Vector2(2f, 2f);
		private static Vector2 windowSize = new Vector2(920f, 520f);
		private static int platformSize = 200;
		private static int blockSize = 100;
		private static int doorSize = 100;
		private static int enemySpawnerSize = 75;

		private const string dataFile = "LevelEditorData";

		#endregion



		#region INIT

		[MenuItem("Tools/Level Editor")]
		private static void Init()
			{
			EditorWindow window = EditorWindow.GetWindow<LevelEditorWindow>();
			window.minSize = windowSize;
			window.maxSize = windowSize;
			window.Show();
			}


		private void OnEnable()
			{
			config = EditorMisc.loadConfig<global::LevelEditor.Config>(dataFile, ref configPath);
			selectedObject = null;
			draggedObject = null;
			enemyType = global::LevelEditor.EnemyType.AddEnemy;

			if (config.levels.Count == 0)
				{
				config.levels.Add(createEmptyLevel());
				}

			loadTextures();
			}




		private void loadTextures()
			{
			pointerTexture = Resources.Load<Texture>("pointer");
			platformTexture = Resources.Load<Texture>("platform");
			blockTexture = Resources.Load<Texture>("block");
			doorTexture = Resources.Load<Texture>("door");
			unknownTexture = Resources.Load<Texture>("question");
			enemySpawnerTexture = Resources.Load<Texture>("enemySpawner");			
			texturesLoaded = true;
			}


		private global::LevelEditor.RoomData createEmptyRoom()
			{
			global::LevelEditor.RoomData defaultRoom = new global::LevelEditor.RoomData();
			return defaultRoom;
			}

		private global::LevelEditor.LevelData createEmptyLevel()
			{
			global::LevelEditor.LevelData defaultLevel = new global::LevelEditor.LevelData();
			defaultLevel.rooms.Add(createEmptyRoom());
			return defaultLevel;
			}

		#endregion



		#region GUI

		void OnGUI()
			{			
			if (!texturesLoaded)
				return;

			mouseEvents();
			drawMainUI();
			drawObjects();
			}


		private void drawMainUI()
			{
			drawGrid(100, 0.4f, Color.gray);
			//drawGrid(20, 0.2f, Color.gray);
			EditorGUI.DrawRect(new Rect(0, 0, 178, windowSize.y), Color.grey);
			List<GUIContent> paletteIcons = new List<GUIContent>();
			paletteIcons.Add(new GUIContent(pointerTexture));
			paletteIcons.Add(new GUIContent(platformTexture));
			paletteIcons.Add(new GUIContent(enemySpawnerTexture));
			paletteIcons.Add(new GUIContent(blockTexture));
			paletteIcons.Add(new GUIContent(doorTexture));
			paletteIcons.Add(new GUIContent(unknownTexture));
			objectIndex = GUI.SelectionGrid(new Rect(2, windowSize.y - 144, 175, 100), objectIndex, paletteIcons.ToArray(), 3);

			drawSelectors();
			drawSelectedObjectPanel();

			if (GUI.Button(new Rect(3, windowSize.y - 40, 172, 36), "Guardar"))
				{
				save();
				}
			}


		private void drawSelectors()
			{
			//LEVELS
			List<string> levelOptions = new List<string>();
			for (int i = 0; i < config.levels.Count; i++)
				levelOptions.Add("Level-"+(i+1));

			currentLevel = EditorGUI.Popup(new Rect(3, 3, 142, 20), currentLevel, levelOptions.ToArray());
			if (GUI.Button(new Rect(148, 3, 27, 18), "+"))
				{
				config.levels.Add(createEmptyLevel());
				save();
				}

			//ROOMS
			List<string> roomOptions = new List<string>();
			for (int i = 0; i < config.levels[currentLevel].rooms.Count; i++)
				roomOptions.Add("Room-" + (i + 1));

			if (currentRoom >= config.levels[currentLevel].rooms.Count)
				currentRoom = 0;

			currentRoom = EditorGUI.Popup(new Rect(3, 25, 142, 20), currentRoom, roomOptions.ToArray());
			if (GUI.Button(new Rect(148, 25, 27, 18), "+"))
				{
				config.levels[currentLevel].rooms.Add(createEmptyRoom());
				save();
				}


			platforms = config.levels[currentLevel].rooms[currentRoom].platforms;
			blocks = config.levels[currentLevel].rooms[currentRoom].blocks;
			doors = config.levels[currentLevel].rooms[currentRoom].doors;

			

			if (blocks == null)//cause of update
				{
				blocks = new List<global::LevelEditor.BlockData>();
				save();
				}

			if (doors == null)
				{
				doors = new List<global::LevelEditor.DoorData>();
				save();
				}

			enemySpawners = config.levels[currentLevel].rooms[currentRoom].enemySpawners;


			config.levels[currentLevel].rooms[currentRoom].type = (global::LevelEditor.RoomType) EditorGUI.EnumPopup(new Rect(3, 55, 172, 18), config.levels[currentLevel].rooms[currentRoom].type);


			//MAX ROOMS
			GUI.Label(new Rect(3, 340, 160, 20), "MaxIterableRooms:");
			string maxIteratableRooms = GUI.TextField(new Rect(125, 340, 50, 18), config.levels[currentLevel].maxIterableRooms+"");
			config.levels[currentLevel].maxIterableRooms = int.Parse(maxIteratableRooms);
			}


		private void drawGrid(float gridSpacing, float gridOpacity, Color gridColor)
			{
			int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
			int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			offset += drag * 0.5f;
			Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

			for (int i = 0; i < widthDivs; i++)
				{
				Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
				}

			for (int j = 0; j < heightDivs; j++)
				{
				Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
				}
			}


		private void drawObjects()
			{
			GUI.color = Color.clear;
			drawObjectsType(platforms, platformSize, platformTexture);
			drawObjectsType(blocks, blockSize, blockTexture);
			drawObjectsType(doors, doorSize, doorTexture);
			drawObjectsType(enemySpawners, enemySpawnerSize, enemySpawnerTexture);
			}


		private void drawObjectsType<T>(List<T> collection, int size, Texture texture)
			{
			global::LevelEditor.ObjectData obj;
			Vector2 position;
			Vector2 finalSize;
			Vector2 centerPivotOffset;
			for (int i = 0; i < collection.Count; i++)
				{
				obj = collection[i] as global::LevelEditor.ObjectData;
				position = obj.position.getVector();
				finalSize = obj.scale.getVector() * size;

				if (dragging && obj == draggedObject)
					{
					position = (Event.current.mousePosition-positionDifferenceOnSelect).snap(5f);
					Repaint();					
					}

				//we are substracting offset to pivot at center
				centerPivotOffset = (new Vector2(finalSize.x * 0.5f, finalSize.y * 0.5f));
				position -= centerPivotOffset;
				EditorGUI.DrawTextureTransparent(new Rect(position.x, position.y, finalSize.x, finalSize.y), texture);
				}
			}


		private void drawSelectedObjectPanel()
			{
			if (selectedObject == null)
				return;

			float y = 85;
			GUI.Label(new Rect(3, y, 172, 20), selectedObject.type.ToString().ToUpper());

			y += 30;
			GUI.Label(new Rect(3, y, 50, 20), "ScaleX:");
			selectedObject.scale.x = EditorGUI.FloatField(new Rect(53, y, 50, 20), selectedObject.scale.x);

			y += 25;
			GUI.Label(new Rect(3, y, 50, 20), "ScaleY:");
			selectedObject.scale.y = EditorGUI.FloatField(new Rect(53, y, 50, 20), selectedObject.scale.y);

			drawEnemySpawnerInspector(y);			
			}


		private void drawEnemySpawnerInspector(float y)
			{
			if (selectedObject.type != global::LevelEditor.ObjectType.Spawner)
				return;

			y += 30;
			GUI.Label(new Rect(3, y, 50, 20), "Count:");
			int.TryParse(EditorGUI.TextField(new Rect(53, y, 50, 20), (selectedObject as global::LevelEditor.EnemySpawnerData).count.ToString()), out (selectedObject as global::LevelEditor.EnemySpawnerData).count);


			y += 30;
			int enemyIndex = EditorGUI.Popup(new Rect(3, y, 142, 20), (int)enemyType, enemyType.valuesList().ToArray());

			enemyType = (global::LevelEditor.EnemyType)enemyIndex;

			if (GUI.Button(new Rect(148, y, 27, 18), "+"))
				{
				if (enemyIndex == 0)
					return;

				global::LevelEditor.SpawnableEnemyType enemy = new global::LevelEditor.SpawnableEnemyType();
				enemy.enemy = enemyType;

				(selectedObject as global::LevelEditor.EnemySpawnerData).enemies.Add(enemy);
				enemyType = global::LevelEditor.EnemyType.AddEnemy;
				}

			y += 25;	
			EditorGUI.DrawRect(new Rect(3, y, 172, 105), new Color(0.34f, 0.34f, 0.34f));

			y -= 39;
			List<global::LevelEditor.SpawnableEnemyType> enemies = (selectedObject as global::LevelEditor.EnemySpawnerData).enemies;
			for (var i = 0; i < enemies.Count; i++)
				{
				GUI.Label(new Rect(6, y, 100, 100), enemies[i].enemy.ToString());
				int.TryParse(GUI.TextField(new Rect(105, y+42, 35, 18), enemies[i].chance.ToString()), out enemies[i].chance);

				if (GUI.Button(new Rect(144, y+42, 27, 18), "-"))
					{
					enemies.RemoveAt(i);
					}

				y += 20;
				}

			}

		#endregion


		#region MOUSE EVENTS

		private void mouseEvents()
			{
			if (Event.current.type == EventType.KeyDown)
				{ 
				if (Event.current.keyCode==KeyCode.Delete)
					deleteSelectedObject();
				}

			if (Event.current.button == 0)
				{
				if (Event.current.type == EventType.MouseDown)
					{
					switch (objectIndex)
						{
						case 0:
							checkIfWeSelectObject();
						break;
						case 1:
							spawnObject(ref platforms, global::LevelEditor.ObjectType.Platform, platformSize);
						break;
						case 2:
							spawnObject(ref enemySpawners, global::LevelEditor.ObjectType.Spawner, enemySpawnerSize);
						break;
						case 3:
							spawnObject(ref blocks, global::LevelEditor.ObjectType.Block, blockSize);
						break;
						case 4:
							spawnObject(ref doors, global::LevelEditor.ObjectType.Door, doorSize);
						break;
						}
					}

				if (Event.current.type == EventType.MouseDrag)
					dragging = true;

				if (Event.current.type == EventType.MouseUp)
					{
					dragging = false;
					releaseDraggedObject();
					}
				}

			}


		private int getObjectPosition<T>(ref List<T> collection, global::LevelEditor.ObjectData toSearch)
			{
			for (int i = 0; i < collection.Count; i++)
				if ((collection[i] as global::LevelEditor.ObjectData) == toSearch)
					return i;

			return -1;
			}


		private void checkIfWeSelectObject()
			{
			global::LevelEditor.ObjectData obj = isObjectInMousePosition(ref enemySpawners, enemySpawnerSize);

			if (obj==null)
				obj = isObjectInMousePosition(ref platforms, platformSize);

			if (obj == null)
				obj = isObjectInMousePosition(ref blocks, blockSize);

			if (obj == null)
				obj = isObjectInMousePosition(ref doors, doorSize);

			if (obj!=null)
				{
				positionDifferenceOnSelect = Event.current.mousePosition-obj.position.getVector();
				draggedObject = obj;
				selectedObject = obj;
				Repaint();
				}
			}


		private void releaseDraggedObject()
			{
			if (draggedObject == null)
				return;
				
			int index = -1;
			switch (draggedObject.type)
				{
				case global::LevelEditor.ObjectType.Platform:
					index = getObjectPosition(ref platforms, draggedObject);

					if (index != -1)
						{
						platforms[index].position = new global::LevelEditor.VectorData(Event.current.mousePosition- positionDifferenceOnSelect);
						}
				break;
				case global::LevelEditor.ObjectType.Spawner:
					index = getObjectPosition(ref enemySpawners, draggedObject);
					if (index != -1)
						{
						enemySpawners[index].position = new global::LevelEditor.VectorData(Event.current.mousePosition- positionDifferenceOnSelect);
						}
				break;
				case global::LevelEditor.ObjectType.Block:
					index = getObjectPosition(ref blocks, draggedObject);

					if (index != -1)
						{
						blocks[index].position = new global::LevelEditor.VectorData((Event.current.mousePosition - positionDifferenceOnSelect).snap(5f));
						}
				break;
				case global::LevelEditor.ObjectType.Door:
					index = getObjectPosition(ref doors, draggedObject);

					if (index != -1)
						{
						doors[index].position = new global::LevelEditor.VectorData((Event.current.mousePosition - positionDifferenceOnSelect).snap(5f));
						}
				break;
				}

			draggedObject = null;
			}


		private void deleteSelectedObject()
			{
			if (draggedObject == null)
				return;

			int index = -1;
			switch (draggedObject.type)
				{
				case global::LevelEditor.ObjectType.Platform:
					index = getObjectPosition(ref platforms, draggedObject);

					if (index != -1)
						platforms.RemoveAt(index);
					break;
				case global::LevelEditor.ObjectType.Spawner:
					index = getObjectPosition(ref enemySpawners, draggedObject);
					if (index != -1)
						enemySpawners.RemoveAt(index);
					break;
				case global::LevelEditor.ObjectType.Block:
					index = getObjectPosition(ref blocks, draggedObject);

					if (index != -1)
						blocks.RemoveAt(index);
				break;
				case global::LevelEditor.ObjectType.Door:
					index = getObjectPosition(ref doors, draggedObject);

					if (index != -1)
						doors.RemoveAt(index);
				break;
				}

			draggedObject = null;
			Repaint();
			}


		private T isObjectInMousePosition<T>(ref List<T> collection, int size) where T : new()
			{
			Vector2 targetPosition = Event.current.mousePosition;
			Rect positionRect;
			Vector2 finalSize;
			Vector2 objectPosition;
			Vector2 centerPivotOffset;

			//check which object we are selecting			
			for (int i = 0; i < collection.Count; i++)
				{
				finalSize = (collection[i] as global::LevelEditor.ObjectData).scale.getVector() * size;

				//substract the offset to pivot at center
				centerPivotOffset = (new Vector2(finalSize.x * 0.5f, finalSize.y * 0.5f));
				objectPosition = (collection[i] as global::LevelEditor.ObjectData).position.getVector();

				positionRect = new Rect(objectPosition-centerPivotOffset, finalSize);
				if (positionRect.Contains(targetPosition))
					{
					return collection[i];
					}
				}

			return default(T);
			}


		private void spawnObject<T>(ref List<T> collection, global::LevelEditor.ObjectType type, int size) where T : new()
			{
			Vector2 targetPosition = Event.current.mousePosition;

			if (targetPosition.x < 210 || isObjectInMousePosition(ref collection, size)!=null)
				return;

			T obj = new T();

			//we are substracting offset to pivot at center
			Vector2 centerPivotOffset = Vector2.one * size * 0.5f;

			(obj as global::LevelEditor.ObjectData).position = new global::LevelEditor.VectorData(targetPosition - new Vector2(size * 0.5f, size * 0.5f)+ centerPivotOffset);
			(obj as global::LevelEditor.ObjectData).type = type;
			collection.Add(obj);
			objectIndex = 0;
			Repaint();
			}


		#endregion



		#region SAVE

		private void save()
			{
			Debug.Log("Saving room "+currentRoom+" on level "+currentLevel);
			config.levels[currentLevel].rooms[currentRoom].platforms = platforms;			
			config.levels[currentLevel].rooms[currentRoom].enemySpawners = enemySpawners;
			config.levels[currentLevel].rooms[currentRoom].blocks = blocks;
			config.levels[currentLevel].rooms[currentRoom].doors = doors;
			Misc.saveConfigFile<global::LevelEditor.Config>(config, configPath);
			AssetDatabase.Refresh();
			}

		#endregion
		}

    }
