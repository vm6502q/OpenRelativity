
//NOTE: For correct relativistic behavior, all light sources must be static
// with respect to world coordinates! General constant velocity lights are more complicated,
// and lights that accelerate might not be at all feasible.

Shader "Relativity/Lit/Standard" {

	Properties{
		[Toggle(LORENTZ_TRANSFORM)] _Lorentz("Lorentz Transform", Range(0, 1)) = 1
		[Toggle(IS_STATIC)] _IsStatic("Light map static", Range(0, 1)) = 0
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		[Toggle(DOPPLER_SHIFT)] _dopplerShift("Doppler shift", Range(0,1)) = 1
		_dopplerIntensity("UV/IR mix-in intensity", Range(0,1)) = 1
		[Toggle(DOPPLER_MIX)] _dopplerMix("Responsive Doppler shift intensity", Range(0,1)) = 0
		[Toggle(UV_IR_TEXTURES)] _UVAndIRTextures("UV and IR textures", Range(0, 1)) = 1
		_UVTex("UV", 2D) = "" {} //UV texture
		_IRTex("IR",2D) = "" {} //IR texture
		[Toggle(SPECULAR)] _SpecularOn("Specular reflections", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0
		_Metallic("Metallic", Range(0, 1)) = 0
		_Attenuation("Attenuation", Range(0, 10)) = 1
		[Toggle(_EMISSION)] _EmissionOn("Emission lighting", Range(0, 1)) = 0
		_EmissionMap("Emission map", 2D) = "black" {}
		[HDR] _EmissionColor("Emission color", Color) = (0,0,0)
		_EmissionMultiplier("Emission multiplier", Range(0,10)) = 1
		_pap("pap", Vector) = (0,0,0,0) //Vector that represents the player's proper acceleration
		_Cutoff("Base alpha cutoff", Range(0,.9)) = 0.1
		[HideInInspector] _lastUpdateSeconds("_lastUpdateSeconds", Range(0, 2500000)) = 0
	}
		HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#define M_PI 3.14159265358979323846f

//Color shift variables, used to make gaussians for XYZ curves
#define xla 0.39952807612909519f
#define xlb 444.63156780935032f
#define xlc 20.095464678736523f

#define xlcSqr 403.827700654347187546611654129529f

#define xha 1.1305579611401821f
#define xhb 593.23109262398259f
#define xhc 34.446036241271742f

#define xhcSqr 1186.529412735006279641477487714564f

#define ya 1.0098874822455657f
#define yb 556.03724875218927f
#define yc 46.184868454550838f

#define ycSqr 2133.042074164165111255232326502244f

#define za 2.0648400466720593f
#define zb 448.45126344558236f
#define zc 22.357297606503543f

#define zcSqr 499.848756265769052653089671552849f

#define bFac 0.5f
#define rFac 0.9f

//Used to determine where to center UV/IR curves
#define IR_RANGE 400
#define IR_START 700
#define UV_RANGE 380
#define UV_START 0

#define FORWARD_FOG (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))

//Prevent NaN and Inf
#ifndef FLT_EPSILON
#define FLT_EPSILON 1.192092896e-07F
#endif

		struct appdata {
			float4 vertex : POSITION;
			float4 texcoord : TEXCOORD0; // main uses 1st uv
			float4 texcoord1 : TEXCOORD1; // lightmap uses 2nd uv
			float4 normal : NORMAL;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		//This is the data sent from the vertex shader to the fragment shader
		struct v2f
		{
			float4 pos : SV_POSITION; //internal, used for display
			float4 pos2 : TEXCOORD0; //Position in world, relative to player position in world
			float2 albedoUV : TEXCOORD1; //Used to specify what part of the texture to grab in the fragment shader(not relativity specific, general shader variable)
			float2 svc : TEXCOORD2; //sqrt( 1 - (v-c)^2), calculated in vertex shader to save operations in fragment. It's a term used often in lorenz and doppler shift calculations, so we need to keep it cached to save computing
			float4 normal : NORMAL; //normal in world

			// (The built-in pipeline version of this shader packed the optional interpolators below
			// into as few TEXCOORD semantics as possible, for very old hardware. The Universal Render
			// Pipeline's minimum targets all have room for these, so each gets a fixed slot.)
#if LIGHTMAP_ON
			float2 lightmapUV : TEXCOORD3; //Lightmap TEXCOORD
#endif
#if FORWARD_FOG
			float4 pos3 : TEXCOORD4; //Untransformed position in world, relative to player position in world
#endif
#if _EMISSION
			float2 emissionUV : TEXCOORD5; //EmisionMap TEXCOORD
#endif
#if defined(_ADDITIONAL_LIGHTS)
			// Rows of the metric at this vertex, including the object Lorentz transform.
			// (The built-in pipeline version ran one pass per point light, and so could interpolate
			// "metric times light-to-vertex vector" for that pass's single light. The Universal Render
			// Pipeline shades every light in one pass, so we interpolate the metric, itself, instead.)
			float4 metric0 : TEXCOORD6;
			float4 metric1 : TEXCOORD7;
			float4 metric2 : TEXCOORD8;
			float4 metric3 : TEXCOORD9;
#endif
			UNITY_VERTEX_OUTPUT_STEREO
		};

		struct f2o {
			float4 color : SV_TARGET;
		};

		//Variables that we use to access texture data
		// (These are deliberately not wrapped in a "UnityPerMaterial" CBUFFER: the relativity scripts set
		// matrices and vectors per material instance that aren't in the Properties block, above, so this
		// shader opts out of the SRP Batcher and renders through the regular per-material path.)
		TEXTURE2D(_MainTex);
		SAMPLER(sampler_MainTex);
		uniform float4 _MainTex_ST;
		TEXTURE2D(_IRTex);
		SAMPLER(sampler_IRTex);
		uniform float4 _IRTex_ST;
		TEXTURE2D(_UVTex);
		SAMPLER(sampler_UVTex);
		uniform float4 _UVTex_ST;

		TEXTURE2D(_EmissionMap);
		SAMPLER(sampler_EmissionMap);
		uniform float4 _EmissionMap_ST;
		float4 _EmissionColor;
		float _EmissionMultiplier;

		float4 _Color;
		float _Cutoff;

		float _Smoothness;
		float _Metallic;

		float _Attenuation;

		float _dopplerIntensity;

		//Lorentz transforms from player to world and from object to world are the same for all points in an object,
		// so it saves redundant GPU time to calculate them beforehand.
		float4x4 _vpcLorentzMatrix;
		float4x4 _viwLorentzMatrix;
		float4x4 _invVpcLorentzMatrix;
		float4x4 _invViwLorentzMatrix;
		// For the original author's purposes, potentially, we have metric torsion.
		// Otherwise, the inverse would be the transpose.
		float4x4 _intrinsicMetric;
		float4x4 _invIntrinsicMetric;

		float4 _viw; //velocity of object in synchronous coordinates
		float4 _vr; //velocity of object relative to player
		float4 _pao; //acceleration of object in world coordinates
		float4 _aviw; //scaled angular velocity
		float4 _vpc; //velocity of player
		float4 _avp; //angular velocity of player
		float4 _pap; //proper acceleration of object
		float4 _playerOffset; //player position in world
		float _spdOfLight; //current speed of light;

		float xyr; // xy ratio
		float xs; // x scale

		uniform float4 _MainTex_TexelSize;

		//Color functions
		float3 RGBToXYZC(float3 rgb)
		{
			const float3x3 rgbToXyz = {
				0.13514f, 0.120432f, 0.057128f,
				0.0668999f, 0.232706f, 0.0293946f,
				0.0f, 0.0000218959f, 0.358278f
			};
			return mul(rgbToXyz, rgb);
		}
		float3 XYZToRGBC(float3 xyz)
		{
			const float3x3 xyzToRgb = {
				9.94845f, -5.1485f, -1.16389f,
				-2.86007f, 5.77745f, -0.0179627f,
				0.000174791f, -0.000353084f, 2.79113f
			};
			return mul(xyzToRgb, xyz);
		}
		float3 weightFromXYZCurves(float3 xyz)
		{
			const float3x3 xyzToWeight = {
				0.0735806f, -0.0380793f, -0.00860837f,
				-0.0665378f, 0.134408f, -0.000417865f,
				0.00000299624f, -0.00000605249f, 0.0484424f
			};
			return mul(xyzToWeight, xyz);
		}

		float getXFromCurve(float3 param, float shift)
		{
			//Use constant memory, or let the compiler optimize constants, where we can get away with it:
			const float sqrt2Pi = sqrt(2 * M_PI);

			//Re-use memory to save per-vertex operations:
			float bottom2 = param.z * shift;
			bottom2 *= bottom2;
			if (bottom2 < FLT_EPSILON) {
				bottom2 = FLT_EPSILON;
			}

			float paramYShift = param.y * shift;

			float top1 = param.x * xla * exp(-(((paramYShift - xlb) * (paramYShift - xlb))
				/ (2 * (bottom2 + xlcSqr)))) * sqrt2Pi;
			float bottom1 = sqrt(1 / bottom2 + 1 / xlcSqr);

			float top2 = param.x * xha * exp(-(((paramYShift - xhb) * (paramYShift - xhb))
				/ (2 * (bottom2 + xhcSqr)))) * sqrt2Pi;
			bottom2 = sqrt(1 / bottom2 + 1 / xhcSqr);

			return (top1 / bottom1) + (top2 / bottom2);
		}
		float getYFromCurve(float3 param, float shift)
		{
			//Use constant memory, or let the compiler optimize constants, where we can get away with it:
			const float sqrt2Pi = sqrt(2 * M_PI);

			//Re-use memory to save per-vertex operations:
			float bottom = param.z * shift;
			bottom *= bottom;
			if (bottom < FLT_EPSILON) {
				bottom = FLT_EPSILON;
			}

			float top = param.x * ya * exp(-((((param.y * shift) - yb) * ((param.y * shift) - yb))
				/ (2 * (bottom + ycSqr)))) * sqrt2Pi;
			bottom = sqrt(1 / bottom + 1 / ycSqr);

			return top / bottom;
		}
		float getZFromCurve(float3 param, float shift)
		{
			//Use constant memory, or let the compiler optimize constants, where we can get away with it:
			const float sqrt2Pi = sqrt(2 * M_PI);

			//Re-use memory to save per-vertex operations:
			float bottom = param.z * shift;
			bottom *= bottom;
			if (bottom < FLT_EPSILON) {
				bottom = FLT_EPSILON;
			}

			float top = param.x * za * exp(-((((param.y * shift) - zb) * ((param.y * shift) - zb))
				/ (2 * (bottom + zcSqr)))) * sqrt2Pi;
			bottom = sqrt(1 / bottom + 1 / zcSqr);

			return top / bottom;
		}

		float3 constrainRGB(float3 rgb)
		{
			float w;

			w = (0 < rgb.r) ? 0 : rgb.r;
			w = (w < rgb.g) ? w : rgb.b;
			w = (w < rgb.b) ? w : rgb.g;

			if (w < 0) {
				rgb -= float3(w, w, w);
			}

			w = rgb.r;
			w = (w < rgb.g) ? rgb.g : w;
			w = (w < rgb.b) ? rgb.b : w;

			if (w > 1)
			{
				rgb /= float3(w, w, w);
			}

			return rgb;
		};

		float3 DopplerShift(float3 rgb, float UV, float IR, float shift) {
#if DOPPLER_SHIFT
			//Color shift due to doppler, go from RGB -> XYZ, shift, then back to RGB.
			if (shift < FLT_EPSILON) {
				shift = FLT_EPSILON;
			}

			float mixIntensity = _dopplerIntensity;

#if DOPPLER_MIX
			// This isn't a physical effect, but we might want to normalize the material color to albedo/light map
			// for the case of 0 relative velocity. Unless we responsively reduce the effect of UV and IR,
			// this isn't the case, with this Doppler shift function. This "mixIntensity" makes it so.
		    mixIntensity *= abs((2 / M_PI) * atan(log(shift) / log(2)));
#endif

			float3 xyz = RGBToXYZC(rgb);
			float3 weights = weightFromXYZCurves(xyz);
			float3 rParam, gParam, bParam, UVParam, IRParam;
			rParam = float3(weights.x, 615, 8);
			gParam = float3(weights.y, 550, 4);
			bParam = float3(weights.z, 463, 5);
			UVParam = float3(0.02f, UV_START + UV_RANGE * UV, 5);
			IRParam = float3(0.02f, IR_START + IR_RANGE * IR, 5);

			xyz = float3(
				(getXFromCurve(rParam, shift) + getXFromCurve(gParam, shift) + getXFromCurve(bParam, shift) + mixIntensity * (getXFromCurve(IRParam, shift) + getXFromCurve(UVParam, shift))),
				(getYFromCurve(rParam, shift) + getYFromCurve(gParam, shift) + getYFromCurve(bParam, shift) + mixIntensity * (getYFromCurve(IRParam, shift) + getYFromCurve(UVParam, shift))),
				(getZFromCurve(rParam, shift) + getZFromCurve(gParam, shift) + getZFromCurve(bParam, shift) + mixIntensity * (getZFromCurve(IRParam, shift) + getZFromCurve(UVParam, shift))));

            // See this link for criticism that suggests this should be the fifth power, rather than the third:
			// https://physics.stackexchange.com/questions/43695/how-realistic-is-the-game-a-slower-speed-of-light#answer-587149
			return constrainRGB(XYZToRGBC(pow(1 / shift, 5) * xyz));
#else
			return rgb;
#endif
		}

		float3 ApplyFog(float3 color, float UV, float IR, float shift, float3 pos) {
			// (This is the built-in pipeline's "UNITY_CALC_FOG_FACTOR_RAW" on radial distance, which the engine still feeds.)
			float fogCoord = length(pos);
#if defined(FOG_LINEAR)
			// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
			float unityFogFactor = fogCoord * unity_FogParams.z + unity_FogParams.w;
#elif defined(FOG_EXP)
			// factor = exp(-density*z)
			float unityFogFactor = unity_FogParams.y * fogCoord;
			unityFogFactor = exp2(-unityFogFactor);
#elif defined(FOG_EXP2)
			// factor = exp(-(density*z)^2)
			float unityFogFactor = unity_FogParams.x * fogCoord;
			unityFogFactor = exp2(-unityFogFactor * unityFogFactor);
#else
			float unityFogFactor = 1;
#endif
	#if _EMISSION
			float3 fogColor = DopplerShift(unity_FogColor.rgb, unity_FogColor.b * bFac, unity_FogColor.r * rFac, shift);
			return lerp(fogColor, color, saturate(unityFogFactor));
	#else
			float saturatedFogFactor = saturate(unityFogFactor);
			return DopplerShift(lerp(unity_FogColor.rgb, color, saturatedFogFactor), lerp(unity_FogColor.b * bFac, UV, saturatedFogFactor), lerp(unity_FogColor.r * rFac, IR, saturatedFogFactor), shift);
	#endif
		}

		// Diffuse (and "specular-masked") light from one realtime light, in the world rest frame.
		// A light position with w == 0 is a directional light, with its direction in xyz.
		float3 RelativisticLight(v2f i, float4x4 metric, float3 viewDir, float4 lightPosition, float3 lightColor, float spotAttenuation)
		{
			float3 lightDirection;
			float attenuation;
			if (0 == lightPosition.w) // directional light?
			{
				attenuation = 1; // no attenuation
				lightDirection = normalize(lightPosition.xyz);
			}
			else // point or spot light
			{
				float4 lightSourceToVertex4 =
					mul(_viwLorentzMatrix, float4((i.pos2.xyz + _playerOffset.xyz) - lightPosition.xyz, 0));
				float3 lightSourceToVertex = lightSourceToVertex4.xyz;
				float squaredDistance = -dot(lightSourceToVertex.xyz, mul(metric, lightSourceToVertex4).xyz);
				if (squaredDistance < 0) {
					squaredDistance = 0;
				}
				attenuation = spotAttenuation / (1 + squaredDistance * _Attenuation);
				lightDirection = normalize(-lightSourceToVertex);
			}
			float nl = max(0, dot(i.normal.xyz, lightDirection));

			// We're "cheating" a bit with the Doppler shift, here.
			// The idea is that the object's albedo should reflect proportional to the color of light that the object sees.
			// We could shift the light color according to the object's velocity relative to world coordinates,
			// (assuming that all lights must be static and stationary relative to world coordinates, which we generally require,)
			// and Doppler shift back to world coordinates after "reflecting," ultimately Doppler shifting according to player
			// perspective at the end.
			//
			// However, the Doppler shift method we have from the original OpenRelativity project doesn't actually self-invert
			// when applying the inverse of the shift parameter, which the Doppler shift should do. (This is a reasonable limitation.)
			// Assuming the Doppler shift were perfect, this should behave similarly to INVERSE Doppler shifting the albedo color
			// to calculate the reflectance, but this probably isn't physically meaningful: the albedo is a proper intrinsic property.

			float pShift = 1;
			if ((i.svc.y > FLT_EPSILON) && (dot(_viw.xyz, _viw.xyz) > FLT_EPSILON)) {
				pShift = (1 - dot(lightDirection, _viw.xyz)) / i.svc.x;
			}

			float3 albedoColor = _Color.rgb;

			albedoColor = DopplerShift(albedoColor, albedoColor.b * bFac, albedoColor.r * rFac, 1 / pShift);

			float3 lightRgb = attenuation * lightColor * albedoColor * nl;

	#if SPECULAR
			// Apply specular reflectance
			// (Schlick's approximation)
			// (Assume surrounding medium has an index of refraction of 1)

			float halfAngle = dot(normalize(lightDirection + viewDir), i.normal.xyz);
			float specFactor = (_Smoothness + (1 - _Smoothness) * pow(1 - halfAngle, 5)) * _Metallic;

			// Specular reflection is added after lightmap and shadow
			specFactor = min(1, specFactor);
			lightRgb *= 1 - specFactor;
			lightRgb += lightRgb * specFactor;
	#endif

			return lightRgb;
		}

#if defined(_ADDITIONAL_LIGHTS)
		// The Universal Render Pipeline's "GetAdditionalLight()" bakes in its own (flat space) distance attenuation.
		// We need the bare light position, to measure the distance with the metric, so this reads the same light data.
		float3 RelativisticAdditionalLight(v2f i, float4x4 metric, float3 viewDir, uint loopIndex)
		{
	#if USE_CLUSTER_LIGHT_LOOP
			int perObjectLightIndex = loopIndex;
	#else
			int perObjectLightIndex = GetPerObjectLightIndex(loopIndex);
	#endif

	#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
			float4 lightPosition = _AdditionalLightsBuffer[perObjectLightIndex].position;
			float3 lightColor = _AdditionalLightsBuffer[perObjectLightIndex].color.rgb;
			float4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[perObjectLightIndex].attenuation;
			float4 spotDirection = _AdditionalLightsBuffer[perObjectLightIndex].spotDirection;
	#else
			float4 lightPosition = _AdditionalLightsPosition[perObjectLightIndex];
			float3 lightColor = _AdditionalLightsColor[perObjectLightIndex].rgb;
			float4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[perObjectLightIndex];
			float4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
	#endif

			// (This is 1 for point and directional lights, and the cone falloff for spot lights.)
			float3 lightVector = lightPosition.xyz - (i.pos2.xyz + _playerOffset.xyz) * lightPosition.w;
			float spotAttenuation = AngleAttenuation(spotDirection.xyz, SafeNormalize(lightVector), distanceAndSpotAttenuation.zw);

			return RelativisticLight(i, metric, viewDir, lightPosition, lightColor, spotAttenuation);
		}
#endif

		//Per vertex operations
		v2f vert(appdata v)
		{
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			o.albedoUV.xy = (v.texcoord.xy + _MainTex_ST.zw) * _MainTex_ST.xy; //get the UV coordinate for the current vertex, will be passed to fragment shader
#ifdef LIGHTMAP_ON
			o.lightmapUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

#if _EMISSION
			o.emissionUV.xy = (v.texcoord.xy + _EmissionMap_ST.zw) * _EmissionMap_ST.xy;
#endif

			float4 tempPos = mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1));
#if FORWARD_FOG
			o.pos3 = float4(tempPos.xyz / tempPos.w - _playerOffset.xyz, 0);
			o.pos = o.pos3;
#else
			o.pos = float4(tempPos.xyz / tempPos.w - _playerOffset.xyz, 0);
#endif

            float speedSqr = dot(_vpc.xyz, _vpc.xyz);
			float speed = sqrt(speedSqr);
			float _spdOfLightSqrd = _spdOfLight * _spdOfLight;

			//relative speed
			float speedRSqr = dot(_vr.xyz, _vr.xyz);
			float pSpeedRSqr = dot(_viw.xyz, _viw.xyz);
			// To decrease number of operations in fragment shader, we're storing this value:
			o.svc = sqrt(float2(1 - speedRSqr, 1 - pSpeedRSqr));

			//riw = location in world, for reference
			float4 riw = float4(o.pos.xyz, 0); //Position that will be used in the output

#if LORENTZ_TRANSFORM || defined(_ADDITIONAL_LIGHTS)
			//Boost to rest frame of player:
			float4 riwForMetric = mul(_vpcLorentzMatrix, riw);

			//Find metric based on player acceleration and rest frame:
			float linFac = 1 + dot(_pap.xyz, riwForMetric.xyz) / _spdOfLightSqrd;
			linFac *= linFac;
			float angFac = dot(_avp.xyz, riwForMetric.xyz) / _spdOfLight;
			angFac *= angFac;
			float avpMagSqr = dot(_avp.xyz, _avp.xyz);
			float3 angVec = float3(0, 0, 0);
			if (avpMagSqr > FLT_EPSILON) {
				angVec = 2 * angFac / (_spdOfLight * avpMagSqr) * _avp.xyz;
			}

			float4x4 metric = {
				-1, 0, 0, -angVec.x,
				0, -1, 0, -angVec.y,
				0, 0, -1, -angVec.z,
				-angVec.x, -angVec.y, -angVec.z, (linFac * (1 - angFac) - angFac)
			};

			//Lorentz boost back to world frame:
			metric = mul(transpose(_invVpcLorentzMatrix), mul(metric, _invVpcLorentzMatrix));

			// Apply world coordinates intrinsic curvature:
			metric = mul(_intrinsicMetric, mul(metric, _invIntrinsicMetric));

			//(We'll also Lorentz transform the vectors.)
			//Apply Lorentz transform:
			metric = mul(transpose(_viwLorentzMatrix), mul(metric, _viwLorentzMatrix));
#endif

#if LORENTZ_TRANSFORM
			float4 paoTransformed = mul(_viwLorentzMatrix, _pao);
			float4 riwTransformed = mul(_viwLorentzMatrix, riw);
			//Translate in time:
			float tisw = riwTransformed.w;
			riwTransformed.w = 0;

			//(When we "dot" four-vectors, always do it with the metric at that point in space-time, like we do so here.)
			float riwDotRiw = -dot(riwTransformed, mul(metric, riwTransformed));
		    float4 paot = mul(metric, paoTransformed);
			float paoDotpao = -dot(paoTransformed, paot);

    #if IS_STATIC
			float sqrtArg = riwDotRiw / _spdOfLightSqrd;
			tisw += (sqrtArg > 0) ? -sqrt(sqrtArg) : 0;
    #else
			float riwDotpao = -dot(riwTransformed, paot);

			float sqrtArg = riwDotRiw * (_spdOfLightSqrd - riwDotpao + paoDotpao * riwDotRiw / (4 * _spdOfLightSqrd)) / ((_spdOfLightSqrd - riwDotpao) * (_spdOfLightSqrd - riwDotpao));
			float paoMagSqr = dot(paoTransformed.xyz, paoTransformed.xyz);
			float paoMag = sqrt(paoMagSqr);
			tisw += (sqrtArg > 0) ? -sqrt(sqrtArg) : 0;
			//add the position offset due to acceleration
			if (paoMag > FLT_EPSILON)
			{
				riwTransformed.xyz -= paoTransformed.xyz / paoMag * _spdOfLightSqrd * (sqrt(1 + sqrtArg * paoMagSqr / _spdOfLightSqrd) - 1);
			}
    #endif
			riwTransformed.w = tisw;

			//Inverse Lorentz transform the position:
			riw = mul(_invViwLorentzMatrix, riwTransformed);
			tisw = riw.w;
			riw = float4(riw.xyz + tisw * _spdOfLight * _viw.xyz, 0);

			float newz = speed * _spdOfLight * tisw;

			if (speed > FLT_EPSILON) {
				float3 vpcUnit = _vpc.xyz / speed;
				newz = (dot(riw.xyz, vpcUnit) + newz) / sqrt(1 - speedSqr);
				riw += (newz - dot(riw.xyz, vpcUnit)) * float4(vpcUnit, 0);
			}

			riw += float4(_playerOffset.xyz, 0);

			//Transform the vertex back into local space for the mesh to use
			tempPos = mul(GetWorldToObjectMatrix(), float4(riw.xyz, 1));
			o.pos = float4(tempPos.xyz / tempPos.w, 0);

			o.pos2 = float4(riw.xyz - _playerOffset.xyz, 0);

			o.pos = TransformObjectToHClip(o.pos.xyz);
#else
			//Transform the vertex back into local space for the mesh to use
			o.pos2 = o.pos;
			o.pos = TransformObjectToHClip(v.vertex.xyz);
#endif

			o.normal = float4(TransformObjectToWorldNormal(v.normal.xyz), 0);
			o.normal = normalize(float4(mul(_viwLorentzMatrix, float4(o.normal.xyz, 0)).xyz, 0));

#if defined(_ADDITIONAL_LIGHTS)
			o.metric0 = metric[0];
			o.metric1 = metric[1];
			o.metric2 = metric[2];
			o.metric3 = metric[3];
#endif

			return o;
		}

		//Per pixel shader, does color modifications
		f2o frag(v2f i)
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

			float shift = 1;
#if DOPPLER_SHIFT
			// ( 1 - (v/c)cos(theta) ) / sqrt ( 1 - (v/c)^2 )
			if ((i.svc.x > FLT_EPSILON) && (dot(_vr.xyz, _vr.xyz) > FLT_EPSILON)) {
				shift = (1 - dot(normalize(i.pos2.xyz), _vr.xyz)) / i.svc.x;
			}
#endif

			//The Doppler factor is squared for (diffuse or specular) reflected light.
			//The light color is given in the world frame. That is, the Doppler-shifted "photons" in the world frame
			//are indistinguishable from photons of the same color emitted from a source at rest with respect to the world frame.
			//Assume the albedo is an intrinsic reflectance property. The reflection spectrum should not be frame dependent.

			//Get initial color 
			float3 viewDir = normalize(mul(_viwLorentzMatrix, float4(_WorldSpaceCameraPos.xyz - i.pos2.xyz, 0)).xyz);
			i.normal /= length(i.normal);
			float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.albedoUV);
			albedo = float4(albedo.rgb * _Color.xyz, albedo.a);

