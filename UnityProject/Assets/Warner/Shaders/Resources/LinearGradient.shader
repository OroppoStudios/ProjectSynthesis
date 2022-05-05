Shader "Warner/Sprites/LinearGradient"
    {
    Properties
        {
        _MainTex("Texture", 2D) = "white" {}
        _TopColor("Top Color", Color) = (1,1,1,1)
        _BottomColor("Bottom Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(-0.5, 1)) = 1
        }

    SubShader
        {
        Tags
            {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            }

        Cull Off
        Lighting Off
        Blend One OneMinusSrcAlpha

        Pass
            {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
                {
                float4 position : POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color: COLOR;
                };

            struct v2f
                {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
                half4 color     : COLOR;
                };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _BottomColor;
            half4 _TopColor;
            float _Smoothness;

            v2f vert(appdata_t IN)
                {
                v2f OUT;
                OUT.position = UnityObjectToClipPos(IN.position);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                float factor = mad(OUT.position.y, -0.5, 0.5);
                factor *= 1 + _Smoothness*2;
                factor -= _Smoothness;
                factor = clamp(factor, 0, 1);
                OUT.color = lerp(_BottomColor, _TopColor, factor)*IN.color.a;

                return OUT;
                }

            half4 frag(v2f IN) : SV_Target
                {
                half4 texCol = tex2D(_MainTex, IN.uv);
                half4 c;
                c.rgb = IN.color.rgb;
                c.rgb *= texCol.a;
                c.a = texCol.a*IN.color.a;

                return c;
                }

            ENDCG
            }
        }
    }