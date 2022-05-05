using UnityEngine;
using System;
using System.Collections.Generic;

namespace Warner 
	{
	public class PoolManager: MonoBehaviour
		{
		#region MEMBER FIELDS

		public PoolContainerData[] poolContainers;

		[Serializable]
		public struct PoolContainerData
			{
			public Transform transform;
			public int sortingOrder;
			}

		public static PoolManager instance;

		public static Dictionary<string, PoolContainer> pools = new Dictionary<string,PoolContainer>();

		public const string poolContainerName = "Pools";

		#endregion



		#region INIT STUFF

		private void Awake()
			{
			instance = this;
			}

		#endregion



		#region CREATE AND DESTROY STUFF

		public static GameObject instantiate(GameObject prefab, Vector3 pos, Transform parentContainer = null, 
			string objectReference = "", int maxInstances = -1, bool useLocalPosition = false)
			{
			GameObject objInstance = null;		

			if (parentContainer==null)
				parentContainer = instance.getMainPoolContainer().transform;

			objectReference = parentContainer.name+"_"+((objectReference!="") ? objectReference : prefab.name);

			var recycleGameObject = prefab.GetComponent<PoolObject>();
			if (recycleGameObject!=null)
				{
				PoolContainer pool = getPool(recycleGameObject, parentContainer, objectReference, maxInstances);
				PoolObject poolObject = pool.getNextObject(pos, useLocalPosition);
				if (poolObject)
					objInstance = poolObject.gameObject;
				}
				else
				{
				objInstance = UnityEngine.Object.Instantiate(prefab);
				objInstance.transform.position = pos;		
				}
		
			return objInstance;
			}

		public static void Destroy(GameObject gameObject, float DestroyTime = 0)
			{
			PoolObject recycleGameObject = gameObject.GetComponent<PoolObject>();

			if (recycleGameObject!=null)
				recycleGameObject.disable(DestroyTime);
				else
				UnityEngine.Object.Destroy(gameObject, DestroyTime);

			}

		#endregion

		

		#region POOL STUFF

		public static void clear()
			{
			foreach (KeyValuePair<string,PoolContainer> pool in pools)
				Destroy(pool.Value.gameObject);

			pools.Clear();
			}


		private static PoolContainer getPool(PoolObject spawnObject, Transform parentContainer, string reference, int maxInstances)
			{
			PoolContainer pool;

			if (pools.ContainsKey(reference))
				{
				pool = pools[reference];
				}
				else
				{
				GameObject poolContainer = new GameObject(reference+poolContainerName);

				if (parentContainer!=null)
					{
					poolContainer.transform.parent = parentContainer;
					poolContainer.transform.localPosition = Vector2.zero;
					poolContainer.transform.localScale = parentContainer.localScale;
					}

				pool = poolContainer.AddComponent<PoolContainer>();
				pool.prefab = spawnObject;
				pool.maxInstances = maxInstances;
				pools.Add(reference, pool);
				}

			return pool;
			}


		public PoolContainerData getMainPoolContainer()
			{
			if (poolContainers.Length==0)
				{
				poolContainers = new PoolContainerData[1];
				poolContainers[0] = new PoolContainerData();
				poolContainers[0].transform = new GameObject("MainPoolContainer").transform;
				}

			return poolContainers[0];
			}

		#endregion
		}
	}