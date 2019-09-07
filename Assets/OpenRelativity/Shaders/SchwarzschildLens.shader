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
		_lensTex("Lens-Pass Texture", 2D) = "black" {}
		[Toggle] _isMirror("Gravity Mirror", float) = 0
		[Toggle] _hasEventHorizon("Block event horizon", float) = 0
		_cameraScale("Camera Scale", float) = 1
	}

	CGINCLUDE
#pragma exclude_renderers xbox360
#pragma glsl

#define divByZeroCutoff 1e-8f
#define PI_2 1.57079632679489661923

	sampler2D _MainTex;
	sampler2D _lensTex;
	float _playerDist, _playerAngle, _lensRadius;
	float _lensUPos, _lensVPos;
	float _frustumWidth, _frustumHeight;
	float _isMirror;
	float _hasEventHorizon;
	float _cameraScale;

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
		float3 sourceColor = float3(0, 0, 0);

		float2 lensUVPos = float2(_lensUPos, _lensVPos);
		float2 frustumSize = float2(_frustumWidth, _frustumHeight);
		float2 lensPlaneCoords = (i.uv - lensUVPos) * frustumSize;
		float r = length(lensPlaneCoords);

		// Minimum impact paramater should be the Schwarzschild radius. Anything less would be trapped.
		if (!_hasEventHorizon || r > _lensRadius || _playerAngle > PI_2) {

			float sourceAngle = atan2(r, _playerDist);
			float deflectionAngle = 2 * (_lensRadius / r) * cos(_playerAngle / 2) / _cameraScale;

			uint inversionCount = abs(deflectionAngle) / PI_2;
			if ((_playerAngle > PI_2 ||
				!(_hasEventHorizon && deflectionAngle >= PI_2))
				&& inversionCount % 2 == (_isMirror < 0.5 ? 0 : 1))
			{
				lensPlaneCoords = _playerDist * tan(sourceAngle - deflectionAngle) * lensPlaneCoords / r;
				float2 uvProj = lensPlaneCoords / frustumSize;
				float scale = length(i.uv - lensUVPos) / length(uvProj);
				uvProj = (uvProj + lensUVPos);
				uvProj = (uvProj - float2(0.5f, 0.5f)) * _cameraScale + float2(0.5f, 0.5f);
				uvProj *= scale;
				float4 s = float4(uvProj, 0, scale);
				sourceColor = tex2Dproj(_MainTex, UNITY_PROJ_COORD(s)).rgb;
			}
			else if (_isMirror >= 0.5f) {
				sourceColor = tex2D(_lensTex, i.uv).rgb;
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