#if UV_IR_TEXTURES
			float UV = SAMPLE_TEXTURE2D(_UVTex, sampler_UVTex, i.albedoUV).r * _Color.b;
			float IR = SAMPLE_TEXTURE2D(_IRTex, sampler_IRTex, i.albedoUV).r * _Color.r;

	#if _EMISSION
			float3 rgbEmission = DopplerShift(((SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.emissionUV) * _EmissionMultiplier) * _EmissionColor).rgb, UV, IR, shift);
	#endif
#else
	#if _EMISSION
			float3 rgbEmission = ((SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.emissionUV) * _EmissionMultiplier) * _EmissionColor).rgb;
			rgbEmission = DopplerShift(rgbEmission, rgbEmission.b * bFac, rgbEmission.r * rFac, shift);
	#endif
#endif

			//Apply lighting in world frame:
			float3 rgbFinal = albedo.xyz;

			// Diffuse lighting color in world rest frame
			float4 diff = 0;

#if defined(LIGHTMAP_ON)
			float2 staticLightmapUV = i.lightmapUV;
			float3 lms = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS, float4(1, 1, 0, 0), true);

	#if defined(DIRLIGHTMAP_COMBINED)
			float3 lightDirection = SAMPLE_TEXTURE2D_LIGHTMAP(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME, LIGHTMAP_SAMPLE_EXTRA_ARGS).xyz;

			lightDirection = lightDirection * 2 - 1;

			// The length of the direction vector is the light's "directionality", i.e. 1 for all light coming from this direction,
			// lower values for more spread out, ambient light.
			float directionality = max(0.001f, length(lightDirection));
			lightDirection /= directionality;

			lightDirection = normalize(mul(_viwLorentzMatrix, float4(lightDirection, 0)).xyz);

			// Split light into the directional and ambient parts, according to the directionality factor.
			diff = float4(lms * (1 - directionality), 0);
			lms = lms * directionality;

			float nl = max(0, dot(i.normal.xyz, lightDirection));

			float3 lightRgb = DopplerShift(lms, lms.b * bFac, lms.r * rFac, shift);
			lightRgb = lightRgb * _Color.rgb * nl;

		#if SPECULAR
			// Apply specular reflectance
			// (Schlick's approximation)
			// (Assume surrounding medium has an index of refraction of 1)

			float halfAngle = dot(normalize(lightDirection + viewDir), i.normal.xyz);
			float specFactor = (_Smoothness + (1 - _Smoothness) * pow(1 - halfAngle, 5)) * _Metallic;

			// Specular reflection is added after lightmap and shadow
			specFactor = min(1, specFactor);
			lightRgb *= 1 - specFactor;
			lightRgb += lightRgb * specFactor;
		#endif

			// Technically this is incorrect, but helps hide jagged light edge at the object silhouettes and
			// makes normalmaps show up.
			lightRgb *= saturate(dot(i.normal.xyz, lightDirection));

			diff += float4(lightRgb, 0);
	#else
			diff = float4((float3)lms, 0);
	#endif
