Shader "Relativity/SchwarzschildLens"
{
	Properties
	{
		_MainTex("Source", 2D) = "white" {}
		_playerDist("Player Distance", float) = 0
		_playerAngle("Player Angle", float) = 0
		_lensRadius("Lens Schwarzschild Radius", float) = 0
		_lensUPos("Lens Position (U)", float) = 0
		_lensVPos("Lens Position (V)", float) = 0
		_frustumWidth("Frustum Width", float) = 0
		_frustumHeight("Frustum Height", float) = 0
		[Toggle] _HasEventHorizon("Block event horizon", float) = 0
	}

	CGINCLUDE
#pragma exclude_renderers xbox360
#pragma glsl

#define divByZeroCutoff 1e-8f
#define PI_2 1.57079632679489661923

	sampler2D _MainTex;
	float _playerDist, _playerAngle, _lensRadius;
	float _lensUPos, _lensVPos;
	float _frustumWidth, _frustumHeight;
	float _HasEventHorizon;

	struct VertexData {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct Interpolators {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	Interpolators vert(VertexData v) {
		Interpolators i;
		i.pos = UnityObjectToClipPos(v.vertex);
		i.uv = v.uv;
		return i;
	}

	float4 frag(Interpolators i) : SV_Target{
		float2 lensUVPos = float2(_lensUPos, _lensVPos);
		float2 frustumSize = float2(_frustumWidth, _frustumHeight);
		float2 lensPlaneCoords = (i.uv - lensUVPos) * frustumSize;
		float3 sourceColor = float3(0, 0, 0);
		float r = length(lensPlaneCoords);
		if (r < divByZeroCutoff) {
			sourceColor = tex2D(_MainTex, i.uv).rgb;
		} else {
			float sourceAngle = atan2(r, _playerDist);
			float deflectionAngle = 2 * (_lensRadius / r) * cos(_playerAngle / 2);
			uint inversionCount = abs(deflectionAngle) / PI_2;
			// Deflection angle greater than pi / 2 implies total deflection, away from camera
			if (inversionCount % 2 == 0) {
				// Minimum impact paramater should be the Schwarzschild radius. Anything less would be trapped.
				// However, per the black hole dissolution treatment, these rays would ultimately be released.
				// (TL;DR - Dan is speculating, but the less speculative case is enacted when black hole dissolution is turned off.)
				float impactParam = _playerDist * tan(sourceAngle - deflectionAngle);
				if (!_HasEventHorizon || abs(impactParam) > _lensRadius) {
					lensPlaneCoords = impactParam * lensPlaneCoords / r;
					i.uv = lensPlaneCoords / frustumSize + lensUVPos;
					sourceColor = tex2D(_MainTex, i.uv).rgb;
				}
			}
		}
		return float4(sourceColor, 1);
	}

	ENDCG

	Subshader {
		Pass{
			Cull Off ZWrite On
			Fog { Mode off }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma fragmentoption ARB_precision_hint_nicest

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			ENDCG
		}
	}
}
