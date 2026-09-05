Shader "CrazyMarket/Main Menu Contact Shadow"
{
    Properties
    {
        _Color ("Shadow Color", Color) = (0.02, 0.035, 0.025, 0.35)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-5" "RenderType" = "Transparent" }
        Pass
        {
            Name "ContactShadow"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half radius = length((input.uv - 0.5) * 2.0);
                half softness = 1.0 - smoothstep(0.1, 1.0, radius);
                return half4(_Color.rgb, _Color.a * softness);
            }
            ENDHLSL
        }
    }
}
