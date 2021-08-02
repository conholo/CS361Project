Shader "Custom/TreeSway"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [Header(Wind)]
        _Blending("Blending", Range(0, 1)) = 0
        _Amplitude("Amplitude", Range(0, 5)) = 1.5
        _AmplitudeOffset("Amplitude Offset", Range(0, 10)) = 2
        _Frequency("Frequency", Range(0, 5)) = 1.5
        _FrequencyOffset("Frequency Offset", Range(0, 10)) = 2
        _Phase("Phase", Range(0, 1)) = 1
        _WindAngle("Wind Angle", Range(0, 360)) = 0
        _WindAngleOffset("Wind Angle Offset", Range(0, 180)) = 0
        _SwayAttenuation("Sway Attenuation", float) = 10
        [Header(Noise)]
        _NoiseTexture("Noise Texture", 2D) = "white" {}
        _NoiseTextureTiling("Noise Tiling - Static(XY), Animated(ZW)", Vector) = (1,1,1,1)
        _NoisePanningSpeed("Noise Panning Speed", Vector) = (0.05, 0.03, 0, 0)
        
    }
    SubShader
    {
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }

        CGPROGRAM
        
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vert 
        #pragma target 3.0



        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        
        float _Glossiness;
        float _Metallic;
        float4 _Color;

        // Wind
        float _Blending;
        float _Amplitude;
        float _AmplitudeOffset;
        float _Frequency;
        float _FrequencyOffset;
        float _Phase;
        float _WindAngle;
        float _WindAngleOffset;
        float _SwayAttenuation;

        float4 _NoiseTextureTiling;
        float2 _NoisePanningSpeed;
        sampler2D _NoiseTexture;


        float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}
        

        void vert(inout appdata_full v, out Input i)
        {
            UNITY_INITIALIZE_OUTPUT(Input, i);

            float3 toWorld = mul(unity_ObjectToWorld, float4(float3(0,0,0), 1)).xyz;
            float2 worldSpaceUVs = float2(toWorld.x, toWorld.z);
            float2 animatedNoiseTiling = _NoiseTextureTiling.zw;
            float2 panning = 0.1 * _Time.y * _NoisePanningSpeed;
            float4 animatedWorldNoise = tex2Dlod(_NoiseTexture, float4(((worldSpaceUVs * animatedNoiseTiling) + panning), 0, 0.0));

            float rads = radians(_WindAngle + (_WindAngleOffset * (-1 + (animatedWorldNoise).r * -2.0))) * -1;
            float3 direction = float3(cos(rads), 0, sin(rads));

            float3 windObjectSpace = mul(unity_WorldToObject, float4(direction, 1)).xyz;
            float3 originObjectSpace = mul(unity_WorldToObject, float4(float3(0,0,0), 1)).xyz;

            float3 normalizedAxis = normalize(windObjectSpace - originObjectSpace);

            float2 staticNoiseTiling = _NoiseTextureTiling.xy;
            float4 staticWorldNoise = tex2Dlod(_NoiseTexture, float4((worldSpaceUVs * staticNoiseTiling), 0, 0));
            
            float3 phaseVertexPosition = v.vertex.xyz;
            float value = sin((toWorld.x + toWorld.z + _Time.y * (_Frequency + _FrequencyOffset * (staticWorldNoise).r)) * _Phase);
            float rotationAngle = radians(((((_Amplitude + (_AmplitudeOffset * (staticWorldNoise).r)) * value)) + _Blending) * (phaseVertexPosition.y / _SwayAttenuation));
            float3 rotated = RotateAroundAxis(float3(0, phaseVertexPosition.y, 0), phaseVertexPosition, normalizedAxis, rotationAngle);
            float3 rotated2 = RotateAroundAxis(float3(0, 0, 0), rotated, normalizedAxis, rotationAngle);
        	float3 localVertexOffset = (rotated2 - phaseVertexPosition) * step(0.01, phaseVertexPosition.y);
        	v.vertex.xyz += localVertexOffset;
        	v.vertex.w = 1;
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            const float4 color = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
