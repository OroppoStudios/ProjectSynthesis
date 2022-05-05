Shader "Warner/Sprites/Background"
	{
    Properties
        {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurAmount("Blur Amount",Range(0,0.02)) = 0
        _LightMix("Light Mix",Range(0,1)) = 1
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
                     
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert nofog nolightmap nodynlightmap keepalpha noinstancing
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        #include "FiltersCG.cginc"

        sampler2D _MainTex;
        float _BlurAmount;
        fixed4 _Color;
        fixed _LightMix;

        struct Input
            {
            float2 uv_MainTex;
            fixed4 color;
            };

        void vert (inout appdata_full v, out Input o)
            {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color;
            }

        fixed4 SampleSpriteTexture (float2 uv)
            {
            fixed4 color = tex2D(_MainTex,uv)*0.34;
            color += frag_blur_h(_MainTex,uv,_BlurAmount)*0.33;
            color += frag_blur_v(_MainTex,uv,_BlurAmount)*0.33;

            #if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
            if (_AlphaSplitEnabled)
                color.a = tex2D (_AlphaTex, uv).r; 
            #endif

            return color;
            }

        void surf (Input IN, inout SurfaceOutput o)
            {
            fixed4 c = SampleSpriteTexture (IN.uv_MainTex) * IN.color;

            clip(c.a==0);

            o.Albedo = c.rgb * c.a * _LightMix;
            o.Alpha = c.a;
            }
        ENDCG
        }
    }