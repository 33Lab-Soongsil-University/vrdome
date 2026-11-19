Shader "DomeStudy/Particles"
{
    Properties { _Leaf("Leaf shape", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma multi_compile_instancing
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR;  UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR;  UNITY_VERTEX_OUTPUT_STEREO };
            float _Leaf;
            v2f vert(appdata v) { v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float d=lerp(length(p), abs(p.x)*1.5+abs(p.y), _Leaf);
                float a=1-smoothstep(lerp(.1,.7,_Leaf),1,d);
                return fixed4(i.color.rgb,i.color.a*a);
            }
            ENDCG
        }
    }
}