#endif

			// Realtime lights: (The built-in pipeline version of this shader added each of these in its own additive pass.)
#if defined(_ADDITIONAL_LIGHTS)
			float4x4 metric = float4x4(i.metric0, i.metric1, i.metric2, i.metric3);
#else
			// (Directional lights don't measure a distance.)
			float4x4 metric = 0;
#endif

			// The main light is the brightest directional light, if there is one. (It's black, otherwise.)
			diff.rgb += RelativisticLight(i, metric, viewDir, float4(_MainLightPosition.xyz, 0), _MainLightColor.rgb, 1);

#if defined(_ADDITIONAL_LIGHTS)
			// (These two lines are just what the Forward+ flavor of the light loop macro expects to find in scope.)
			InputData inputData = (InputData)0;
			inputData.positionWS = i.pos2.xyz + _playerOffset.xyz;
			inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);

			uint pixelLightCount = GetAdditionalLightsCount();

	#if USE_CLUSTER_LIGHT_LOOP
			[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
			{
				CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

				diff.rgb += RelativisticAdditionalLight(i, metric, viewDir, lightIndex);
			}
	#endif

			LIGHT_LOOP_BEGIN(pixelLightCount)
				diff.rgb += RelativisticAdditionalLight(i, metric, viewDir, lightIndex);
			LIGHT_LOOP_END
#endif

			//Apply lighting:
			rgbFinal *= diff.rgb;

#if SPECULAR
			// Apply specular reflectance
			// (Schlick's approximation)
			// (Assume surrounding medium has an index of refraction of 1)
			// WARNING: Real-time reflections will be wrong. Use baked.

			float3 reflectionDir = reflect(-viewDir, i.normal.xyz);
			float cosAngle = dot(viewDir, i.normal.xyz);
			float specFactor2 = (_Smoothness + (1 - _Smoothness) * pow(1 - cosAngle, 5)) * _Metallic;

			// Specular reflection is added after lightmap and shadow
			specFactor2 = min(1, specFactor2);
			rgbFinal *= 1 - specFactor2;

			// (The Universal Render Pipeline handles box projection and probe blending, here. Its perceptual roughness
			// is the same "1 - smoothness" that this shader passed to the built-in pipeline's "Unity_GlossyEnvironment".)
			rgbFinal += GlossyEnvironmentReflection(reflectionDir, i.pos2.xyz + _playerOffset.xyz, 1 - _Smoothness, 1,
				GetNormalizedScreenSpaceUV(i.pos)) * specFactor2;
#endif

#if _EMISSION
			//Doppler factor should be squared for reflected light:
	#if UV_IR_TEXTURES
			rgbFinal = DopplerShift(rgbFinal, UV, IR, shift);
	#else
			rgbFinal = DopplerShift(rgbFinal, rgbFinal.b * bFac, rgbFinal.r * rFac, shift);
	#endif

			//Add emission:
			rgbFinal += rgbEmission;

	#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
			// We're approximating a volumetric effect for a fog that's stationary relative
			// to the untransformed world coordinates, so we just use those.
		#if UV_IR_TEXTURES
			rgbFinal = ApplyFog(rgbFinal, UV, IR, shift, i.pos3.xyz);
		#else
			rgbFinal = ApplyFog(rgbFinal, rgbFinal.b * bFac, rgbFinal.r * rFac, shift, i.pos3.xyz);
		#endif
	#endif
#else
			// Reflections (diffuse and specular) have effectively twice the velocity, for a Doppler shift.
			shift *= shift;

	#if FORWARD_FOG
			// Doppler shift can be combined into a single step, if there's no emission
		#if UV_IR_TEXTURES
			rgbFinal = ApplyFog(rgbFinal, UV, IR, shift, i.pos3.xyz);
		#else
			rgbFinal = ApplyFog(rgbFinal, rgbFinal.b * bFac, rgbFinal.r * rFac, shift, i.pos3.xyz);
		#endif
	#else
		#if UV_IR_TEXTURES
			rgbFinal = DopplerShift(rgbFinal, UV, IR, shift);
		#else
			rgbFinal = DopplerShift(rgbFinal, rgbFinal.b * bFac, rgbFinal.r * rFac, shift);
		#endif
	#endif
#endif

			f2o output;
			output.color = float4(rgbFinal.rgb, albedo.a);

			return output;
		}

		ENDHLSL

		Subshader {

			Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

			Pass{
				Name "ForwardLit"
				Tags{ "LightMode" = "UniversalForward" }

				//Shader properties, for things such as transparency
				ZWrite On
				ZTest LEqual
				LOD 100

				Blend SrcAlpha OneMinusSrcAlpha

				HLSLPROGRAM

				// Universal Render Pipeline keywords:
				// (This shader doesn't receive shadows, like its built-in pipeline version.)
				#pragma multi_compile _ _ADDITIONAL_LIGHTS
				#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
				#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
				#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
				#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
				#pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION

				// Unity keywords:
				#pragma multi_compile _ LIGHTMAP_ON
				#pragma multi_compile _ DIRLIGHTMAP_COMBINED
				#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
				#pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
				#pragma multi_compile_fog

				#pragma shader_feature IS_STATIC
				#pragma shader_feature LORENTZ_TRANSFORM
			    #pragma shader_feature DOPPLER_SHIFT
				#pragma shader_feature UV_IR_TEXTURES
				#pragma shader_feature DOPPLER_MIX
				#pragma shader_feature SPECULAR
			    #pragma shader_feature _EMISSION

				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.5

				ENDHLSL
			}

			Pass
			{
				Name "META"
				Tags{ "LightMode" = "Meta" }
				Cull Off
				HLSLPROGRAM

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

				struct appdata_meta
				{
					float4 vertex : POSITION;
					float2 uv0 : TEXCOORD0;
					float2 uv1 : TEXCOORD1;
					float2 uv2 : TEXCOORD2;
				};

				struct v2f_meta
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
					float2 vizUV : TEXCOORD1;
					float4 lightCoord : TEXCOORD2;
#endif
				};

				TEXTURE2D(_GIAlbedoTex);
				SAMPLER(sampler_GIAlbedoTex);
				half4 _GIAlbedoColor;

				v2f_meta vert_meta(appdata_meta v)
				{
					v2f_meta o = (v2f_meta)0;
					o.pos = UnityMetaVertexPosition(v.vertex.xyz, v.uv1, v.uv2);
					o.uv = v.uv0 * _MainTex_ST.xy + _MainTex_ST.zw;
#ifdef EDITOR_VISUALIZATION
					UnityEditorVizData(v.vertex.xyz, v.uv0, v.uv1, v.uv2, o.vizUV, o.lightCoord);
#endif
					return o;
				}

				float4 frag_meta2(v2f_meta i) : SV_Target
				{
					// We're interested in diffuse & specular colors
					// and surface roughness to produce final albedo.

					UnityMetaInput o = (UnityMetaInput)0;
					half4 c = SAMPLE_TEXTURE2D(_GIAlbedoTex, sampler_GIAlbedoTex, i.uv);
#if _EMISSION
					o.Emission = ((SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv) * _EmissionMultiplier) * _EmissionColor).rgb;
#else
					o.Emission = 0;
#endif
					o.Albedo = half3(c.rgb * _GIAlbedoColor.rgb);
#ifdef EDITOR_VISUALIZATION
					o.VizUV = i.vizUV;
					o.LightCoord = i.lightCoord;
#endif

					return UnityMetaFragment(o);
				}

				#pragma vertex vert_meta
				#pragma fragment frag_meta2
				#pragma shader_feature IS_STATIC
				#pragma shader_feature LORENTZ_TRANSFORM
				#pragma shader_feature DOPPLER_SHIFT
				#pragma shader_feature UV_IR_TEXTURES
				#pragma shader_feature DOPPLER_MIX
				#pragma shader_feature _EMISSION
				#pragma shader_feature EDITOR_VISUALIZATION
				ENDHLSL
			}

			Pass
			{
				Name "ShadowCaster"
				Tags{ "LightMode" = "ShadowCaster" }

				ZWrite On
				ZTest LEqual
				ColorMask 0

				HLSLPROGRAM
				#pragma vertex vertShadow
				#pragma fragment fragShadow

				// This is used during shadow map generation to differentiate between directional and punctual light shadows,
				// as they use different formulas to apply Normal Bias
				#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

				// Shadow Casting Light geometric parameters, set by the Universal Render Pipeline
				float3 _LightDirection;
				float3 _LightPosition;

				struct appdataShadow {
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2fShadow {
					float4 pos : SV_POSITION;
				};

				// (Like the built-in pipeline version, shadows are cast from the world rest frame mesh.)
				v2fShadow vertShadow(appdataShadow v)
				{
					v2fShadow o;
					UNITY_SETUP_INSTANCE_ID(v);

					float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
					float3 normalWS = TransformObjectToWorldNormal(v.normal);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
					float3 lightDirectionWS = _LightDirection;
#endif

					o.pos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
					o.pos = ApplyShadowClamping(o.pos);
					return o;
				}

				float4 fragShadow(v2fShadow i) : SV_Target
				{
					return 0;
				}
				ENDHLSL
			}
		}

		CustomEditor "StandardRelativityGUI"

		FallBack "Relativity/Unlit/ColorLorentz"
}
