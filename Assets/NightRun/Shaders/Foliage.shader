Shader "NightRun/Foliage" {
 Properties { _Color("Leaf color",Color)=(.13,.17,.09,1) _Cutoff("Cutoff",Range(0,1))=.13 }
 SubShader { Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"} LOD 150
 CGPROGRAM
 #pragma surface surf Standard alphatest:_Cutoff vertex:vert
 #pragma target 3.0
 fixed4 _Color;struct Input {float3 stablePos;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.stablePos=v.vertex.xyz;}
 float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float grain=hash(floor(IN.stablePos*14));o.Albedo=_Color.rgb*(.5+grain*.7);o.Smoothness=.12;o.Occlusion=.75;o.Alpha=grain;
 } ENDCG } Fallback "Standard"
}
