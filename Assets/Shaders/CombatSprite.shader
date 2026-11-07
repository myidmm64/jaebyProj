Shader "Zanshin/CombatSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Accent ("Energy", Color) = (0.4,0.9,1,1)
        _Flash ("Impact Flash", Range(0,1)) = 0
        _Charge ("Energy Rim", Range(0,1)) = 0
        _Dissolve ("Pixel Dissolve", Range(0,1)) = 0
        [HideInInspector] _SpriteMode ("Sprite Mode", Float) = 1
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Accent;
                float4 _MainTex_TexelSize;
                float _Flash, _Charge, _Dissolve, _SpriteMode;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tint = input.color * _Color;
                if (_SpriteMode > 0.5)
                {
                    SetUpSpriteInstanceProperties();
                    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                    tint *= unity_SpriteColor;
                }
                Varyings output = CommonUnlitVertex(input);
                output.color = tint;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*input.color;
                float2 cell = floor(input.uv*64);
                float noise = frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);
                clip(noise + 0.001 - _Dissolve);
                if (_Charge > 0.001)
                {
                    float2 stepUV = _MainTex_TexelSize.xy;
                    half neighbor = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(stepUV.x,0)).a;
                    neighbor = min(neighbor,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(stepUV.x,0)).a);
                    neighbor = min(neighbor,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(0,stepUV.y)).a);
                    neighbor = min(neighbor,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(0,stepUV.y)).a);
                    color.rgb += _Accent.rgb * saturate(color.a-neighbor) * _Charge * (0.7+0.3*sin(_Time.y*5));
                }
                color.rgb = lerp(color.rgb,half3(1,0.96,0.88),_Flash);
                return color;
            }
            ENDHLSL
        }
    }
}
