using System.Collections.Generic;
using UnityEngine;
using System;

namespace Warner
	{
	public partial class WeatherManager
		{
		[Serializable]
		public class RainManager
			{
			#region MEMBER FIELDS
            
			public RainLayer[] layers;

			[Serializable]
			public class RainLayer: ParallaxLayer
				{
				public float verticalOffset;
				public Rain.Settings particleSystem;
				[NonSerialized] public List<Rain> objects = new List<Rain>();
				}

			public delegate void EventsHandler();

			public event EventsHandler onStartedRaining;
			public event EventsHandler onStoppedRaining;

			private bool _enabled;

			#endregion



			#region INIT

			public void init()
				{
                instance.getLayerContainers(layers);
				createPrefabs();
				WorldTiling.instance.onWorldReady += onWorldReady;
				}
             

			private void createPrefabs()
				{
				GameObject rainObject;
				Rain rain;

				for (int i = 0; i<layers.Length; i++)
					{
					if (!layers[i].enabled)
						continue;

					if (layers[i].transform.childCount==0)
						{
						Debug.LogWarning("WeatherManager: The parallax layer "+
						    layers[i].transform.name+" doesnt have a WorldTiling chunk child");
                        //we need the world tiling chunk because rain is part of the chunks system 
                        //so that it gets correctly parallaxed and is infinite
                        continue;
						}

					rainObject = new GameObject("Rain");
					rainObject.layer = layers[i].transform.gameObject.layer;
					rainObject.transform.SetParent(layers[i].transform.GetChild(0));
					rain = rainObject.AddComponent<Rain>();
					rain.transform.localPosition = rain.transform.localPosition.
						setY(layers[i].verticalOffset);

					layers[i].particleSystem.sortingLayer = layers[i].sortingLayer;
					layers[i].particleSystem.sortingOrder = layers[i].sortingOrder;
					}
				}


			private void onWorldReady()
				{
				WorldTiling.LayerData[] tilingLayers = WorldTiling.instance.layers;			

				for (int i = 0; i<tilingLayers.Length; i++)
					{
					if (!tilingLayers[i].enabled)
						continue;

					initRainOnBuddy(tilingLayers[i].leftBuddy.transform, tilingLayers[i]);
					initRainOnBuddy(tilingLayers[i].centerBuddy.transform, tilingLayers[i]);
					initRainOnBuddy(tilingLayers[i].rightBuddy.transform, tilingLayers[i]);
					}
				}

			private void initRainOnBuddy(Transform buddy, WorldTiling.LayerData tilingLayer)
				{
				Transform rainTransform = buddy.Find("Rain");

				if (rainTransform==null)
					return;

				RainLayer rainLayer = layers.findMathingOn<RainLayer, WorldTiling.LayerData>
					(tilingLayer, (rainItem, layerItem) =>
					rainItem.transform.name==layerItem.transform.name);

				Rain rain = rainTransform.GetComponent<Rain>();
				rainLayer.objects.Add(rain);
				rain.init(rainLayer.particleSystem);								
				}

			#endregion



			#region ENABLE

			public bool enabled
				{
				get
					{ 
					return _enabled;
					}
				set
					{
					_enabled = value;

					for (int i = 0; i<layers.Length; i++)
						for (int j = 0; j < layers[i].objects.Count; j++)
							layers[i].objects[j].isEnabled = _enabled;

					if (_enabled && onStartedRaining!=null)
						onStartedRaining();
					else
					if (!_enabled && onStoppedRaining!=null)
						onStoppedRaining();
					}
				}

			#endregion



			#region DESTROY

			public void onDisable()
				{
				WorldTiling.instance.onWorldReady -= onWorldReady;
				}


			public void clear()
				{
				_enabled = false;

				for (int i = 0; i<layers.Length; i++)
					{
					for (int j = 0; j<layers[i].objects.Count; j++)
						Destroy(layers[i].objects[j].gameObject);

					layers[i].objects.Clear();
					}
				}

			#endregion
			}
		}
	}
