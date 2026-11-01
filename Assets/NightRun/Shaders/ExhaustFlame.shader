Shader "NightRun/ExhaustFlame" {
 Properties { _Intensity("Intensity",Float)=1 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" } Blend SrcAlpha One ZWrite Off Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct v {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 float _Intensity;
 v vert(a i){v o;float flicker=sin(_Time.y*63+i.uv.y*18)*.012*i.uv.y;i.vertex.x+=flicker;i.vertex.y+=cos(_Time.y*47+i.uv.y*14)*.008*i.uv.y;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
 float4 frag(v i):SV_Target {
 float edge=pow(saturate(1-abs(i.uv.x*2-1)),1.5);
 float jet=saturate(sin(i.uv.y*3.14159))*(1-i.uv.y*.7);
 float flicker=.8+.2*sin(i.uv.y*55-_Time.y*90);
 float3 color=lerp(float3(.48,.68,1.4),float3(1.5,.37,.06),smoothstep(.25,.7,i.uv.y));
 return float4(color*_Intensity,edge*jet*flicker);
 } ENDCG } } Fallback Off
}
