Shader "NightRun/Architecture" {
 Properties { _MainTex("Surface",2D)="white"{} _Color("Tint",Color)=(.6,.6,.6,1) _Scale("Meters to UV",Float)=.4 _Smoothness("Smoothness",Range(0,1))=.18 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color;float _Scale,_Smoothness;
 struct Input { float3 stablePos; float3 stableNormal; };
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.stablePos=v.vertex.xyz;o.stableNormal=v.normal;}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float3 n=pow(abs(IN.stableNormal),6);n/=max(dot(n,1),.0001);
 float3 p=IN.stablePos*_Scale;
 float3 c=tex2D(_MainTex,p.zy).rgb*n.x+tex2D(_MainTex,p.xz).rgb*n.y+tex2D(_MainTex,p.xy).rgb*n.z;
 o.Albedo=c*_Color.rgb;o.Metallic=.04;o.Smoothness=_Smoothness;o.Occlusion=.85;
 } ENDCG } Fallback "Standard"
}
