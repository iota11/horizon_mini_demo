Shader "HorizonMini/VolumeBackface"
{
    Properties
    {
        _Color ("Color", Color) = (0.5, 0.5, 0.5, 0.1)
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 0.5)
        _EdgeWidth ("Edge Width", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Pass 1: Draw backfaces only
        Pass
        {
            Name "Backfaces"
            Cull Front  // Only render back faces
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float4 _Color;
            float4 _EdgeColor;
            float _EdgeWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fresnel-like edge highlight
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fresnel = 1.0 - abs(dot(viewDir, normalize(i.worldNormal)));
                fresnel = pow(fresnel, 2);

                // Mix base color with edge color
                fixed4 finalColor = lerp(_Color, _EdgeColor, fresnel);

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
