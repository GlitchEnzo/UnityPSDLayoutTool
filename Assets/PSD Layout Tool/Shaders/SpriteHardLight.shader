Shader "Sprites/HardLight"
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

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                
                // Hard Light blend mode calculation
                // if blend < 0.5: result = 2 * base * blend
                // if blend >= 0.5: result = 1 - 2 * (1 - base) * (1 - blend)
                fixed3 base = fixed3(0.5, 0.5, 0.5); // Assuming middle gray as base
                fixed3 blend = c.rgb;
                
                fixed3 hardLight = lerp(
                    2.0 * base * blend,
                    1.0 - 2.0 * (1.0 - base) * (1.0 - blend),
                    step(0.5, blend)
                );
                
                c.rgb = hardLight;
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
