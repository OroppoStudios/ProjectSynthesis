Shader "Warner/Sprites/Shadow"
    {
    Properties
        {
        [Toggle] _ShadowEnabled ("ShadowEnabled", Range(0, 1)) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _MainTex ("Sprite Texture", 2D) = "transparent" {}
        _Smoothness("Smoothness", Range(-0.5, 1)) = 1
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Blur ("Glow Blur",Range(0,1)) = 0.45
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowStrength("Glow Strength",Range(1,2)) = 1.1
        _FlipAmount("FlipAmount", Range(0, 1)) = 0
        _HorizontalPosition ("ShadowHorizontalPosition", Range(-4, 4)) = 0
        _VerticalPosition ("ShadowVerticalPosition", Range(-4, 4)) = 2
        _HorizontalSkew ("ShadowHorizontalSkew", Range(-4, 4)) = 0
        _VerticalSkew ("ShadowVerticalSkew", Range(-4, 4)) = 0
        _RotationDegrees ("ShadowRotationDegrees", Range(-1, 1)) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha

         Pass
            {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "FiltersCG.cginc"

            sampler2D _MainTex;
            float _ShadowEnabled;
            half4 _Color;
            float _Smoothness;
            float _Blur;
            fixed4 _GlowColor;
            fixed _GlowStrength;
            float _FlipAmount;
            float _HorizontalPosition;
            float _VerticalPosition;
            float _VerticalSkew;
            float _HorizontalSkew;
            float _RotationDegrees; 
        
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

            v2f vert(appdata input)
                {
                v2f output;

                if (!_ShadowEnabled)
                    {
                    UNITY_INITIALIZE_OUTPUT(v2f, output);
                    return output;
                    }

                //postition and skew
                float flipMultiplier = _FlipAmount * 2 - 1;
                input.vertex.y *= -(1 + _VerticalSkew);
                input.vertex.y -= _VerticalPosition;
                input.vertex.x -= input.vertex.y * _HorizontalSkew * flipMultiplier + _HorizontalPosition * flipMultiplier;

                //rotation                                       
                float s = sin (_RotationDegrees);
                float c = cos (_RotationDegrees);
                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                rotationMatrix *=0.5;
                rotationMatrix +=0.5;
                rotationMatrix = rotationMatrix*2-1;
                input.uv.xy -=0.5; 
                input.uv.xy = mul (input.uv.xy, rotationMatrix);
                input.uv.xy += 0.5;

                //out
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color*_Color;

                return output;
                }

            half4 frag(v2f input): SV_Target
                {
                if (_ShadowEnabled)
                    {
                    fixed4 color = tex2D(_MainTex, input.uv)*0.40;
                    color += frag_blur_h(_MainTex, input.uv, _Blur*0.01)*0.40;
                    color += frag_blur_v(_MainTex, input.uv, _Blur*0.01)*0.20;

                    return color*input.color;
                    }
                    else
                    return float4(0,0,0,0);
                }
            ENDCG
            }
        }
    }