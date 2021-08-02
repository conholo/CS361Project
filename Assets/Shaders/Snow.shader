Shader "Custom/SnowSurf"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        _SnowColor ("Snow Color", Color) = (1, 1, 1, 1)
        _SnowDirection ("Snow Direction", Vector) = (0, 1, 0, 0)
        _SnowAmount ("Snow Amount", Range(0,1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Gloss ("Gloss", Range(0, 1)) = 0
        _RimColor ("Rim Color", Color) = (0.1, 0.3, 0.6, 1.0) 
        _RimPower ("Rim Power", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float3 vertex : SV_POSITION;
            float2 uv_MainTex;
            float3 worldNormal : NORMAL;
            float3 viewDir;
            INTERNAL_DATA
        };

        float _Gloss;
        float _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)


        float _SnowAmount;
        float3 _SnowDirection;
        float4 _SnowColor;
        float4 _RimColor;
        float _RimPower;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            float snowCoverage = 1 - (dot(IN.worldNormal, normalize(_SnowDirection)) + 1) * 0.5;

            float rim = 1.0 - saturate(dot(normalize(IN.viewDir), IN.worldNormal));
            
            o.Albedo = lerp(_Color, _SnowColor, snowCoverage * _SnowAmount);
            o.Emission = _RimColor.rgb * pow(rim, _RimPower) * _SnowAmount;
            o.Metallic = _Metallic;
            o.Smoothness = _Gloss;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

