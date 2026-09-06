Shader "CrazyMarket/Butter/Organic Puddle"
{
    Properties
    {
        _ButterColor ("Butter body", Color) = (1,.84,.38,1)
        _EdgeColor ("Golden edge", Color) = (.96,.74,.27,1)
        _CreamColor ("Reflected cream", Color) = (1,.97,.72,1)
        _Seed ("Shape seed", Float) = 1
        _Age ("Normalized age", Range(0,1)) = 0
        _Opacity ("Opacity", Range(0,1)) = .94
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ButterColor, _EdgeColor, _CreamColor;
                float _Seed, _Age, _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 normalWS:TEXCOORD2; float3 tangentWS:TEXCOORD3; float3 bitangentWS:TEXCOORD4; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv=i.uv;
                o.normalWS=TransformObjectToWorldNormal(float3(0,1,0));
                o.tangentWS=normalize(TransformObjectToWorldDir(float3(1,0,0)));
                o.bitangentWS=normalize(TransformObjectToWorldDir(float3(0,0,1)));
                return o;
            }
            float Edge(float2 p)
            {
                float angle=atan2(p.y,p.x);
                float radius=.72 + .10*sin(angle*3+_Seed) + .065*sin(angle*5-_Seed*1.7) + .03*sin(angle*8+_Seed*.8);
                radius += .012*sin(angle*4 + _Age*2 + _Seed);
                return radius-length(p);
            }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                float4 v=float4(dot(cell,float2(127.1,311.7)),dot(cell+float2(1,0),float2(127.1,311.7)),dot(cell+float2(0,1),float2(127.1,311.7)),dot(cell+1,float2(127.1,311.7)));
                float4 h=frac(sin(v)*43758.5453);
                return lerp(lerp(h.x,h.y,f.x),lerp(h.z,h.w,f.x),f.y);
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.uv*2-1;
                float edge=Edge(p);
                float aa=max(fwidth(edge),.009);
                float alpha=smoothstep(-aa,aa,edge)*_Opacity;
                clip(alpha-.004);
                // Do not leave a floating sheet where a stamp crosses the edge of a ramp or step.
                float sceneDepth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS)),_ZBufferParams);
                float eyeDepth=-TransformWorldToView(i.positionWS).z;
                alpha*=1-smoothstep(.08,.32,sceneDepth-eyeDepth);
                float rim=exp(-max(edge,0)*22);
                // World-space flow stays continuous when neighboring wet patches merge.
                float2 flow=i.positionWS.xz;
                float marble=Noise(flow*1.7+Noise(flow*.55)*2);
                float wave=sin(flow.x*3.7+sin(flow.y*1.6)*2.8+marble*3);
                float crossWave=sin(flow.y*4.2+sin(flow.x*1.3)+marble*2);
                float3 n=normalize(i.normalWS+float3(wave*.14,0,crossWave*.12));
                float3 v=GetWorldSpaceNormalizeViewDir(i.positionWS);
                Light light=GetMainLight();
                float broad=pow(saturate(dot(reflect(-v,n),normalize(float3(-.25,1,.45)))),32);
                float glint=pow(saturate(dot(n,normalize(v+light.direction))),96);
                float sheen=smoothstep(.91,.99,wave*.5+crossWave*.5)*.30;
                half3 color=lerp(_ButterColor.rgb,_EdgeColor.rgb,rim*.05);
                color*=.78+.16*saturate(dot(n,light.direction)) + marble*.09;
                color=lerp(color,_CreamColor.rgb,saturate(broad*.65+glint*.8+sheen));
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
