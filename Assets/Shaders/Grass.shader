Shader "Custom/Grass"
{
    Properties
    {
		[Header(Shading)]
        _TopColor("Top Color", Color) = (1,1,1,1)
		_BottomColor("Bottom Color", Color) = (1,1,1,1)
    	_TranslucentGain("Translucent Gain", Range(0,1)) = 0.5

    	[Header(Tessellation)]
        _TessellationUniform("Tessellation Uniform", Range(1, 64)) = 1
    	
    	[Header(Blade)]
    	_BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2
    	_BladeWidth("Blade Width", Float) = 0.05
		_BladeWidthRandom("Blade Width Random", Float) = 0.02
		_BladeHeight("Blade Height", Float) = 0.5
		_BladeHeightRandom("Blade Height Random", Float) = 0.3
    	_BladeForward("Blade Forward Amount", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2

    	[Header(Wind)]
    	_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
    	_WindStrength("Wind Strength", Float) = 1
    	_WindDirectionOffset("Wind Direction Offset", Vector) = (0, 0, 0, 0)
    	_WindFrequencyX("X", Range(-0.05, 0.05)) = 0.05
    	_WindFrequencyY("Y", Range(-0.05, 0.05)) = 0.05
    	
    	[Header(Snow)]
		_SnowPercent("Snow Percent", Range(0, 1)) = 0
		_SnowColor("Snow Color", Color) = (1,1,1,1)
    }
    
    CGINCLUDE
	#include "UnityCG.cginc"
	#include "Autolight.cginc"
    #include "CustomTessellation.cginc"

	#define BLADE_SEGMENT_COUNT 3
    
	// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
	// Extended discussion on this function can be found at the following link:
	// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
	// Returns a number in the 0...1 range.
	float rand(float3 co)
	{
		return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
	}

	// Construct a rotation matrix that rotates around the provided axis, sourced from:
	// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
	float3x3 AngleAxis3x3(float angle, float3 axis)
	{
		float c, s;
		sincos(angle, s, c);

		float t = 1 - c;
		float x = axis.x;
		float y = axis.y;
		float z = axis.z;

		return float3x3(
			t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			t * x * z - s * y, t * y * z + s * x, t * z * z + c
			);
	}
	

	struct GeometryOutput
    {
	    float4 pos : SV_POSITION;
    	float2 uv : TEXCOORD0;
    	unityShadowCoord4 _ShadowCoord : TEXCOORD1;
    	float3 normal : NORMAL;
    };


    GeometryOutput GenerateVertexOutput(float3 pos, float2 uv, float3 normal)
    {
		GeometryOutput o;
    	o.pos = UnityObjectToClipPos(pos);
    	o.uv = uv;
    	o._ShadowCoord = ComputeScreenPos(o.pos);
    	o.normal = UnityObjectToWorldNormal(normal);

    	#if UNITY_PASS_SHADOWCASTER
			// Applying the bias prevents artifacts from appearing on the surface.
			o.pos = UnityApplyLinearShadowBias(o.pos);
		#endif
    	
    	return o;
    }

    GeometryOutput GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv, float3x3 transformMatrix)
    {
	    const float3 tangentPoint = float3(width, forward, height);
    	const float3 tangentNormal = normalize(float3(0, -1, forward));
    	const float3 localNormal = mul(transformMatrix, tangentNormal);
	    const float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
    	
    	return GenerateVertexOutput(localPosition, uv, localNormal);
    }

    // Blade
	float _BendRotationRandom;
	float _BladeHeight;
	float _BladeHeightRandom;	
	float _BladeWidth;
	float _BladeWidthRandom;
    float _BladeForward;
    float _BladeCurve;

	// Wind
    float _WindStrength;
	float2 _WindFrequency;
    sampler2D _WindDistortionMap;
    float4 _WindDistortionMap_ST;
    float2 _WindDirectionOffset;
    float _WindFrequencyX;
    float _WindFrequencyY;
    
    [maxvertexcount(BLADE_SEGMENT_COUNT * 2 + 1)]
    void geo(triangle VertexOutput IN[3], inout TriangleStream<GeometryOutput> triStream)
    {
	    const float3 vertexPosition = IN[0].vertex;

		float3 vNormal = IN[0].normal;
    	float4 vTangent = IN[0].tangent;
    	float3 vBinormal = cross(vNormal, vTangent) * vTangent.w;

    	// Setup Matrices 
	    const float3x3 tangentToLocal = float3x3(
    		vTangent.x, vBinormal.x, vNormal.x,	
    		vTangent.y, vBinormal.y, vNormal.y,	
    		vTangent.z, vBinormal.z, vNormal.z	
    	);
	    const float3x3 facingRotationMatrix = AngleAxis3x3(rand(vertexPosition) * UNITY_TWO_PI, float3(0, 0, 1));
    	const float3x3 bendRotationMatrix = AngleAxis3x3(rand(vertexPosition.zzx) * _BendRotationRandom * UNITY_PI * 0.5, float3(-1, 0, 0));

    	float2 windUV = vertexPosition.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + float2(_WindFrequencyX, _WindFrequencyY) * _Time.y;
		float2 windSample = (tex2Dlod(_WindDistortionMap, float4(windUV, 0, 0)).xy * 2 - 1) * _WindStrength;
	    const float3 windDirection = normalize(float3(_WindFrequencyX * windSample.x, _WindFrequencyY * windSample.y, 0));
		
	    const float3x3 windRotationMatrix = AngleAxis3x3(UNITY_PI * windSample, windDirection);

    	// Tip Matrix
	    const float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotationMatrix), facingRotationMatrix), bendRotationMatrix);
    	// Base Matrix
    	const float3x3 transformationFacingMatrix = mul(tangentToLocal, facingRotationMatrix);

    	const float forward = rand(vertexPosition.yyz) * _BladeForward;
	    const float height = (rand(vertexPosition.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
	    const float width = (rand(vertexPosition.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth;

    	for(int i = 0; i < BLADE_SEGMENT_COUNT; i++)
    	{
    		float percent = i / (float) BLADE_SEGMENT_COUNT;

            const float segmentForward = pow(percent, _BladeCurve) * forward;
            const float segmentHeight = height * percent;
            const float segmentWidth = width * (1 - percent);

    		// If this is the base set of vertices, apply the facing matrix without wind/bend applied.
            const float3x3 transformMatrix = i == 0 ? transformationFacingMatrix : transformationMatrix;

    	    const GeometryOutput baseRightVertex = GenerateGrassVertex(vertexPosition, segmentWidth, segmentHeight, segmentForward, float2(0, percent), transformMatrix);
			const GeometryOutput baseLeftVertex = GenerateGrassVertex(vertexPosition, -segmentWidth, segmentHeight, segmentForward, float2(1, percent), transformMatrix);

    		triStream.Append(baseRightVertex);
			triStream.Append(baseLeftVertex);
    	}

	    const GeometryOutput tipVertex = GenerateGrassVertex(vertexPosition, 0, height, forward, float2(.5, 1), transformationMatrix);
    	
		triStream.Append(tipVertex);
    }
    
    ENDCG
    
    
    SubShader
    {	
		Cull Off

        Pass
        {
        	Tags 
        	{ 
        		"RenderType"="Opaque" 
        		"LightMode" = "ForwardBase" 
        	}
        	
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6
			#pragma multi_compile_fwdbase
            #pragma geometry geo
            #pragma hull hull
            #pragma domain domain
            
			#include "Lighting.cginc"


            float4 _TopColor;
            float4 _BottomColor;
			float _TranslucentGain;

			float4 _SnowColor;
            float _SnowPercent;
            
            float4 frag (GeometryOutput i, fixed facing : VFACE) : SV_Target
            {
	            const float3 normal = facing > 0 ? i.normal : -i.normal;

	            const float shadow = SHADOW_ATTENUATION(i);
            	float normalDotLight = saturate(saturate(dot(normal, _WorldSpaceLightPos0)) + _TranslucentGain) * shadow;
            	float3 ambient = ShadeSH9(float4(normal, 1));
            	float4 lightIntensity = normalDotLight * _LightColor0 + float4(ambient, 1);

	            const float smoothSnowPercent = smoothstep(_SnowPercent + 0.1, _SnowPercent - 0.1, 1 - i.uv.y + 0.1);
	            const float4 snowColor = _SnowColor * smoothSnowPercent;
			
				float4 color = lerp(_BottomColor, (_TopColor + snowColor) * lightIntensity, i.uv.y);
            	
                return color;
            }
            ENDCG
        }
    	Pass
    	{
    		Tags
    		{
    			"LightMode" = "ShadowCaster"
    		}

    		CGPROGRAM

    		#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6
			#pragma multi_compile_shadowcaster

    		float4 frag(GeometryOutput i) : SV_TARGET
    		{
    			SHADOW_CASTER_FRAGMENT(i);
    		}
    		
    		ENDCG
		}
    }
}
