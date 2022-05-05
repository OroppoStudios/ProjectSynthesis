using System;
using UnityEngine;

namespace Warner
	{
	[Serializable]
	public struct Layer
		{
		public string name;
        public int id
            {
            get
                {
                return LayerMask.NameToLayer(name);
                }
            }
		}

	[Serializable]
	public struct SortingLayer
		{
		public string name;
		}	
	}