Shader "HorizonMini/VolumeGrid_URP"
{
    Properties
    {
        _MainTex ("Grid Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _GridScale ("Grid Scale (cells per meter)", Float) = 1.0
        _AlphaClip ("Alpha Clip", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "VolumeGrid"
            Cull Front  // Only render back faces (inside view)
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
                float _GridScale;
                half _AlphaClip;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;

                // Calculate world-space UVs for consistent grid scale
                // Use world position to generate UVs instead of mesh UVs
                float3 absNormal = abs(output.normalWS);
                float2 worldUV;

                // Triplanar mapping based on dominant normal axis
                if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                {
                    // X-axis dominant (YZ plane)
                    worldUV = output.positionWS.yz * _GridScale;
                }
                else if (absNormal.y > absNormal.z)
                {
                    // Y-axis dominant (XZ plane)
                    worldUV = output.positionWS.xz * _GridScale;
                }
                else
                {
                    // Z-axis dominant (XY plane)
                    worldUV = output.positionWS.xy * _GridScale;
                }

                output.uv = worldUV;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample grid texture with world-space UVs
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Apply tint color
                half4 finalColor = texColor * _Color;

                // Optional alpha clipping for grid lines
                clip(texColor.a - _AlphaClip);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
