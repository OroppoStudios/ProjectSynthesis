using UnityEngine;
using System.Collections.Generic;
using System;

namespace Warner
	{
	public class SpriteFlasher: MonoBehaviour
		{
		#region MEMBER FIELDS

		public Color color = Color.red;

		[NonSerialized] public List<RendererData> spriteRenderers = new List<RendererData>();

		public struct RendererData
			{
			public SpriteRenderer renderer;
			[NonSerialized] public Color originalColor;
			}

		private Color originalColor;
		private float stateStartTime;
		private float fadeInDuration;
		private float fadeOutDuration;
		private float holdDuration;
		private State state;

		private enum State {Idle, FadeIn, FadeOut, Hold}

		#endregion


	
		#region INIT STUFF

		public void Awake()
			{
			addSpriteRenderer(GetComponent<SpriteRenderer>());
			}


		public void addSpriteRenderer(SpriteRenderer spriteRenderer)
			{
			if (spriteRenderer==null)
				return;

			RendererData rendererData = new RendererData();
			rendererData.renderer = spriteRenderer;

			if (!spriteRenderers.Contains(rendererData))
				spriteRenderers.Add(rendererData);
			}

		#endregion



		#region UPDATE/FLASH

		public void flash(float duration, float fadeIn = 0f, float fadeOut = 0f)
			{
			if (state!=State.Idle)
				return;

			RendererData rendererData;

			for (int i = 0; i<spriteRenderers.Count; i++)
				{
				rendererData = spriteRenderers[i];
				rendererData.originalColor = rendererData.renderer.color;
				spriteRenderers[i] = rendererData;
				}

			fadeInDuration = fadeIn;
			fadeOutDuration = fadeOut;
			holdDuration = duration;
			setState(State.FadeIn);
			}


		public void stopFlash()
			{
			setState(State.Idle);

			for (int i = 0; i<spriteRenderers.Count; i++)
				spriteRenderers[i].renderer.color = spriteRenderers[i].originalColor;
			}


		private void setState(State targetState)
			{
			state = targetState;
			stateStartTime = Time.time;
			}


		private void Update()
			{
			float duration;

			switch (state)
				{
				case State.FadeIn:
					duration = fadeInDuration;
				break;
				case State.Hold:
					duration = holdDuration;
				break;
				case State.FadeOut:
					duration = fadeOutDuration;
				break;
				default:
				return;
				}

			float elapsed = Time.time-stateStartTime;
			float percent = elapsed/duration;

			switch (state)
				{
				case State.FadeIn:	
					for (int i = 0; i<spriteRenderers.Count; i++)
						spriteRenderers[i].renderer.color = Color.Lerp(spriteRenderers[i].originalColor, color, percent);
				break;
				case State.FadeOut:
					for (int i = 0; i<spriteRenderers.Count; i++)
						spriteRenderers[i].renderer.color = Color.Lerp(color, spriteRenderers[i].originalColor, percent);
				break;
				}

			if (percent>=1)
				{
				switch(state)
					{
					case State.FadeIn:
						setState(State.Hold);
					break;
					case State.Hold:
						setState(State.FadeOut);
					break;
					case State.FadeOut:
						state = State.Idle;
					break;
					}
				}
			}


		#endregion
		}

	}