Shader "CrazyMarket/Butter/Liquid"
{
    Properties
    {
        _ButterColor ("Butter", Color) = (1, .72, .13, 1)
        _CreamColor ("Cream highlight", Color) = (1, .97, .72, 1)
        _Gloss ("Gloss", Range(8,128)) = 72
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ButterColor, _CreamColor;
                float _Gloss, _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; half4 color:COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                Light light = GetMainLight();
                float diffuse = saturate(dot(n, light.direction));
                float spec = pow(saturate(dot(n, normalize(light.direction + v))), _Gloss);
                // A broad cream reflection gives small droplets readable volume even in the shadowed aisles.
                float ribbon = pow(saturate(dot(reflect(-v,n), normalize(float3(-.4,1,.25)))), 18);
                float fresnel = pow(1-saturate(dot(n,v)), 3);
                half3 color = _ButterColor.rgb * i.color.rgb * (.68 + diffuse*.32);
                color = lerp(color, _CreamColor.rgb, saturate(spec*.8 + ribbon*.55 + fresnel*.16));
                return half4(color, i.color.a * _ButterColor.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
