Shader "Abel/Instanced/HealthBar"
{
    Properties
    {
        // Define 3 color states for the health bar
        _ColorHigh ("HP High (Green)", Color) = (0.2, 0.8, 0.2, 1) 
        _ColorMed ("HP Medium (Yellow)", Color) = (1.0, 0.8, 0.0, 1) 
        _ColorLow ("HP Low (Red)", Color) = (0.8, 0.1, 0.1, 1) 
        
        _ColorBG ("Background Color", Color) = (0.2, 0.2, 0.2, 1) // Dark Gray
        _BorderColor ("Border Color", Color) = (0.0, 0.0, 0.0, 1) // Black
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry+1" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorHigh;
                half4 _ColorMed;
                half4 _ColorLow;
                half4 _ColorBG;
                half4 _BorderColor;
            CBUFFER_END

            // --- INSTANCED DATA: Each health bar has its own HP percentage ---
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _HPPercent)
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
                
                // Get HP percentage from C# (0.0 to 1.0)
                float hp = UNITY_ACCESS_INSTANCED_PROP(Props, _HPPercent);
                
                // 1. Draw Border (Thick horizontal, thin vertical)
                float borderX = 0.02;
                float borderY = 0.1;
                if (input.uv.x < borderX || input.uv.x > 1.0 - borderX || 
                    input.uv.y < borderY || input.uv.y > 1.0 - borderY)
                {
                    return _BorderColor;
                }

                // 2. Draw HP Bar with Dynamic Color
                if (input.uv.x < hp)
                {
                    // Calculate dynamic color based on HP
                    // If HP is 0.0 to 0.5: Transition from Low(Red) to Med(Yellow)
                    // If HP is 0.5 to 1.0: Transition from Med(Yellow) to High(Green)
                    
                    half4 currentHPColor = lerp(_ColorLow, _ColorMed, saturate(hp * 2.0));
                    currentHPColor = lerp(currentHPColor, _ColorHigh, saturate((hp - 0.5) * 2.0));
                    
                    return currentHPColor;
                }
                
                // 3. Draw Background for lost HP
                return _ColorBG;
            }
            ENDHLSL
        }
    }
}