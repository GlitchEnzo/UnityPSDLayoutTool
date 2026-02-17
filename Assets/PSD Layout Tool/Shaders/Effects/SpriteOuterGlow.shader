Shader "Sprites/Effects/OuterGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,1,0,0.5)
        _GlowSize ("Glow Size", Float) = 5.0
        _GlowIntensity ("Glow Intensity", Float) = 1.0
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

            fixed4 _GlowColor;
            float _GlowSize;
            float _GlowIntensity;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                
                // Calculate glow effect
                float2 texelSize = _GlowSize / _ScreenParams.xy;
                
                // Sample surrounding pixels to create glow
                fixed glowAlpha = 0;
                int samples = 8;
                
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * 6.28318 / samples; // 2*PI / samples
                    float2 offset = float2(cos(angle), sin(angle)) * texelSize;
                    glowAlpha += tex2D(_MainTex, IN.texcoord + offset).a;
                }
                
                glowAlpha /= samples;
                
                // Create glow effect
                fixed3 glow = _GlowColor.rgb * glowAlpha * _GlowIntensity;
                
                // Combine original sprite with glow
                // If original alpha is high, show sprite; if low, show glow
                c.rgb = lerp(glow, c.rgb, c.a);
                c.a = max(c.a, glowAlpha * _GlowColor.a * _GlowIntensity);
                
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
