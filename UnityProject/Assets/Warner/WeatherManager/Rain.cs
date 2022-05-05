using System;
using UnityEngine;
using DG.Tweening;

namespace Warner
	{
	public class Rain: MonoBehaviour
		{
		#region MEMBER FIELDS


		[Serializable]
		public class Settings
			{
			public Material material;
			public Vector2 startSize;
            public float fadeTime = 25f;
			public float simulationSpeed;
			public int rateOverTime;
			public Vector2 velocity1;
			public Vector2 velocity2;
			public Color color1 = Color.white;
			public Color color2 = Color.white;
			[NonSerialized]
			public SortingLayer sortingLayer;
			[NonSerialized]
			public int sortingOrder;
			}

		private Settings settings;
		private ParticleSystem pSystem;
        private string rainFadeId;
		private ParticleSystem.MainModule mainModule;
		ParticleSystem.EmissionModule emissionModule;
		ParticleSystem.VelocityOverLifetimeModule velocityModule;
		ParticleSystem.ShapeModule shapeModule;
		ParticleSystemRenderer rendererModule;

		#endregion



		#region INIT STUFF

        private void Awake()
            {
            rainFadeId = "RainFade"+GetInstanceID();
            }

		public void init(Settings settings)
			{
			this.settings = settings;

			pSystem = gameObject.AddComponent<ParticleSystem>();            
			rendererModule = gameObject.GetComponent<ParticleSystemRenderer>();

			mainModule = pSystem.main;
			emissionModule = pSystem.emission;
			shapeModule = pSystem.shape;
			velocityModule = pSystem.velocityOverLifetime;

            pSystem.Stop();
			pSystem.useAutoRandomSeed = true;
			mainModule.duration = 5f;
			mainModule.loop = true;
			mainModule.prewarm = true;
			mainModule.startLifetime = 1.5f;
			mainModule.startSpeed = new ParticleSystem.MinMaxCurve(4f, 2f);
			mainModule.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
			mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
			mainModule.scalingMode = ParticleSystemScalingMode.Local;
			mainModule.playOnAwake = true;
			mainModule.maxParticles = 1000;
			mainModule.startSize = new ParticleSystem.MinMaxCurve(
				settings.startSize.x, settings.startSize.y);			
			mainModule.startColor = new ParticleSystem.MinMaxGradient(
				settings.color1, settings.color2);			
			mainModule.simulationSpeed = settings.simulationSpeed;

			emissionModule.rateOverTime = settings.rateOverTime;
			emissionModule.enabled = false;

			shapeModule.shapeType = ParticleSystemShapeType.Box;
			shapeModule.scale = new Vector3(20f, 0.5f, 10f);

			velocityModule.space = ParticleSystemSimulationSpace.Local;
			velocityModule.enabled = true;
			velocityModule.x = new ParticleSystem.MinMaxCurve(
				settings.velocity1.x, settings.velocity2.x);			
			velocityModule.y = new ParticleSystem.MinMaxCurve(
				settings.velocity1.y, settings.velocity2.y);			
			velocityModule.z = new ParticleSystem.MinMaxCurve(0f, 0f);

			rendererModule.material = settings.material;
			rendererModule.renderMode = ParticleSystemRenderMode.Stretch;
			rendererModule.sortingLayerName = settings.sortingLayer.name;
			rendererModule.sortingOrder = settings.sortingOrder;
			}

		public bool isEnabled
            {
            set
                {
                if (!emissionModule.enabled)
                    emissionModule.enabled = true;

                //fade in/out
                float targetRate = value ? settings.rateOverTime : 0;

                if (value)
                    {
                    emissionModule.rateOverTime = 0;
                    pSystem.Play();
                    }

                DOTween.Kill(rainFadeId);

                DOTween.To(() => emissionModule.rateOverTime.constant, 
                    x => emissionModule.rateOverTime = x, targetRate, settings.fadeTime).OnComplete(() =>
                    {
                    if (!value)
                        pSystem.Stop();
                    }).SetId(rainFadeId);	
				}
			}
				

		#endregion



		#region DESTROY

		private void OnDestroy()
			{

			}

		#endregion
		}
	}