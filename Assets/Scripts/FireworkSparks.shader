Shader "DomeStudy/FireworkSparks"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma multi_compile_instancing
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0;  UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0;  UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float r=length(i.uv*2-1);
                float glow=saturate(1-r);
                return fixed4(i.color.rgb*1.8, i.color.a*glow*glow);
            }
            ENDCG
        }
    }
}
