using UnityEngine;
using System;
using System.Collections.Generic;

namespace Warner
    {
    public class Glow : MonoBehaviour
        {
        #region MEMBER FIELDS

        public FxData tintData = new FxData();
        public GlowData glowData = new GlowData();

        public delegate void FxEvent();

        [Serializable]
        public class GlowData: FxData
            {
            public float strength = 1.5f;
            public float blur = 0.45f;
            }

        [Serializable]
        public class FxData
            {
            public bool active;
            public Color color = Color.red;
            public float fadeIn;
            public float fadeInDelay;
            public ColorLerp lerp;
            [NonSerialized] public bool originalActive;
			[NonSerialized] public Color originalColor;
            }

        public class ColorLerp
            {
            public bool active;
            public bool isSine;
            public Color fromColor;
            public Color toColor;
            public float duration;
            public float percentage;
            public float startTime;
            public FxEvent onFinish;

            public ColorLerp(Color from, Color to, float duration, FxEvent onFinish, bool isSine = false)
                {
                fromColor = from;
                toColor = to;
                this.isSine = isSine;
                this.duration = duration;
                this.onFinish = onFinish;
                startTime = Time.unscaledTime;
                active = true;
                }
            }

        private Shader glowShader;
        private Material originalMaterial;
        private Material glowMaterial;
        private SpriteRenderer spriteRenderer;
        private bool ready;
        private bool glowShaderActive;
        private IEnumerator<float> glowFadeInRoutine;
		private IEnumerator<float> tintFadeInRoutine;

        #endregion



        #region INIT STUFF


        private void Awake()
            {
            glowShader = Shader.Find("Warner/Sprites/Glow");
            glowMaterial = new Material(glowShader);
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            backupOriginalValues(tintData);
			backupOriginalValues(glowData);
            }


        private void backupOriginalValues(FxData data)
        	{
			data.originalActive = data.active;
			data.originalColor = data.color;
        	}


        private void OnEnable()
			{
			clearFadeInTintDelaysRoutine();
			clearFadeInGlowDelaysRoutine();

			glowFadeInRoutine = checkFadeIn(tintData, false);
			Timing.run(glowFadeInRoutine);

			tintFadeInRoutine = checkFadeIn(glowData, true);
			Timing.run(tintFadeInRoutine);
        	}   


        private void clearFadeInTintDelaysRoutine()
			{
			if (tintFadeInRoutine!=null)
				Timing.kill(tintFadeInRoutine);
			}


		private void clearFadeInGlowDelaysRoutine()
			{
			if (glowFadeInRoutine!=null)
				Timing.kill(glowFadeInRoutine);
			}


       	private IEnumerator<float> checkFadeIn(FxData data, bool isGlow)
			{
			if (!(data.originalActive && data.fadeIn!=0f))
				yield break;

			data.active = false;
			Color color = data.originalColor;
			color.a = 0;

			if (!isGlow)
				spriteRenderer.color = color;

			color.a = data.originalColor.a;

			yield return Timing.waitForSeconds(data.fadeInDelay);

			if (isGlow)
				startGlow(color, data.fadeIn);
				else
				tint(color, data.fadeIn);
			}     

        #endregion



        #region FRAME UPDATE

        private void Update()
            {
            updateGlowValues();
            updateTintValues();
            updateMaterial();
            }


		private void updateMaterial()
            {
            if (glowData.active)
                {
                if (!glowShaderActive)
                    {
                    originalMaterial = spriteRenderer.sharedMaterial;
                    spriteRenderer.sharedMaterial = glowMaterial;
                    glowShaderActive = true;
                    }

                spriteRenderer.material.SetColor("_GlowColor", glowData.color);
                spriteRenderer.material.SetFloat("_GlowBlur", glowData.blur);
                spriteRenderer.material.SetFloat("_GlowStrength", glowData.strength);
                } else
                {
                if (glowShaderActive)
                    {
                    spriteRenderer.sharedMaterial = originalMaterial;
                    glowShaderActive = false;
                    }
                }
            }

        #endregion



        #region GLOBAL COLOR FX STUFF

		private Color colorTransition(ColorLerp colorLerp)
            {
            if (colorLerp.isSine)
                {
                return Color.Lerp(colorLerp.fromColor, colorLerp.toColor, 
                    0.5f*Mathf.Sin(Time.unscaledTime*colorLerp.duration*(Mathf.PI*2))+0.5f);
                }

            float elapsed = Time.unscaledTime-colorLerp.startTime;
            colorLerp.percentage = elapsed/colorLerp.duration;

            if (colorLerp.percentage>=1f)
                {
                colorLerp.active = false;

                if (colorLerp.onFinish!=null)
                    colorLerp.onFinish();
                }

            return Color.Lerp(colorLerp.fromColor, colorLerp.toColor, colorLerp.percentage);
            }


		private void stopColorFx(FxData data, Color fadeColor, float transitionDuration, FxEvent onFinish = null)
            {
            if (data.active)
                {
                data.lerp = new ColorLerp(data.color, fadeColor, transitionDuration, () =>
                        {
                        data.lerp.active = false;
                        data.active = false;

                        if (onFinish!=null)
                            onFinish();
                        });                                 
                } else
                data.active = false;
            }


		private void showColorFx(FxData data, Color color, Color fadeColor, float transitionDuration, FxEvent onFinish = null)
            {
            Color toColor = color;

            if (data.active)
                {
                if (data.lerp!=null && data.lerp.active)
                    data.lerp.active = false;

                fadeColor = data.color;
                toColor = color;
                } else
                data.active = true;

            data.lerp = new ColorLerp(fadeColor, toColor, transitionDuration, onFinish);
            }


		private void flashingColorFx(FxData data, Color color1, Color color2, float frequency)
            {
            if (data.active)
                {
                if (data.lerp!=null && data.lerp.active)
                    data.lerp.active = false;
                } else
                data.active = true;

            data.lerp = new ColorLerp(color1, color2, frequency, null, true);
            }

        #endregion



        #region TINT STUFF

		private void updateTintValues()
            {
            if (tintData.lerp!=null && tintData.lerp.active)
                {
                tintData.color = colorTransition(tintData.lerp);
                }

            if (tintData.active)
                spriteRenderer.color = tintData.color;
            }


        public void tint(Color color, float transitionDuration = 0f, FxEvent onFinish = null)
            {
            showColorFx(tintData, color, spriteRenderer.color, transitionDuration, onFinish);
            }


        public void stopTint(Color toColor, float transitionDuration = 0f, FxEvent onFinish = null)
			{
			clearFadeInTintDelaysRoutine();

			if (transitionDuration==0)
				{
				tintData.active = false;

				if (tintData.lerp!=null)
					tintData.lerp.active = false;

				spriteRenderer.color = toColor;
				}
				else
				stopColorFx(tintData, toColor, transitionDuration, onFinish);
            }


        public void flashingTint(Color color1, Color color2, float frequency)
            {           
            flashingColorFx(tintData, color1, color2, frequency);
            }

        #endregion



        #region GLOW STUFF

		private void updateGlowValues()
            {
            if (glowData.lerp!=null && glowData.lerp.active)
                glowData.color = colorTransition(glowData.lerp);
            }

        public void startGlow(Color color, float transitionDuration = 0f, FxEvent onFinish = null)
            {           
            showColorFx(glowData, color, glowData.color.setAlpha(0), transitionDuration, onFinish);
            }


        public void stopGlow(float transitionDuration = 0f, FxEvent onFinish = null)
            {
			clearFadeInGlowDelaysRoutine();
            stopColorFx(glowData, glowData.color.setAlpha(0), transitionDuration, onFinish);
            }


        public void flashingGlow(Color color1, Color color2, float frequency)
            {
            flashingColorFx(glowData, color1, color2, frequency);
            }

        #endregion

        }
    }