Shader "NightRun/CityWindows" {
 Properties { _Color("Concrete",Color)=(.035,.045,.07,1) _Accent("Light tint",Color)=(.2,.38,.6,1) }
 SubShader { Tags {"RenderType"="Opaque"} LOD 150
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 struct Input { float3 worldPos; float3 worldNormal; };
 fixed4 _Color,_Accent;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float2 xy=float2(abs(IN.worldNormal.z)>.5?IN.worldPos.x:IN.worldPos.z,IN.worldPos.y);
 float2 grid=xy*float2(.58,.48);float2 f=frac(grid);float window=step(.18,f.x)*step(f.x,.76)*step(.18,f.y)*step(f.y,.72);
 float lit=step(.57,hash(floor(grid)));float face=step(abs(IN.worldNormal.y),.2);
 o.Albedo=_Color.rgb;o.Metallic=.25;o.Smoothness=.45;
 float warm=step(.84,hash(floor(grid)+.75));float3 light=lerp(_Accent.rgb,float3(1,.55,.25),warm);
 o.Emission=light*window*lit*face*(.25+hash(floor(grid)*3.2)*.65);
 } ENDCG } Fallback "Standard"
}
