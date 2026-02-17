Shader "Sprites/Effects/GradientOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _GradientAngle ("Gradient Angle", Float) = 90
        _GradientScale ("Gradient Scale", Float) = 1.0
        _GradientOpacity ("Gradient Opacity", Float) = 0.5
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

            sampler2D _GradientTex;
            float _GradientAngle;
            float _GradientScale;
            float _GradientOpacity;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                
                // Calculate gradient UV based on angle
                float angleRad = _GradientAngle * 3.14159 / 180.0;
                float cosAngle = cos(angleRad);
                float sinAngle = sin(angleRad);
                
                // Transform UV coordinates based on gradient angle
                float2 centeredUV = IN.texcoord - 0.5;
                float gradientU = (centeredUV.x * cosAngle - centeredUV.y * sinAngle) * _GradientScale + 0.5;
                
                // Sample gradient texture
                fixed4 gradientColor = tex2D(_GradientTex, float2(gradientU, 0.5));
                
                // Apply gradient overlay
                c.rgb = lerp(c.rgb, gradientColor.rgb, _GradientOpacity * gradientColor.a);
                
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
