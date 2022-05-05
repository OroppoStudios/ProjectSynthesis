Shader "Warner/Sprites/ColorSwap"
    {
    Properties
        {
        _Color ("MainColor", Color) = (1,1,1,1)
        _GradientTopColor("Gradient Top Color", Color) = (1,1,1,1)   
        _GradientBottomColor("Gradient Bottom Color", Color) = (1,1,1,1)
        _GradientSmoothness("Gradient Smoothness", Range(-5, 1)) = -5    
        _GradientOffset("Gradient Offset", Range(0, 1)) = 1
        [Toggle] _OverrideLineArtOverlap ("Override LineArt Overlap", Range(0, 1)) = 0
        _LineArtAlphaOverride ("LineArt Alpha Override", Range(0, 1)) = 0
        _MainTex ("Sprite Texture", 2D) = "transparent" {}
        _DataTexture("DataTexture", 2D) = "transparent" {}
        _LineArtTexture ("LineArt Texture", 2D) = "transparent" {}
        }

    SubShader
        {
        Tags
            { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent"
            }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha     

        Pass
            {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            sampler2D _DataTexture;
            sampler2D _LineArtTexture;
            float4 _MainTex_ST;
            fixed4 _Color;
            half4 _GradientTopColor;
            half4 _GradientBottomColor;
            float _GradientSmoothness;
            float _GradientOffset;
            fixed _OverrideLineArtOverlap;
            fixed _LineArtAlphaOverride;
            
            struct appdata_t
                {
                float4 vertex: POSITION;
                half2 texcoord: TEXCOORD0;
                fixed4 color: COLOR;
                };

            struct v2f
                {
                float4 pos: SV_POSITION;
                half2 uv: TEXCOORD0;
                fixed4 color: COLOR;
                };          


            v2f vert(appdata_t i)
                {
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.uv = i.texcoord;
                o.color = i.color * _Color;

                return o;
                }    


            float4 frag(v2f i) : SV_Target
                {
                if (_OverrideLineArtOverlap==1)
                    {
                    fixed4 lineArtColor = tex2D(_LineArtTexture, i.uv);
                    if (lineArtColor.a>_LineArtAlphaOverride)
                        return 0;
                    }

                fixed4 mainColor = tex2D(_MainTex, i.uv);
                //fixed4 swapColor = tex2D(_DataTexture, float2(mainColor.r*255, 0) / float2(256, 1));//(256,1) is our texture size             
                fixed4 swapColor = tex2D(_DataTexture, float2(mainColor.r, 0));

                //general gradient
                 _GradientSmoothness = 0.5-(_GradientSmoothness);
                float factor = mad(i.uv.y, -_GradientOffset, _GradientOffset);
                factor *= 1 + _GradientSmoothness*2;
                factor -= _GradientSmoothness;
                factor = clamp(factor, 0, 1);
                swapColor *= lerp(_GradientTopColor, _GradientBottomColor, factor);

                fixed4 finalColor = swapColor*i.color;
                finalColor.a = mainColor.a;
                finalColor.rgb *= mainColor.a;  
                return finalColor;
                }
            ENDCG
            } 
        }
    }