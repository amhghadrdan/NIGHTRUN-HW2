Shader "NightRun/WetRoad" {
 Properties { _MainTex("Asphalt",2D)="gray"{} _BumpMap("Normal",2D)="bump"{} _Color("Tint",Color)=(.28,.29,.3,1) }
 SubShader { Tags {"RenderType"="Opaque"} LOD 250
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 #include "UnityStandardUtils.cginc"
 sampler2D _MainTex,_BumpMap;fixed4 _Color;
 struct Input{float3 stablePos;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.stablePos=v.vertex.xyz;}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float2 uv=IN.stablePos.xz*.3;float4 tex=tex2D(_MainTex,uv);
 o.Albedo=tex.rgb*_Color.rgb;o.Normal=UnpackScaleNormal(tex2D(_BumpMap,uv),.55);
 o.Metallic=.12;o.Smoothness=lerp(.5,.78,saturate(tex.r*1.2));o.Occlusion=.9;
 } ENDCG } Fallback "Standard"
}
