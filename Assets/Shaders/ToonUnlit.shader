Shader "Unlit/ToonUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _RampTexture("Ramp Texture", 2D) = "white" {}
        _AmbientColor("Ambient Color", Color) = (1, 1, 1, 1)
        
        [Header(Specular Reflection)]
        [HDR]
        _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        _Glossiness("Glossiness", Float) = 32
        
        [Header(Rim)]
        [HDR]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        
    }
    SubShader
    {
        Pass
        {
            Tags
			{
				"LightMode" = "ForwardBase"
				"PassFlags" = "OnlyDirectional"
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
			#include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldNormal : NORMAL;
                float3 viewDirection : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDirection = WorldSpaceViewDir(v.vertex);
                return o;
            }
            
            sampler2D _RampTexture;
            float4 _Color;
            float4 _AmbientColor;

            float _Glossiness;
            float4 _SpecularColor;
            
            float4 _RimColor;
            float _RimAmount;

            float4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDirection = normalize(i.viewDirection);
                
                float NdotL = saturate(dot(normal, _WorldSpaceLightPos0));
                float2 rampUV = float2((NdotL * 0.5 + 0.5), 0.5);
                float rampSample = tex2D(_RampTexture, rampUV);
                float4 globalLight = rampSample * _LightColor0;

                float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDirection);
                float NdotH = dot(normal, halfVector);

                float specularIntensity = pow(NdotH * rampSample, _Glossiness * _Glossiness);
                float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
                float4 specular = specularIntensitySmooth * _SpecularColor;
                

                float4 rimDot = 1 - dot(viewDirection, normal);
                float rimIntensity = smoothstep(1 - _RimAmount - 0.01, 1 - _RimAmount + 0.01, rimDot);
                float4 rim = rimIntensity * _RimColor;
                
                float4 sample = tex2D(_MainTex, i.uv);
                
                
                return sample * _Color * (_AmbientColor + globalLight + specular + rim);
            }
            ENDCG
        }
    }
}
