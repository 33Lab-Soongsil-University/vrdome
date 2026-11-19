Shader "DomeStudy/SceneParticles"
{
 Properties { _Kind("Kind",Float)=0 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Tags {"LightMode"="SRPDefaultUnlit"}
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 CGPROGRAM
 #pragma vertex vert
            #pragma multi_compile_instancing
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
 float _Kind;
 v2f vert(appdata v){v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {
 float2 p=i.uv*2-1;float r=length(p);float a=saturate(1-r);float3 col=i.color.rgb;
 if(_Kind<.5||(_Kind>6.5&&_Kind<7.5)) {a=a*a; a*=.65+.35*sin(p.x*13+sin(p.y*17))*sin(p.y*11);}
 else if(_Kind<1.5) {a=1-smoothstep(.78,1,abs(p.x)*1.8+abs(p.y));col*=.7+.3*step(0,p.x);}
 else if(_Kind<2.5) {a=pow(saturate(1-length(p*float2(1.5,.7))),2);col=lerp(col,float3(1,1,1),a*.6);}
 else if(_Kind<3.5) {a=(1-smoothstep(.4,.5,abs(p.x)))*(1-smoothstep(.75,.85,abs(p.y)));}
 else if(_Kind<4.5) {a=(1-smoothstep(.85,1,r))*smoothstep(.55,.8,r)*.7 + pow(saturate(1-length((p-float2(-.3,.35))*4)),2)*.7;}
 else if(_Kind<5.5) {
 a=(1-smoothstep(.58,.63,abs(p.x)))*(1-smoothstep(.88,.93,abs(p.y)));
 float diamond=1-step(.28,abs(p.x)+abs(p.y));
 col=lerp(float3(.94,.94,.88),i.color.rgb,diamond);
 col=lerp(col,i.color.rgb,step(.47,abs(p.x))*.35);
 }
 else if(_Kind<6.5) {a=pow(saturate(1-length(p*float2(3,.75))),2);col*=1.5;}
 else {a=pow(a,3);col*=1.5;}
 return fixed4(col,i.color.a*a);
 }
 ENDCG
 }
 }
}
