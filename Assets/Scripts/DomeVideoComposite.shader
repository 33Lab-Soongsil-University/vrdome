Shader "DomeStudy/VideoComposite"
{
    Properties
    {
        _MainVideo ("Main Video", 2D) = "black" {}
        _BGVideo ("Background Video", 2D) = "black" {}
        _MainRect ("Main UV Rect", Vector) = (0.33,0.4,0.34,0.2)
        _SourceCrop ("Main source crop", Vector) = (0.1,0.1,0.8,0.8)
        _EdgeBlend ("Edge blend fraction", Range(0,0.49)) = 0.08
        _BGStrength ("Background Brightness", Float) = 1
        _BGRect ("Background UV crop", Vector) = (0,0,1,1)
        _CleanSky ("Clean beach sky strip", Float) = 0
        _SourceEdges ("Source edge background", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma multi_compile_instancing
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0;  UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0;  UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainVideo, _BGVideo;
            float4 _MainRect, _SourceCrop, _BGRect;
            float _EdgeBlend;
            float _BGStrength, _CleanSky, _SourceEdges;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 localUV = (i.uv - _MainRect.xy) / _MainRect.zw;
                float2 bgUV = _BGRect.xy + i.uv * _BGRect.zw;
                float3 bg = tex2D(_BGVideo, bgUV).rgb;
                float3 cleanSky = tex2D(_BGVideo, float2(.035, i.uv.y)).rgb;
                float3 background = lerp(bg, cleanSky, _CleanSky * smoothstep(.18,.32,i.uv.y)) * _BGStrength;
                if (_SourceEdges > .5)
                {
                    float y = saturate(localUV.y) * .8 + .1;
                    float3 edgeColor = 0;
                    for(int k=-4;k<=4;k++)
                        edgeColor += lerp(tex2D(_MainVideo,float2(.025,saturate(y+k*.035))).rgb,
                            tex2D(_MainVideo,float2(.975,saturate(y+k*.035))).rgb, smoothstep(0,1,i.uv.x)) / 9;
                    background = edgeColor * _BGStrength;
                }
                if (localUV.x < 0 || localUV.x > 1 || localUV.y < 0 || localUV.y > 1)
                    return float4(background, 1);
                float2 edge = min(localUV, 1 - localUV);
                float alpha = _EdgeBlend <= 0 ? 1 :
                    smoothstep(0, _EdgeBlend, edge.x) * smoothstep(0, _EdgeBlend, edge.y);
                float2 sourceUV = _SourceCrop.xy + saturate(localUV) * _SourceCrop.zw;
                float3 main = tex2D(_MainVideo, sourceUV).rgb;
                return float4(lerp(background, main, alpha), 1);
            }
            ENDCG
        }
    }
}
