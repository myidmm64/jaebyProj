Shader "Zanshin/UI/CardFrame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Grade ("Rarity", Range(0,3)) = 0
        _Accent ("Highlight", Color) = (0.8,0.9,1,1)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Attributes { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 localPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd, _Accent;
            float4 _MainTex_TexelSize, _ClipRect;
            float _Grade, _CardTime;
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition=input.vertex;
                output.vertex=UnityObjectToClipPos(input.vertex);
                output.uv=input.uv;
                output.color=input.color*_Color;
                return output;
            }
            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed4 color=(tex2D(_MainTex,input.uv)+_TextureSampleAdd)*input.color;
                float2 pixels=floor(input.uv*_MainTex_TexelSize.zw);
                float2 edge=min(pixels,_MainTex_TexelSize.zw-1-pixels);
                float frame=(1-step(17,min(edge.x,edge.y)))*step(.18,max(color.r,max(color.g,color.b)));
                float2 uv=floor(pixels/4)*4*_MainTex_TexelSize.xy;
                float sweep=1-step(.035,abs(frac(uv.x*.55+uv.y*.8-_CardTime*.09)-.5));
                float strength=.035+step(.5,_Grade)*.12+step(1.5,_Grade)*.06;
                if(_Grade>2.5)
                {
                    float hue=uv.x*.7+uv.y*.4+_CardTime*.055;
                    float3 rainbow=saturate(abs(frac(hue+float3(0,2.0/3,1.0/3))*6-3)-1);
                    color.rgb=lerp(color.rgb,color.rgb*(.65+rainbow*.85),frame*.72);
                }
                float glint=step(.985,sin(dot(floor(pixels/4),float2(1.7,2.3))+_CardTime*1.15));
                glint*=step(1.5,_Grade)*.08;
                color.rgb=saturate(color.rgb+frame*_Accent.rgb*(sweep*strength+glint));
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(input.localPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
