using UnityEngine;
using DG.Tweening;
using System;

namespace Warner
	{
	public class ThunderLight: MonoBehaviour
		{
		#region MEMBER FIELDS

		public GlowData glow;
		public Fade thunderFade;
		public Fade lightFade;
		public float lightIntensity;
		public SortingLayer sortingLayer;
		public int sortingOrder;
		public Sprite[] sprites;

		[Serializable]
		public class GlowData
			{
			public Color color = Color.blue;
			public float strength = 1f;
			public float blur = 1f;
			}

		[Serializable]
		public class Fade
			{
			public float fadeIn = 0.1f;
			public float fadeOut = 0.35f;
			}

		private SpriteRenderer spriteRenderer;
		private Light theLight;
//		private Glow glowComponent;

		private const float lifeTime = 0.025f;

		#endregion


		
		#region INIT STUFF
		
		private void Awake()
			{
			spriteRenderer = GetComponent<SpriteRenderer>();
			theLight = transform.GetChild(0).GetComponent<Light>();
//			glowComponent = GetComponent<Glow>();
			}


		private void OnEnable()
			{
			spriteRenderer.sortingLayerName = sortingLayer.name;
			spriteRenderer.sortingOrder = sortingOrder;
			spriteRenderer.sprite = sprites.getRandom();

			transform.localScale = transform.localScale.setX(
				(UnityEngine.Random.value>0.5f) ? 1 : -1);

			spriteRenderer.color = spriteRenderer.color.setAlpha(0);
			theLight.intensity = 0;

//			glowComponent.strength = glow.strength;
//			glowComponent.blur = glow.blur;
			//glowComponent.startGlow(settings.glowColor);

			spriteRenderer.DOFade(1, thunderFade.fadeIn).OnComplete(() =>
				spriteRenderer.DOFade(0, thunderFade.fadeOut));

			theLight.DOIntensity(lightIntensity, lightFade.fadeIn).OnComplete(() =>
				theLight.DOIntensity(0, lightFade.fadeOut).OnComplete(() =>
					PoolManager.Destroy(gameObject)));
			}
		
		#endregion
		}
	}