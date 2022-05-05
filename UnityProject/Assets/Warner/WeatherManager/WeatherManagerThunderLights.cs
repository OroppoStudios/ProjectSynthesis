using System.Collections.Generic;
using UnityEngine;
using System;

namespace Warner
	{
	public partial class WeatherManager
		{
		[Serializable]
		public class ThunderLightsManager
			{
			#region MEMBER FIELDS

            public Interval interval;
            public LayerMask lightTargetLayers;
            public float lightIntensity;
            public Layer transformLayer;
            public Color thunderColor = Color.white;
            public ThunderLight.GlowData glow;
            public ThunderLight.Fade thunderFade;
            public ThunderLight.Fade lightFade;
            public Sprite[] sprites;
            public ParallaxLayer[] layers;

			private bool _enabled;
			private ParallaxLayer[] enabledLayers;
			private IEnumerator<float> spawnRoutine;
            private GameObject prefab;

			#endregion



			#region INIT 

			public bool enabled
				{
				get
					{ 
					return _enabled;
					}
				set
					{
					_enabled = value;

					if (spawnRoutine!=null)
						Timing.kill(spawnRoutine);

					if (_enabled)
						{      
                        if (sprites.Length==0)
                            {
                            Debug.LogWarning("Weather Manager: Please set the thunderlight sprites");
                            return;
                            }

                                                                     
						if (enabledLayers==null)
							{
							Debug.LogWarning("Weather Manager: Thunderlights not initialized, call init method");
							return;
							}

						if (enabledLayers.Length==0)
							{
							Debug.LogWarning("Weather Manager: There are no target parallax layers for the thunderlights");
							return;
							}

						spawnRoutine = spawnCoRoutine();
						Timing.run(spawnRoutine);
						}
					}
				}


			public void init()
				{
                instance.getLayerContainers(layers);

				enabledLayers = layers.transformTo<ParallaxLayer, ParallaxLayer>
					(layer =>
					{
					if (!layer.transform.gameObject.activeSelf || !layer.enabled)
						return null;

					return layer;					 
					}, true);

                createPrefab();
				}


            private void createPrefab()
                {
                prefab = new GameObject("ThunderlightPrefab");
                prefab.gameObject.layer = transformLayer.id;
                prefab.SetActive(false);
                prefab.AddComponent<Glow>();
				SpriteRenderer spriteRenderer = prefab.AddComponent<SpriteRenderer>();
				spriteRenderer.color = thunderColor;

                PoolObject poolObject = prefab.AddComponent<PoolObject>();
                poolObject.offScreenDestroy = false;


                ThunderLight thunderLight = prefab.AddComponent<ThunderLight>();
                thunderLight.sprites = sprites;
                thunderLight.lightIntensity = lightIntensity;
                thunderLight.glow = glow;
                thunderLight.thunderFade = thunderFade;
                thunderLight.lightFade = lightFade;

                GameObject lightObject = new GameObject("Light");
                lightObject.transform.SetParent(prefab.transform);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
                light.cookieSize = 0;
                light.renderMode = LightRenderMode.ForceVertex;
                light.bounceIntensity = 0;
                }

			#endregion



			#region SPAWN

			private IEnumerator<float> spawnCoRoutine()
                {            
                float maxTime;

                while (true)
                    {
                    switch (interval)
                        {
                        case Interval.High:
                            maxTime = 4f;
                        break;
                        case Interval.Mid:
                            maxTime = 7f;
                        break;
                        default:
                            maxTime = 11f;
                        break;
                        }

                    yield return Timing.waitForSeconds(UnityEngine.Random.Range(1f, maxTime));

                    spawn();

                    //chance to get two thunderlights almost at same time
                    if (UnityEngine.Random.value>0.8f)
                        {
                        yield return Timing.waitForSeconds(UnityEngine.Random.Range(0.25f, 0.6f));
                        spawn();
                        }
					}
				}


            private void spawn()
                {
                Vector2 position = Vector2.zero;
                position.x = CameraController.instance.worldBoundaries.getRandomX();
                position.y = CameraController.instance.worldBoundaries.yMax+2f;

                ParallaxLayer parallaxLayer = enabledLayers.getRandom();

                ThunderLight thunderLight = PoolManager.instantiate(prefab, position, parallaxLayer.transform)
                    .GetComponent<ThunderLight>();

                thunderLight.sortingLayer = parallaxLayer.sortingLayer;
                thunderLight.sortingOrder = parallaxLayer.sortingOrder;
                thunderLight.gameObject.SetActive(true);
                //thunderLight.init();
                }

			#endregion

			}
		}
	}
