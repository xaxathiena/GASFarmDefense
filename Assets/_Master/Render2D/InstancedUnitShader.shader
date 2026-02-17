Shader "Abel/InstancedUnit_Smooth"
{
    Properties
    {
        _MainTexArray ("Texture Array", 2DArray) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.0 // Mặc định 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off // Vẽ 2 mặt
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
                
                // Lấy frame từ C# gửi xuống
                float frame = UNITY_ACCESS_INSTANCED_PROP(Props, _FrameIndex);
                
                // Sample Texture
                half4 col = SAMPLE_TEXTURE2D_ARRAY(_MainTexArray, sampler_MainTexArray, input.uv, frame);
                
                // --- LOGIC CHỐNG TÀNG HÌNH ---
                // Tuyệt đối KHÔNG dùng hàm clip()
                // Nếu pixel này trong suốt (Alpha = 0), ta cho nó thành màu ĐEN (hoặc ĐỎ) để nhìn thấy nó chiếm chỗ thế nào.
                
                if (col.a < 0.1) 
                {
                   // return half4(1, 0, 0, 0.5); // Bật dòng này nếu muốn thấy phần trong suốt thành màu đỏ mờ
                   clip(-1); // Tạm thời vẫn clip để viền đẹp, NHƯNG...
                }

                // ... Ta ép Alpha = 1 để chắc chắn nó không bị Unity làm mờ
                return half4(col.rgb, 1.0); 
            }
            ENDHLSL
        }
    }
}