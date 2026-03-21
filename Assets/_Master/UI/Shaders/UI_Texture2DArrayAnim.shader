Shader "UI/Texture2DArrayAnim"
{
    Properties
    {
        // --- DUMMY TEXTURE ĐỂ LỪA UNITY CANVAS ---
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {} 
        
        // --- TEXTURE THẬT CỦA CHÚNG TA ---
        _MainTexArray ("Texture Array", 2DArray) = "white" {}
        
        _SliceIndex ("Slice Index", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)

        // --- CÁC THUỘC TÍNH BẮT BUỘC CỦA UNITY UI ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            "RenderPipeline" = "UniversalPipeline"
        }

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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            TEXTURE2D_ARRAY(_MainTexArray);
            SAMPLER(sampler_MainTexArray);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _SliceIndex;
                float4 _ClipRect;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.worldPosition = input.positionOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Lấy màu từ biến Array thay vì MainTex
                half4 color = SAMPLE_TEXTURE2D_ARRAY(_MainTexArray, sampler_MainTexArray, input.uv, _SliceIndex) * input.color;

                // Xử lý RectMask2D (UI Clip)
                #ifdef UNITY_UI_CLIP_RECT
                    float2 clipFactor = step(_ClipRect.xy, input.worldPosition.xy) * step(input.worldPosition.xy, _ClipRect.zw);
                    color.a *= clipFactor.x * clipFactor.y;
                #endif

                // Xử lý Alpha Clip
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}