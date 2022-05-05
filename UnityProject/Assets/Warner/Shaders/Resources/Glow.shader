Shader "Warner/Sprites/Glow"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_GlowBlur ("Glow Blur",Range(0,1)) = 0.45
		_GlowColor ("Glow Color", Color) = (1,1,1,1)
		_GlowStrength("Glow Strength",Range(1,2)) = 1.1
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "FiltersCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord  : TEXCOORD0;
			};
			
			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				return OUT;
			}

			sampler2D _MainTex;
			float _GlowBlur;
			fixed4 _GlowColor;
			fixed _GlowStrength;

			fixed4 frag(v2f IN) : SV_Target
			{
                fixed4 mainColor = tex2D(_MainTex, IN.texcoord);

                if (mainColor.a==1)
                    return 0;

				fixed4 c = frag_glow_h(_MainTex, IN.texcoord, _GlowColor*_GlowStrength, _GlowBlur*0.01);
                c *= _GlowColor;
                return c;
			}
		ENDCG
		}



		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "FiltersCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				return OUT;
			}

			sampler2D _MainTex;
			float _GlowBlur;
			fixed4 _GlowColor;
			fixed _GlowStrength;

			fixed4 frag(v2f IN) : SV_Target
			{
                fixed4 mainColor = tex2D(_MainTex, IN.texcoord);

                if (mainColor.a==1)
                    return 0;

				fixed4 c = frag_glow_v(_MainTex, IN.texcoord, _GlowColor*_GlowStrength, _GlowBlur*0.01);
                c *= _GlowColor;
                return c;
			}

		ENDCG
		}


        
		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float _AlphaSplitEnabled;

			fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D(_MainTex,uv);

				#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
				if (_AlphaSplitEnabled)
					color.a = tex2D (_AlphaTex, uv).r;
				#endif //UNITY_TEXTURE_ALPHASPLIT_ALLOWED

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
				c.rgb *= c.a;
				return c;
			}
		ENDCG
		}
	}
}