Shader "CrazyMarket/Butter Recall Puff"
{
    Properties
    {
        _BaseColor ("Butter", Color) = (.98, .906, .47, 1)
        _ShadowColor ("Cel shadow", Color) = (.784, .604, .208, 1)
        _HighlightColor ("Cel highlight", Color) = (1, .957, .69, 1)
        _OutlineColor ("Ink", Color) = (.125, .106, .071, 1)
        _OutlineWidth ("Ink width", Range(.01, .15)) = .055
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "CartoonPuff"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            // The player renders after transparents; solid cloudlets must occlude that pass.
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _ShadowColor, _HighlightColor, _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2 - 1;
                return output;
            }

            // Overlapping lobes give each billboard a hand-drawn cloud silhouette.
            float CloudDistance(float2 p)
            {
                // A solid core prevents the lobe joins from receiving interior ink specks.
                float d = length(p) - .52;
                d = min(d, length(p - float2(-.38, -.12)) - .4);
                d = min(d, length(p - float2(-.28, .29)) - .38);
                d = min(d, length(p - float2(.13, .38)) - .43);
                d = min(d, length(p - float2(.46, .05)) - .35);
                d = min(d, length(p - float2(.24, -.3)) - .39);
                return min(d, length(p - float2(-.17, -.34)) - .36);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv;
                float d = CloudDistance(p);
                // Only the outermost pixel is softened; the fill and cel bands stay solid.
                float aa = max(fwidth(d), .001);
                clip(-d);
                float alpha = saturate(-d / aa);
                float light = length((p - float2(-.24, .34)) * float2(.85, 1));
                half3 fill = lerp(_BaseColor.rgb, _ShadowColor.rgb, step(.7, light));
                fill = lerp(fill, _HighlightColor.rgb, 1 - step(.29, light));
                // Keep a readable ink stroke as the cloudlets shrink toward disappearance.
                float width = max(_OutlineWidth, aa * 1.5);
                half3 color = lerp(fill, _OutlineColor.rgb, smoothstep(-width-aa*.5, -width+aa*.5, d));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
