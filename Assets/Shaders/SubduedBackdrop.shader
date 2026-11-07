Shader "Zanshin/SubduedBackdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _Saturation ("Saturation",Range(0,1))=.45
        _Brightness ("Brightness",Range(0,1))=.7
        _Contrast ("Contrast",Range(0,1))=.8
        [HideInInspector] _RendererColor ("Renderer Color",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Saturation,_Brightness,_Contrast;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings output=CommonUnlitVertex(input);
                output.color=input.color*_Color*unity_SpriteColor;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*input.color;
                half luminance=dot(color.rgb,half3(.2126,.7152,.0722));
                color.rgb=lerp(luminance.xxx,color.rgb,_Saturation);
                color.rgb=saturate((color.rgb-.5)*_Contrast+.5)*_Brightness;
                color.rgb*=half3(.95,.97,1);
                return color;
            }
            ENDHLSL
        }
    }
}
