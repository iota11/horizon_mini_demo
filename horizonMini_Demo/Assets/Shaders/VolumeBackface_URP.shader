Shader "HorizonMini/VolumeBackface_URP"
{
    Properties
    {
        _Color ("Color", Color) = (0.5, 0.5, 0.5, 0.1)
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 0.5)
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "VolumeBackface"
            Cull Front  // Only render back faces
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                half _EdgeWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Fresnel-like edge highlight
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float fresnel = 1.0 - abs(dot(viewDir, normalize(input.normalWS)));
                fresnel = pow(fresnel, 2) * _EdgeWidth;

                // Mix base color with edge color
                half4 finalColor = lerp(_Color, _EdgeColor, fresnel);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
