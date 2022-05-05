Shader "Warner/Sprites/LineArt"
    {
    Properties
        {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color",Color) = (1,1,1,1)
        [Toggle] _OverrideColor ("OverrideColor", Range(0, 1)) = 0
        _AlphaCorrection ("AlphaCorrection", Range(0, 1)) = 0
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
            fixed _AlphaCorrection;
            fixed _OverrideColor;

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

            v2f vert(appdata i)
                {
                v2f output;
                output.vertex = UnityObjectToClipPos(i.vertex);
                output.uv = i.uv;
                output.color = i.color * _Color;
                return output;
                }

            float4 frag(v2f i): SV_Target
                {
                float4 currentPixel = tex2D(_MainTex, i.uv);

                if (currentPixel.a>0.2)
                    {
                    currentPixel.a += _AlphaCorrection;
                    if (currentPixel.a>1)
                        currentPixel.a = 1;
                    }

                if (_OverrideColor>0)
                    return i.color*currentPixel.a;
                    else
                    return currentPixel;
                }
            ENDCG
            }
        }
    }
