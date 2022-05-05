using UnityEngine;
using System.Collections.Generic;

namespace Warner 
	{

	public class PoolContainer: MonoBehaviour
		{
		#region MEMBER FIELDS

		public PoolObject prefab;
		public int maxInstances;
		public List<PoolObject> poolInstances = new List<PoolObject>();

		#endregion



		#region POOL STUFF

		private PoolObject createPooledObject(Vector3 pos,bool useLocalPosition)
			{
			PoolObject clone = (useLocalPosition) ? GameObject.Instantiate(prefab) : (PoolObject) GameObject.Instantiate(prefab,pos,Quaternion.identity);	
					
			clone.transform.SetParent(transform);
			clone.name = prefab.name;
			if (useLocalPosition)
				clone.transform.localPosition = pos;

			//Debug.Log("Instantiated: "+prefab.name);
			poolInstances.Add (clone);
			return clone;
			}
			
			
		public PoolObject getNextObject(Vector3 pos,bool useLocalPosition)
			{
			PoolObject instance = null;

			for (int i=0;i<poolInstances.Count;i++)
				{
				if (!poolInstances[i].gameObject.activeSelf)
					{
					instance = poolInstances[i];
					instance.transform.position = pos;
					instance.reset();
					break;
					}	
				}

			if (instance==null && (poolInstances.Count<maxInstances || maxInstances==-1)) 
				instance = createPooledObject(pos,useLocalPosition);

			return instance;
			}
			
		#endregion
		}

}