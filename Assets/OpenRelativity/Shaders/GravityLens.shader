Shader "Relativity/GravityLens"
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
	}

	CGINCLUDE
#pragma exclude_renderers xbox360
#pragma glsl

#define divByZeroCutoff 1e-8f

	sampler2D _MainTex;
	float _playerDist, _playerAngle, _lensRadius;
	float _lensUPos, _lensVPos;
	float _frustumWidth, _frustumHeight;

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
			lensPlaneCoords = _playerDist * tan(sourceAngle - deflectionAngle) * lensPlaneCoords / r;
			i.uv = lensPlaneCoords / frustumSize + lensUVPos;
			sourceColor = tex2D(_MainTex, i.uv).rgb;
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
