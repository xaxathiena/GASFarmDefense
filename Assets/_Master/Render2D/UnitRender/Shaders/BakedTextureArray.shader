Shader "Abel/Instanced/BakedTextureArray"
{
    Properties
    {
        _MainTexArray ("Texture Array", 2DArray) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.0 // Default is 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off // Render both sides
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            TEXTURE2D_ARRAY(_MainTexArray);
            SAMPLER(sampler_MainTexArray);

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _FrameIndex)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Get frame index from C# script
                float frame = UNITY_ACCESS_INSTANCED_PROP(Props, _FrameIndex);
                
                // Sample Texture Array
                half4 col = SAMPLE_TEXTURE2D_ARRAY(_MainTexArray, sampler_MainTexArray, input.uv, frame);
                
                // --- ANTI-INVISIBLE LOGIC ---
                // Discard pixels with low alpha to create the cutout effect
                if (col.a < 0.1) 
                {
                   clip(-1); 
                }

                // Force Alpha to 1.0 to prevent Unity from blending it semi-transparently
                return half4(col.rgb, 1.0); 
            }
            ENDHLSL
        }
    }
}