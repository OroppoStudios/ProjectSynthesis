using UnityEngine;
using System.Collections.Generic;
using System;

namespace Warner
    {
    public class VfxManager: MonoBehaviour
        {
        #region MEMBER FIELDS

		public GameObject[] prefabs;

        public static VfxManager instance;

        [NonSerialized] public List<Vfx> instances = new List<Vfx>();

        #endregion



        #region INIT

        private void Awake()
            {
            instance = this;
            }

        private void OnEnable()
			{
			instances.Clear();
			}           

        #endregion



        #region VFX

		public Vfx playVfx(string name, string animationName, Vector2 targetPosition, Vector2 scale, float rotation, 
			Character owner, Transform parent = null)
			{
			if (BuildManagerFlags.getFlag("disableVfx"))
				return null;

			int index = -1;
			for (int i = 0; i<prefabs.Length; i++)
				if (prefabs[i]!=null && prefabs[i].name==name)
					{
					index = i;     
					break;
					}

			if (index==-1)
				{
				Debug.LogWarning("FxAssetManager: Animation "+name+" is not added in the animations list");
				return null;
				}

			GameObject theObject = PoolManager.instantiate(prefabs[index], targetPosition, parent);
			theObject.transform.localScale = scale;
			theObject.transform.eulerAngles = new Vector3(1f, 1f, rotation);

			Vfx vfx = theObject.GetComponent<Vfx>();

			if (vfx==null)
				{
				Debug.LogWarning("Vfx component not attached to: "+name);
				return null;
				}
				else
				{
				if (string.IsNullOrEmpty(animationName))
					animationName = "Default";

				vfx.playAnimation(animationName);

				vfx.ownerCharacter = owner;
				instances.Add(vfx);
				return vfx;
				}
			}


        public bool destroyVfxByName(string name, float fade = 0f)
			{
			bool ret = false;

			for (int i = instances.Count-1; i>=0; i--)
				{
				if (instances[i].name==name)
					{
					instances[i].fadeAndDestroy(fade);

					ret = true;
					}
				}

			return ret;
        	}


        public bool removeVfxInstance(int id)
			{
			bool ret = false;

			for (int i = instances.Count-1; i>=0; i--)
				{
				if (instances[i].instanceId==id)
					{
					instances.RemoveAt(i);
					ret = true;
					}
				}

			return ret;
			}

        #endregion
        }
    }