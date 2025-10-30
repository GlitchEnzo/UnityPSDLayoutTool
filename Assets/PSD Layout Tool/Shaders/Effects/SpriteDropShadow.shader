Shader "Sprites/Effects/DropShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
        _ShadowOffset ("Shadow Offset", Vector) = (2,2,0,0)
        _ShadowBlur ("Shadow Blur", Float) = 1.0
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

        // Shadow pass - renders behind the main sprite
        Pass
        {
            Name "Shadow"
            
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            
            fixed4 _ShadowColor;
            float2 _ShadowOffset;
            float _ShadowBlur;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // Offset the vertex position for shadow
                IN.vertex.xy += _ShadowOffset;
                
                OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                
                // Apply shadow color and blur effect
                c.rgb = _ShadowColor.rgb;
                c.a *= _ShadowColor.a;
                
                // Simple blur approximation by sampling alpha
                if (_ShadowBlur > 0)
                {
                    float2 texelSize = 1.0 / _ScreenParams.xy;
                    fixed alpha = c.a;
                    
                    // Sample surrounding pixels for blur effect
                    alpha += tex2D(_MainTex, IN.texcoord + float2(texelSize.x, 0)).a * 0.2;
                    alpha += tex2D(_MainTex, IN.texcoord + float2(-texelSize.x, 0)).a * 0.2;
                    alpha += tex2D(_MainTex, IN.texcoord + float2(0, texelSize.y)).a * 0.2;
                    alpha += tex2D(_MainTex, IN.texcoord + float2(0, -texelSize.y)).a * 0.2;
                    
                    c.a = alpha * _ShadowBlur;
                }
                
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }

        // Main sprite pass - renders the sprite itself
        Pass
        {
            Name "Sprite"
            
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
        ENDCG
        }
    }
}
