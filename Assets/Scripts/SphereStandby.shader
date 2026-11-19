Shader "DomeStudy/SphereStandby"
{
    Properties { _Tint("Sky tint", Color)=(.035,.11,.22,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma multi_compile_instancing
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0;  UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0;  UNITY_VERTEX_OUTPUT_STEREO };
            float4 _Tint;
            v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float h=saturate(uv.y);
                float3 sky=lerp(float3(.17,.26,.33),float3(.004,.012,.036),smoothstep(0,.9,h));
                float glow=exp(-pow((uv.x-.54)*7,2)-pow((h-.19)*8,2));
                sky+=glow*float3(.28,.12,.045);
                float ridge=.13+.018*sin(uv.x*28)+.022*sin(uv.x*61)+.008*sin(uv.x*159);
                sky=lerp(sky,float3(.025,.055,.084),1-smoothstep(ridge-.003,ridge+.003,h));
                float ridge2=.065+.01*sin(uv.x*39+2)+.016*sin(uv.x*83);
                sky=lerp(sky,float3(.008,.022,.038),1-smoothstep(ridge2-.002,ridge2+.002,h));
                float2 cell=floor(uv*float2(1500,650));
                float star=step(.998,hash(cell))*pow(saturate(1-length(frac(uv*float2(1500,650))-.5)*2),5);
                sky+=star*smoothstep(.18,.5,h)*.8;
                float ribbon=.43+.10*sin(uv.x*8)+.025*sin(uv.x*27);
                float aurora=exp(-abs(h-ribbon)*26)*(.25+.75*pow(.5+.5*sin(uv.x*92),2));
                sky+=aurora*float3(.012,.09,.065)*smoothstep(.15,.4,h);
                return float4(sky,1);
            }
            ENDCG
        }
    }
}
