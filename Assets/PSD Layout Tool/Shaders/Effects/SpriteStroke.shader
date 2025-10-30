Shader "Sprites/Effects/Stroke"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StrokeColor ("Stroke Color", Color) = (1,0,0,1)
        _StrokeWidth ("Stroke Width", Float) = 2.0
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

            fixed4 _StrokeColor;
            float _StrokeWidth;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                
                // Calculate stroke effect by sampling neighboring pixels
                float2 texelSize = _StrokeWidth / _ScreenParams.xy;
                
                // Sample 8 directions around current pixel
                fixed strokeAlpha = 0;
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(texelSize.x, 0)).a);
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(-texelSize.x, 0)).a);
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(0, texelSize.y)).a);
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -texelSize.y)).a);
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(texelSize.x, texelSize.y)).a);
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(-texelSize.x, texelSize.y)).a);
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(texelSize.x, -texelSize.y)).a);
                strokeAlpha = max(strokeAlpha, tex2D(_MainTex, IN.texcoord + float2(-texelSize.x, -texelSize.y)).a);
                
                // Create stroke where there's no original sprite but neighboring pixels exist
                fixed3 stroke = _StrokeColor.rgb * (strokeAlpha - c.a) * _StrokeColor.a;
                stroke = max(stroke, 0); // Clamp to positive values
                
                // Combine original sprite with stroke
                c.rgb = lerp(stroke, c.rgb, c.a);
                c.a = max(c.a, strokeAlpha * _StrokeColor.a);
                
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
