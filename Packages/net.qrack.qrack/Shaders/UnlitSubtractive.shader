// The "Clear" material used the built-in render pipeline's "Particles/Standard Unlit" shader, in its "Subtractive"
// rendering mode, which the Universal Render Pipeline's particle shaders don't have. This is that mode, on its own:
// the (unlit) texture times color, times alpha, is subtracted from whatever is already drawn behind it.
Shader "Qrack/Unlit Subtractive"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            BlendOp RevSub
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            ColorMask RGB

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color * i.color;
            }

            ENDHLSL
        }
    }

    Fallback Off
}
