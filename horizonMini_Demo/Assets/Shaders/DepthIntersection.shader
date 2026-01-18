Shader "HorizonMini/DepthIntersection"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.3, 0.6, 1, 0.15)
        _GridColor ("Grid Color", Color) = (1, 1, 1, 0.6)
        _GridSize ("Grid Size", Float) = 0.5
        _GridThickness ("Grid Thickness", Float) = 0.02

        _IntersectionColor ("Intersection Color", Color) = (0, 1, 1, 1)
        _IntersectionThickness ("Intersection Thickness", Float) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DepthIntersectionPass"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GridColor;
                float _GridSize;
                float _GridThickness;
                half4 _IntersectionColor;
                float _IntersectionThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(positionInputs.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Grid pattern
                float2 worldXZ = input.positionWS.xz;
                float2 grid = abs(frac(worldXZ / _GridSize - 0.5) - 0.5) / fwidth(worldXZ / _GridSize);
                float lineValue = min(grid.x, grid.y);
                float gridMask = 1.0 - saturate(lineValue - _GridThickness);

                // Base color with grid
                half4 color = lerp(_BaseColor, _GridColor, gridMask);

                // Depth intersection
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // Sample scene depth
                float sceneDepth = SampleSceneDepth(screenUV);
                float sceneDepthEye = LinearEyeDepth(sceneDepth, _ZBufferParams);

                // Current surface depth
                float surfaceDepthEye = LinearEyeDepth(input.positionHCS.z, _ZBufferParams);

                // Calculate depth difference
                float depthDiff = abs(sceneDepthEye - surfaceDepthEye);

                // Create intersection highlight
                float intersection = 1.0 - saturate(depthDiff / _IntersectionThickness);
                intersection = pow(intersection, 2); // Sharpen the edge

                // Only show intersection when plane is behind geometry
                intersection *= step(surfaceDepthEye, sceneDepthEye);

                // Blend intersection color
                color.rgb = lerp(color.rgb, _IntersectionColor.rgb, intersection * _IntersectionColor.a);
                color.a = max(color.a, intersection * _IntersectionColor.a);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
