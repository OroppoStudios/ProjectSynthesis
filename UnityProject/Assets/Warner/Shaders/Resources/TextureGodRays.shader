Shader "Warner/Sprites/TextureGodRays" 
    {
    Properties 
        {
        _Color ("Color",Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _NoiseTex ("Noise (RGB) Trans (A)", 2D) = "white" {}
        _BlurAmount("Blur Amount",Range(0,0.02)) = 0        
        _Mix("Mix", Float) = 0.05
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
            #include "FiltersCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _BlurAmount;

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

            fixed4 SampleSpriteTexture (float2 uv)
                {
                fixed4 color = tex2D(_MainTex,uv)*0.34;
                color += frag_blur_h(_MainTex,uv,_BlurAmount)*0.33;
                color += frag_blur_v(_MainTex,uv,_BlurAmount)*0.33;

                return color;
                }

            float4 frag(v2f input): SV_Target
                {
                fixed4 c = SampleSpriteTexture (input.uv) * input.color;

                //clip(c.a==0);

                return c;
                }
            ENDCG
            }
        }
    }
