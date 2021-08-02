Shader "Hidden/Clouds"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewVector : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0));
                return o;
            }
            
            Texture2D<float4> _WeatherMap;
            Texture3D<float4> _WorleyNoiseMap;
            Texture2D<float4> _BlueNoise;
            
            SamplerState sampler_WeatherMap;
            SamplerState sampler_WorleyNoiseMap;
            SamplerState sampler_BlueNoise;

            sampler2D _CameraDepthTexture;

            // Debug
            float _RayOffsetStrength;
            float _MapDisplaySize;
            float _TileAmount;
            float _SliceDepth;
            int _MapDisplayType;

            float _Speed;
            float _FallOffDistance;
            
            // Shape
            float3 _BoundsMin;
            float3 _BoundsMax;
            float _CloudScale;
            float3 _ShapeOffset;
            float4 _ShapeNoiseWeights;
            
            // Light
            float4 _PhaseParams;
            float _DensityOffset;
            float _DensityMultiplier;
            float _LightStepsCount;
            float _LightAbsorptionTowardsSun;
            float _LightAbsorptionThroughCloud;
            float _DarknessThreshold;

            float _FogDistanceThreshold;

            
            float4 _SkyColorA;
            float4 _SkyColorB;

            
            float2 squareUV(float2 uv)
            {
                const float width = _ScreenParams.x;
                const float height =_ScreenParams.y;
                const float scale = 1000;
                const float x = uv.x * width;
                const float y = uv.y * height;
                return float2 (x/scale, y/scale);
            }

            // Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
            float2 RayBoxDistance(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 invRaydir)
            {
                // Adapted from: http://jcgt.org/published/0007/03/04/
                float3 t0 = (boundsMin - rayOrigin) * invRaydir;
                float3 t1 = (boundsMax - rayOrigin) * invRaydir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(tmax.x, min(tmax.y, tmax.z));

                // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
                // dstA is dst to nearest intersection, dstB dst to far intersection

                // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
                // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

                // CASE 3: ray misses box (dstA > dstB)

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }

            // Henyey-Greenstein
            float HG(float a, float g)
            {
                float g2 = g * g;
                return (1 - g2) / (4 * 3.1415 * pow(1 + g2 - 2 * g * (a), 1.5));
            }

            float Beer(float d)
            {
                const float beer = exp(-d);
                return beer;
            }
            
            float Phase(float a)
            {
                float blend = .5;
                float hgBlend = HG(a,_PhaseParams.x) * (1 - blend) + HG(a, - _PhaseParams.y) * blend;
                return _PhaseParams.z + hgBlend * _PhaseParams.w;
            }

            float Remap(float v, float minOld, float maxOld, float minNew, float maxNew)
            {
                return minNew + (v-minOld) * (maxNew - minNew) / (maxOld-minOld);
            }

            float Remap01(float v, float low, float high)
            {
                return (v-low)/(high-low);
            }

            float4 DrawNoise(float2 uv)
            {
                float4 channels = 0;
                float3 samplePosition = float3(uv, _SliceDepth);

                if(_MapDisplayType == 1)
                    channels = _WorleyNoiseMap.SampleLevel(sampler_WorleyNoiseMap, samplePosition, 0);
                else if (_MapDisplayType == 2)
                    channels = _WeatherMap.SampleLevel(sampler_WeatherMap, samplePosition.xy, 0);

                return channels;
            }

            float SampleDensity(float3 rayPosition)
            {
                const int mipLevel = 0;
                const float baseScale = 1 / 1000.0;
                const float offsetSpeed = 1 / 100.0;

                float3 size = _BoundsMax - _BoundsMin;
                float3 boundsCenter = (_BoundsMin + _BoundsMax) * 0.5;
                float3 uvWorld = (size * 0.5 + rayPosition) * baseScale * _CloudScale;
                float3 shapeSamplePosition = uvWorld + _ShapeOffset * offsetSpeed + float3(_Time.y, _Time.y * 0.1, _Time.y * 0.2) * _Speed;

                float distanceFromEdgeX = min(_FallOffDistance, min(rayPosition.x - _BoundsMin.x, _BoundsMax.x - rayPosition.x));
                float distanceFromEdgeZ = min(_FallOffDistance, min(rayPosition.z - _BoundsMin.z, _BoundsMax.z - rayPosition.z));
                float edgeWeight = min(distanceFromEdgeZ, distanceFromEdgeX) / _FallOffDistance;
                

                float2 weatherUV = (size.xz * 0.5 + (rayPosition.xz - boundsCenter.xz)) / max(size.x, size.z);
                float2 weatherMap = _WeatherMap.SampleLevel(sampler_WeatherMap, weatherUV, mipLevel);
                float gMin = Remap(weatherMap.x, 0, 1, 0.1, 0.5);
                float gMax = Remap(weatherMap.x, 0, 1, gMin, 0.9);
                float heightPercent = (rayPosition.y - _BoundsMin.y) / size.y;
                float heightGradient = saturate(Remap(heightPercent, 0.0, gMin, 0, 1)) * saturate(Remap(heightPercent, 1, gMax, 0, 1));
                heightGradient *= edgeWeight;
                
                float4 worleyNoise = _WorleyNoiseMap.SampleLevel(sampler_WorleyNoiseMap, shapeSamplePosition, mipLevel);
                float4 normalizedShapeWeights = _ShapeNoiseWeights / dot(_ShapeNoiseWeights, 1);
                float shapeFBM = dot(worleyNoise, normalizedShapeWeights) * heightGradient;
                float baseDensity = shapeFBM + _DensityOffset * 0.1;

                if(baseDensity > 0)
                {
                    float oneMinus = 1 - shapeFBM;
                    float detailErodeWeight = oneMinus * oneMinus * oneMinus;
                    float cloudDensity = saturate(baseDensity - (1 - shapeFBM) * detailErodeWeight);

                    return cloudDensity * _DensityMultiplier * 0.1;
                }

                return 0;
            }

            float LightMarch(float3 position)
            {
                float directionToLight = _WorldSpaceLightPos0.xyz;
                float distanceInsideBox = RayBoxDistance(_BoundsMin, _BoundsMax, position, 1 / directionToLight).y;

                float stepSize = distanceInsideBox / _LightStepsCount;
                position += directionToLight * stepSize * 0.5;

                float totalDensity = 0;

                for(int step = 0; step < _LightStepsCount; step++)
                {
                    float density = SampleDensity(position);
                    totalDensity += max(0, density * stepSize);
                    position += directionToLight * stepSize;
                }

                float transmittance = Beer(totalDensity * _LightAbsorptionTowardsSun);

                float clampedTransmittance = _DarknessThreshold + transmittance * (1 - _DarknessThreshold);

                return clampedTransmittance;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);

                float3 rayPosition = _WorldSpaceCameraPos;
                float viewLength = length(i.viewVector);
                float3 rayDirection = i.viewVector / viewLength;

                float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = LinearEyeDepth(nonLinearDepth);

                float2 rayToContainerInfo = RayBoxDistance(_BoundsMin, _BoundsMax, rayPosition, 1 / rayDirection);
                float distanceToBox = rayToContainerInfo.x;
                float distanceInBox = rayToContainerInfo.y;
                float3 hitPoint = rayPosition + rayDirection * distanceToBox;

                float randomOffset = _BlueNoise.SampleLevel(sampler_BlueNoise, squareUV(i.uv * 3), 0);
                randomOffset *= _RayOffsetStrength;

                float cosAngle = dot(rayDirection, _WorldSpaceLightPos0.xyz);
                float phaseValue = Phase(cosAngle);
                
                float distanceLimit = min(linearDepth - distanceToBox, distanceInBox);
                float distanceTraveled = randomOffset;

                float3 lightEnergy = 0.0;
                float transmittance = 1;
                const float stepSize = 11;
                
                while(distanceTraveled < distanceLimit)
                {
                    rayPosition = hitPoint + rayDirection * distanceTraveled;

                    float density = SampleDensity(rayPosition);

                    if(density > 0)
                    {
                        float lightTransmittance = LightMarch(rayPosition);
                        lightEnergy += density * stepSize * transmittance * lightTransmittance * phaseValue;
                        transmittance *= exp(-density * stepSize * _LightAbsorptionThroughCloud);

                        if(transmittance < 0.01)
                            break;
                    }

                    distanceTraveled += stepSize;
                }

                float3 skyColorBase = lerp(_SkyColorA, _SkyColorB, sqrt(abs(saturate(rayDirection.y))));
                float3 backgroundColor = tex2D(_MainTex, i.uv);
                float distanceFog = 1 - exp(-max(0, linearDepth) * 8 * _FogDistanceThreshold);
                float3 sky = distanceFog * skyColorBase;
                backgroundColor = backgroundColor * (1 - distanceFog) + sky;

                float focusedEyeCos = pow(saturate(cosAngle), _PhaseParams.x);
                float sun = saturate(HG(focusedEyeCos, .9995)) * transmittance;

                float3 cloudColor = lightEnergy * _LightColor0;
                float3 color = backgroundColor * transmittance + cloudColor;
                color = saturate(color) * (1 - sun) + _LightColor0 * sun;
                
                return float4(color, 0);
            }
            ENDCG
        }
    }
}
