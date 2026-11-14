
Shader "Colors/Desaturate" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	HLSLINCLUDE
		
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// (Full-screen passes in the Universal Render Pipeline draw a single triangle, with the source image bound as "_BlitTexture".
// "Blit.hlsl" supplies both that texture and the matching "Vert" vertex shader, in place of "Graphics.Blit()" and "_MainTex".)
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
	

	float3 hsv_to_rgb(float3 HSV)
	{
		HSV.x /= 360;
		float var_h = HSV.x * 6;

		float var_1 = HSV.z * ( 1 - HSV.y );
		float var_2 = HSV.z * ( 1 - HSV.y * (var_h-floor( var_h )));
		float var_3 = HSV.z * ( 1 - HSV.y * (1-(var_h-floor( var_h ))));

		float3 RGB = float3(HSV.z, var_1, var_2);

		if (var_h < 5)    { RGB = float3(var_3, var_1, HSV.z); }
		if (var_h < 4)    { RGB = float3(var_1, var_2, HSV.z); }
		if (var_h < 3)    { RGB = float3(var_1, HSV.z, var_3); }
		if (var_h < 2)    { RGB = float3(var_2, HSV.z, var_1); }
		if (var_h < 1)    { RGB = float3(HSV.z, var_3, var_1); }

		return (RGB);
	}

	float3 rgb_to_hsv( float3 rgb )
	{
		float minC, maxC, delta;
		float3 hsv;

	    minC = rgb.x <= rgb.y ? rgb.x  : rgb.y;
		minC = minC <= rgb.z ? minC : rgb.z;
		maxC = rgb.x >= rgb.y ? rgb.x : rgb.y;
		maxC = maxC >= rgb.z ? maxC : rgb.z;

		hsv.z = maxC; 
		delta = maxC - minC;
		if( maxC != 0 )
		{
			hsv.y = delta / maxC;
		}
		else 
		{
			// r = g = b = 0  // s = 0, v is undefined
			hsv.y = 0;
			hsv.x = 0;
			return hsv;
		}
		if( rgb.x == maxC )
			hsv.x = ( rgb.y - rgb.z ) / delta;		// between yellow & magenta
		else if( rgb.y == maxC )
			hsv.x = 2 + ( rgb.z - rgb.x ) / delta;	// between cyan & yellow
		else
			hsv.x = 4 + ( rgb.x - rgb.y ) / delta;	// between magenta & cyan
		hsv.x *= 60;				// degrees
		if( hsv.x < 0 )
			hsv.x += 360;
		return hsv;
	}
		
	//Per pixel or something like that
	float4 frag(Varyings i) : SV_Target 
	{	
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		//Grab initial color
		half4 color = (half4)SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgba;

		float3 hsv = rgb_to_hsv(float3(color.x,color.y,color.z));
		hsv.y *= 0.5f;
		float3 rgb = hsv_to_rgb(hsv);
		
		return float4(rgb.x,rgb.y,rgb.z,0);
	}

	ENDHLSL
	
Subshader {

 Tags { "RenderPipeline" = "UniversalPipeline" }
	
 Pass {
	  Name "Desaturate"

	  ZTest Always Cull Off ZWrite Off

      HLSLPROGRAM
      
      
      #pragma vertex Vert
      #pragma fragment frag
	  #pragma target 3.5
      
      ENDHLSL
  }
}

Fallback off
	
} // shader