Shader "HorizonMini/VolumeGridLit"
{
    Properties
    {
        _MainTex ("Grid Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _GridScale ("Grid Scale (cells per meter)", Float) = 1.0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _OcclusionStrength ("AO Strength", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Cull Front  // Only render back faces

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _Color;
        float _GridScale;
        half _Glossiness;
        half _Metallic;
        half _OcclusionStrength;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate world-space UVs (triplanar)
            float3 absNormal = abs(IN.worldNormal);
            float2 worldUV;

            if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
            {
                // X-axis dominant (YZ plane)
                worldUV = IN.worldPos.yz * _GridScale;
            }
            else if (absNormal.y > absNormal.z)
            {
                // Y-axis dominant (XZ plane)
                worldUV = IN.worldPos.xz * _GridScale;
            }
            else
            {
                // Z-axis dominant (XY plane)
                worldUV = IN.worldPos.xy * _GridScale;
            }

            // Sample texture
            fixed4 c = tex2D(_MainTex, worldUV) * _Color;

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Occlusion = lerp(1.0, c.a, _OcclusionStrength);
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
