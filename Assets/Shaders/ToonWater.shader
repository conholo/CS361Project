Shader "Custom/ToonWater"
{
    Properties
    {
        _DepthGradientShallow("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)
        _DepthGradientDeep("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _DepthMaxDistance("Depth Maximum Distance", Range(0, 10)) = 1
        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _FoamDistance("Foam Distance", Range(0, 10)) = 0.4
        _SurfaceNoiseScrollX("Noise Scroll Direction X", Range(0, 1)) = 0.1
        _SurfaceNoiseScrollY("Noise Scroll Direction Y", Range(0, 1)) = 0.1
        _SurfaceDistortion("Surface Distortion", 2D) = "white" {}	
        _SurfaceDistortionAmount("Surface Distortion Amount", Range(0, 1)) = 0.27
        _FromDirection("From Direction", Vector) = (0, 0, 0, 0)
        _ScrollSpeed("Scroll Speed", Range(-1, 1)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Transparent" }

        Pass
        {
            
            Blend SrcAlpha OneMinusSrcAlpha
            Zwrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            float4 AlphaBlend(float4 top, float4 bottom)
            {
	            float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
	            float alpha = top.a + bottom.a * (1 - top.a);

	            return float4(color, alpha);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 noiseUV : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPosition: TEXCOORD1;
                float2 distortUV : TEXCOORD2;
            };

            // Depth 
            float4 _DepthGradientShallow;
            float4 _DepthGradientDeep;
            float _DepthMaxDistance;
            sampler2D _CameraDepthTexture;

            // Surface Noise
            sampler2D _SurfaceNoise;
            float4 _SurfaceNoise_ST;
            float _SurfaceNoiseCutoff;

            // Foam
            float4 _FoamColor;
            float _FoamDistance;

            // Scroll
            float2 _SurfaceNoiseScroll;
            float _SurfaceNoiseScrollX;
            float _SurfaceNoiseScrollY;
            float _ScrollSpeed;
            
            // Distortion
            sampler2D _SurfaceDistortion;
            float4 _SurfaceDistortion_ST;
            float _SurfaceDistortionAmount;

            float2 _FromDirection;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.vertex);
                o.noiseUV = TRANSFORM_TEX(v.uv + _FromDirection, _SurfaceNoise);
                o.distortUV = TRANSFORM_TEX(v.uv, _SurfaceDistortion);
               
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float existingDepth01 = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPosition)).r;
                const float existingDepthLinear = LinearEyeDepth(existingDepth01);

                const float depthDifference = existingDepthLinear - i.screenPosition.w;
                const float waterDepthDifference01 = saturate(depthDifference / _DepthMaxDistance);
                
                float4 waterColor = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepthDifference01);

                float foamDepthDifference01 = saturate(depthDifference / _FoamDistance);
                float surfaceNoiseCutoff = foamDepthDifference01 * _SurfaceNoiseCutoff;

                float2 distortSample = (tex2D(_SurfaceDistortion, i.distortUV).xy * 2 - 1) * _SurfaceDistortionAmount;
                
                float2 noiseUV = float2(
                    (i.noiseUV.x + _SurfaceNoiseScrollX + _Time.y * _ScrollSpeed) + distortSample.x,
                    (i.noiseUV.y + _SurfaceNoiseScrollY + _Time.y * _ScrollSpeed) + distortSample.y);
                
                float surfaceNoiseSample = tex2D(_SurfaceNoise, noiseUV).r;

                float surfaceNoise = smoothstep(surfaceNoiseCutoff - 0.01, surfaceNoiseCutoff + 0.01, surfaceNoiseSample);
                float4 surfaceNoiseColor = _FoamColor;
                surfaceNoiseColor.a *= surfaceNoise;

                return AlphaBlend(surfaceNoiseColor, waterColor);
            }
            ENDCG
        }
    }
}
