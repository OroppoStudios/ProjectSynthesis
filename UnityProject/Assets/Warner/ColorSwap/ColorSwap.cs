using UnityEngine;
using System;
using System.Collections.Generic;

namespace Warner
	{
	public class ColorSwap: MonoBehaviour
		{
		#region MEMBER FIELDS

        public Color unswappedColor;
        public SpriteRenderer lineArtSpriteRenderer;
		[Range (0f, 1f)] public float lineArtAlphaOverride = 0.5f;

		[Serializable]
		public class Config
			{
			public List<SwapColorObject> swapColorObjects = new List<SwapColorObject>();
			}

		[Serializable]
		public class SwapColorObject
			{
			public string name;
			public List<ColorData> topColors;
			public List<ColorData> bottomColors;
			public List<ColorDataPart> colorDataInfo = new List<ColorDataPart>();
			[SerializeField]
			public bool editorVisible;
			[SerializeField]
			public bool editorPartsVisible;
			}

		[Serializable]
		public class ColorDataPart
			{
			public string name;
			public int redChannel;
            public int textureIndex;
			public List<ColorData> colorVariants = new List<ColorData>();
			}

		[Serializable]
		public class ColorData
			{
			public float r;
            public float g;
            public float b;
            public float a = 1;

			public ColorData(){}

			public ColorData(Color color)
				{
				r = color.r;
				g = color.g;
				b = color.b;
				a = color.a;
				}

			public Color getColor()
				{
				return new Color(r, g, b, a);
				}						
			}

		public const string configFileName = "ColorSwapManagerData";

		private Config config;
		private Texture2D dataTexture;
		private SpriteRenderer spriteRenderer;
		private string lastSchemeName;
		private int lastSchemeIndex;
		private bool updateSchemeEveryFrame;
		private int lastLineArtTexture;
		private float lastLineArtAlphaOverride;

		#endregion



		#region INIT

		private void Awake()
			{
			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.material = new Material(Shader.Find("Warner/Sprites/ColorSwap"));
			updateSchemeEveryFrame = BuildManagerFlags.getFlag("debugColorSwap");
			createDataTexture();
			loadConfig();
			}

		private void Start()
			{
			spriteRenderer.material.SetFloat("_OverrideLineArtOverlap", lineArtSpriteRenderer!=null ? 1 : 0);
			}

		#endregion



		#region UPDATE

		private void Update()
			{
			checkLineArtTexture();
			checkDebugMode();
			}


		private void checkDebugMode()
			{
			if (!updateSchemeEveryFrame || spriteRenderer==null)
				return;

			createDataTexture();
			loadConfig();
			swapColor(lastSchemeName, lastSchemeIndex);
			}


		private void checkLineArtTexture()
			{
			if (lineArtSpriteRenderer==null || lineArtSpriteRenderer.sprite==null)
				return;

			if (lastLineArtTexture==lineArtSpriteRenderer.sprite.texture.GetInstanceID())
				return;

			if (lastLineArtAlphaOverride!=lineArtAlphaOverride)
				{
				lastLineArtAlphaOverride = lineArtAlphaOverride;
				spriteRenderer.material.SetFloat("_LineArtAlphaOverride", lineArtAlphaOverride);
				}

			lastLineArtTexture = lineArtSpriteRenderer.sprite.texture.GetInstanceID();
			spriteRenderer.material.SetTexture("_LineArtTexture", lineArtSpriteRenderer.sprite.texture);
			}

		#endregion



		#region CONFIG

		private void loadConfig()
			{
			TextAsset configAsset = Misc.loadConfigAsset(configFileName);		
			config = Misc.loadConfigFile<Config>(configAsset);
			}


		public SwapColorObject getSwapObjectByName(string name)
			{
			for (int i = 0; i<config.swapColorObjects.Count; i++)
				if (config.swapColorObjects[i].name==name)
					return config.swapColorObjects[i];

			return null;
			}


		public ColorDataPart getSwapColorDataPartByName(SwapColorObject swapObject, string name)
			{
			for (int i = 0; i<swapObject.colorDataInfo.Count; i++)
				if (swapObject.colorDataInfo[i].name==name)
					return swapObject.colorDataInfo[i];

			return null;
			}

		#endregion



		#region SWAP STUFF
        
		public void swapColor(string schemeName, int schemeIndex)
            {
			SwapColorObject swapObject = getSwapObjectByName(schemeName);

            if (swapObject!=null)
                {
				if (swapObject.topColors!=null && swapObject.topColors.Count>schemeIndex)
					spriteRenderer.material.SetColor("_GradientTopColor", 
						swapObject.topColors[schemeIndex].getColor());

				if (swapObject.bottomColors!=null && swapObject.bottomColors.Count>schemeIndex)
					spriteRenderer.material.SetColor("_GradientBottomColor", 
						swapObject.bottomColors[schemeIndex].getColor());


                ColorDataPart colorDataPart;

                for (int i = 0; i<swapObject.colorDataInfo.Count; i++)
                    {          
                    colorDataPart = swapObject.colorDataInfo[i];  

					if (colorDataPart.colorVariants.Count<=schemeIndex)
						continue;

                    dataTexture.SetPixel(colorDataPart.redChannel, 0, 
                        colorDataPart.colorVariants[schemeIndex].getColor());
                    }

                dataTexture.Apply();
				}

			lastSchemeName = schemeName;
			lastSchemeIndex = schemeIndex;
			}


		public void createDataTexture()
            {
            dataTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false, false);
            dataTexture.filterMode = FilterMode.Point;

            for (int i = 0; i<dataTexture.width; ++i)
                dataTexture.SetPixel(i, 0, unswappedColor);

            dataTexture.Apply();

            spriteRenderer.material.SetTexture("_DataTexture", dataTexture);
			}

		#endregion



		#region MISC

		private Color ColorFromInt(int c, float alpha = 1.0f)
			{
			int r = (c >> 16) & 0x000000FF;
			int g = (c >> 8) & 0x000000FF;
			int b = c & 0x000000FF;

			return ColorFromIntRGB(r, g, b).setAlpha(alpha);
			}

		private Color ColorFromIntRGB(int r, int g, int b)
			{
			return new Color((float)r/255.0f, (float)g/255.0f, (float)b/255.0f, 1.0f);
			}


		#endregion
		}
	}
