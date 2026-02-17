Shader "Sprites/Hue"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            // RGB to HSV conversion
            fixed3 rgb2hsv(fixed3 c)
            {
                fixed4 K = fixed4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                fixed4 p = lerp(fixed4(c.bg, K.wz), fixed4(c.gb, K.xy), step(c.b, c.g));
                fixed4 q = lerp(fixed4(p.xyw, c.r), fixed4(c.r, p.yzx), step(p.x, c.r));

                fixed d = q.x - min(q.w, q.y);
                fixed e = 1.0e-10;
                return fixed3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            // HSV to RGB conversion
            fixed3 hsv2rgb(fixed3 c)
            {
                fixed4 K = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                
                // Hue blend mode calculation
                fixed3 base = fixed3(0.5, 0.5, 0.5); // Assuming middle gray as base
                fixed3 blend = c.rgb;
                
                fixed3 baseHSV = rgb2hsv(base);
                fixed3 blendHSV = rgb2hsv(blend);
                
                // Use hue from blend, saturation and value from base
                fixed3 resultHSV = fixed3(blendHSV.x, baseHSV.y, baseHSV.z);
                fixed3 hue = hsv2rgb(resultHSV);
                
                c.rgb = hue;
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
