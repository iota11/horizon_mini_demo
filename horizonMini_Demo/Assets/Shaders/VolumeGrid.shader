Shader "HorizonMini/VolumeGrid"
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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "VolumeGrid"
            Cull Front  // Only render back faces (inside view)
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _GridScale;
            half _AlphaClip;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                // Calculate world-space UVs for consistent grid scale
                // Use world position to generate UVs instead of mesh UVs
                float3 absNormal = abs(o.worldNormal);
                float2 worldUV;

                // Triplanar mapping based on dominant normal axis
                if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                {
                    // X-axis dominant (YZ plane)
                    worldUV = o.worldPos.yz * _GridScale;
                }
                else if (absNormal.y > absNormal.z)
                {
                    // Y-axis dominant (XZ plane)
                    worldUV = o.worldPos.xz * _GridScale;
                }
                else
                {
                    // Z-axis dominant (XY plane)
                    worldUV = o.worldPos.xy * _GridScale;
                }

                o.uv = worldUV;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample grid texture with world-space UVs
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // Apply tint color
                fixed4 finalColor = texColor * _Color;

                // Optional alpha clipping for grid lines
                clip(texColor.a - _AlphaClip);

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
