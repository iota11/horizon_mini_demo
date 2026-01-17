Shader "HorizonMini/GridPlane_URP"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (1, 1, 1, 0.5)
        _PlaneColor ("Plane Color", Color) = (0.2, 0.5, 1.0, 0.2)
        _GridSize ("Grid Size", Float) = 0.5
        _GridThickness ("Grid Thickness", Float) = 0.02
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
            Name "GridPlane"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GridColor;
                half4 _PlaneColor;
                float _GridSize;
                float _GridThickness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Use world position for grid
                float2 pos = IN.positionWS.xz;

                // Calculate grid lines
                float2 grid = abs(frac(pos / _GridSize - 0.5) - 0.5) / fwidth(pos / _GridSize);
                float lineValue = min(grid.x, grid.y);

                // Threshold for grid lines
                float gridMask = 1.0 - saturate(lineValue - _GridThickness);

                // Mix grid color and plane color
                half4 color = lerp(_PlaneColor, _GridColor, gridMask);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
