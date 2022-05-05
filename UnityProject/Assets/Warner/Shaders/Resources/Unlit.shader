Shader "Warner/Sprites/Unlit"
	{
	Properties
		{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color",Color) = (1,1,1,1)
		}

	SubShader
		{
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		Lighting Off
		ZWrite Off

		Tags
			{ 
			"Queue" = "Transparent" 
			"RenderType" = "Transparent"
			"CanUseSpriteAtlas" = "True"
			}

		Pass
			{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed4 _Color;

			struct appdata
				{
				float4 vertex: POSITION;
				half2 uv: TEXCOORD0;
				fixed4 color: COLOR;
				};

			struct v2f
				{
				float4 vertex: SV_POSITION;
				half2 uv: TEXCOORD0;
				fixed4 color: COLOR;
				};

			v2f vert(appdata data)
				{
				v2f output;
				output.vertex = UnityObjectToClipPos(data.vertex);
				output.uv = data.uv;
				output.color = data.color * _Color;
				return output;
				}

			float4 frag(v2f input): SV_Target
				{
				float4 color = tex2D(_MainTex,input.uv) * input.color;
				return color;
				}
			ENDCG
			}
		}
	}
