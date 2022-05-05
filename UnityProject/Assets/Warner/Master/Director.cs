using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Warner
	{
	public class Director: MonoBehaviour
		{
		#region MEMBER FIELDS

		public SpawnableCharacter[] characters;

		public static Director instance;

		[Serializable]
		public class SpawnableCharacter
			{
			public bool enabled;
			public CharacterType type;
			public GameObject prefab;
			public Vector2 healthRanges = new Vector2(100, 100);
			[Range (1, 10)] public int maxInstances;
			[Range(0, 10)] public int rightSpawnPointWeight;
			public Vector2 spawnPointRadius;
			public int prePoolCount = 1;
			public List<CharacterInstance> instances = new List<CharacterInstance>();
			}		

		public enum CharacterType {Player, GroundEnemy, AirEnemy, SideKick}

		[Serializable]
		public class CharacterInstance
			{
			public int id;
			public CharacterType type;
			public Character character;

			public CharacterInstance(CharacterType type, int id)
				{
				this.type = type;
				this.id = id;
				}
			}

		private IEnumerator<float> enemySpawnerRoutine;

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			}

		public void init()
			{
			enemySpawnerRoutine = characterSpawnerCoRoutine();
			Timing.run(enemySpawnerRoutine);
			}

		#endregion



		#region DESTROY STUFF

		private void OnDisable()
			{
			Timing.kill(enemySpawnerRoutine);

			for (int i = 0;i<characters.Length;i++)
				characters[i].instances.Clear();
			}

		#endregion



		#region SPAWN STUFF

		private IEnumerator <float> characterSpawnerCoRoutine()
			{
			while (true)
				{		
				yield return Timing.waitForSeconds(10f);

				for (int i = 0; i<characters.Length; i++)
					if (characters[i].enabled)
						spawnCharacter(i, CameraController.instance.worldBoundaries.xMax+2.5f);
				}							
			}	


		public Character spawnCharacter(int classIndex, float horizontalReference = 0f)
			{
			SpawnableCharacter spawnableCharacter = characters[classIndex];

			Vector2 position = LevelMaster.instance.getGroundSpawnPosition(
				spawnableCharacter.prefab, horizontalReference);

			return spawnCharacter(classIndex, position, getNewInstanceId());
			}


		public Character spawnCharacter(int classIndex, Vector2 position)
			{
			SpawnableCharacter spawnableCharacter = characters[classIndex];
			return spawnCharacter(classIndex, position, getNewInstanceId());
			}


		public Character spawnCharacter(int classIndex, Vector2 position, int instanceId)
			{
			//get the alive count for this character and check we are not spawning more than max instances
			int aliveCount = 0;
			for (int i = 0; i<characters[classIndex].instances.Count; i++)
				if (!characters[classIndex].instances[i].character.pendingDeath 
					&& !characters[classIndex].instances[i].character.dead)
					aliveCount++;

			if (aliveCount>=characters[classIndex].maxInstances)
				return null;				

			SpawnableCharacter spawnableCharacter = characters[classIndex];

			GameObject characterObject = PoolManager.instantiate(spawnableCharacter.prefab, position, null, 
				                             spawnableCharacter.prefab.name);

			Character character = characterObject.GetComponent<Character>();
			//character.health = ((int) spawnableCharacter.healthRanges.getRandom())+1;
			//character.initialHealth = character.health;
			character.type = spawnableCharacter.type;
			character.classIndex = classIndex;
			character.instanceId = instanceId;

			if (spawnableCharacter.type==CharacterType.Player || spawnableCharacter.type == CharacterType.SideKick)
				{
				character.setSpriteRenderersSortingLayer(
            		LevelMaster.instance.sortingLayers.players);
				character.attacks.targetLayers = LevelMaster.instance.layers.enemies;
				}
				else
				{
				character.setSpriteRenderersSortingLayer(
            		LevelMaster.instance.sortingLayers.enemies);
				character.control.controlMode = CharacterControl.ControlMode.AI;
				character.ai.targetsLayer = LevelMaster.instance.layers.player;				
				character.attacks.targetLayers = LevelMaster.instance.layers.player;
				character.gameObject.setLayerRecursively(LevelMaster.instance.layers.enemies);

				if (character.ai.canAttack && BuildManagerFlags.getFlag("enemiesDontAttack"))
					character.ai.canAttack = false;
				}

			sort(character, true);

			CharacterInstance characterInstance = new CharacterInstance(spawnableCharacter.type, instanceId);
			characters[classIndex].instances.Add(characterInstance);
			characterInstance.character = character;

			return character;
			}


		public void sort(Character character, bool front)
			{
			int lastSortingOrder = 0;
			SpriteRenderer[] renderers;

			string characterSortingLayer = character.spriteRenderers[0].sortingLayerName;

			for (int i = 0; i<characters.Length; i++)
				for (int j = 0; j<characters[i].instances.Count; j++)
					{
					renderers = characters[i].instances[j].character.spriteRenderers;
					for (int k = 0; k<renderers.Length; k++)
						{
						if (renderers[k].sortingLayerName!=characterSortingLayer)
							continue;

						if (front)
							{
							if (renderers[k].sortingOrder>lastSortingOrder)
								lastSortingOrder = renderers[k].sortingOrder;
							}
						else//behind
							{
							if (renderers[k].sortingOrder<lastSortingOrder)
								lastSortingOrder = renderers[k].sortingOrder;
							}
						}
					}
				

			renderers = character.spriteRenderers;

			if (!front)
				{
				int max = 0;

				for (int i = 0; i<renderers.Length; i++)
					{
					if (renderers[i].sortingLayerName!=characterSortingLayer)
						continue;

					if (renderers[i].sortingOrder>max)
						max = renderers[i].sortingOrder;
					}

				max += 1;

				lastSortingOrder -= max;
				}

			for (int i = 0; i<renderers.Length; i++)
				{
				if (renderers[i].sortingLayerName!=characterSortingLayer)
					continue;

				renderers[i].sortingOrder = 
					lastSortingOrder+(renderers[i].sortingOrder+1);	
				}
			}


		#endregion



		#region DEATH STUFF

		public void characterDied(Character character)
			{
			for (int i = 0; i<characters[character.classIndex].instances.Count; i++)
				if (characters[character.classIndex].instances[i].id==character.instanceId)
					{
					characters[character.classIndex].instances.RemoveAt(i);
					return;
					}
			}

		#endregion



		#region MISC

		public IEnumerable getFriendCharacters(Character askingCharacter)
			{
			for (int i = 0; i<characters.Length; i++)
				{
				for (int j = 0; j<characters[i].instances.Count; j++)
					{
					if (askingCharacter.type!=characters[i].instances[j].character.type 
						|| characters[i].instances[j].character.dead 
						|| characters[i].instances[j].character.pendingDeath)
						continue;

					if (characters[i].instances[j].character.instanceId==askingCharacter.instanceId)
						continue;	

					yield return characters[i].instances[j].character;
					}	
				}
			}


		private Vector2 pickEnemySpawnPoint(SpawnableCharacter spawnableEnemy)
			{
			Vector2 spawnPoint = new Vector2(0, 10f);

			if (UnityEngine.Random.value>spawnableEnemy.rightSpawnPointWeight*0.1)
				spawnPoint.x = CameraController.instance.worldBoundaries.min.x-5f;
				else
				spawnPoint.x = CameraController.instance.worldBoundaries.max.x+5f;

			spawnPoint.x += spawnableEnemy.spawnPointRadius.getRandomFromX();
			
			RaycastHit2D hit = Physics2D.Raycast(spawnPoint, Vector2.down, Mathf.Infinity, LevelMaster.instance.layers.ground);
			if (hit.collider!=null)
				spawnPoint.y = hit.point.y+0.5f+spawnableEnemy.spawnPointRadius.getRandomFromY();
				else
				Debug.Log("Couldnt find the ground to spawn "+spawnableEnemy.prefab.name);

			return spawnPoint;
			}


		public int getEnemyIndexById(int classIndex, int id)
			{
			for (int i=0; i<characters[classIndex].instances.Count; i++)
				if (characters[classIndex].instances[i].id==id)
					return i;

			return -1;
			}


		private int getNewInstanceId()
			{
			int id;
			bool found;

			while (true)
				{
				found = false;
				id = new System.Random().Next(1000000, 9999999);

				for (int i=0; i<characters.Length; i++)
					for (int j=0; j<characters[i].instances.Count; j++)
						if (characters[i].instances[j].id==id)
							found = true;

				if (!found)
					break;
                    //TODO change this so we dont block with a while loop
				}

			return id;
			}

		#endregion
		}	
	}