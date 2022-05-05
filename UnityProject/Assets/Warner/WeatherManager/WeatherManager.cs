using UnityEngine;
using System;

namespace Warner
	{
	public partial class WeatherManager : MonoBehaviour
		{
		#region MEMBER FIELDS

        public Transform layersContainer;
		public ThunderLightsManager thunderLights;
		public RainManager rain;

		[Serializable]
		public class ParallaxLayer
			{
			public bool enabled;
            public string targetContainer;
			[NonSerialized] public Transform transform;
			public SortingLayer sortingLayer;
			public int sortingOrder;
			}

		public enum Interval {Low, Mid, High}

		public static WeatherManager instance;

		#endregion



		#region INIT

		private void Awake()
			{
			instance = this;
			}


		public void init()
			{
			thunderLights.init();
			rain.init();
			}

        private void getLayerContainers(ParallaxLayer[] layers)
            {
            Transform targetContainer;

            for (int i = 0; i<layers.Length; i++)
                {
                targetContainer = layersContainer.Find(layers[i].targetContainer);

                if (targetContainer!=null)
                    layers[i].transform = targetContainer;
                    else
                    layers[i].enabled = false;
                }
            }

		#endregion



		#region DESTROY

		private void OnDisable()
			{
			rain.onDisable();
			}

		public void clear()
			{
			rain.clear();
			}

            		#endregion
		}
	}