Shader "NightRun/NightSky" {
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; }; struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
 float hash(float2 p){float3 p3=frac(float3(p.xyx)*.1031);p3+=dot(p3,p3.yzx+33.33);return frac((p3.x+p3.y)*p3.z);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float fbm(float2 p){float n=0,a=.5;for(int j=0;j<5;j++){n+=noise(p)*a;p=mul(float2x2(.8,-.6,.6,.8),p)*2.05+3.1;a*=.5;}return n;}
 float4 frag(v2f i):SV_Target {
 float3 d=normalize(i.dir);float h=saturate(d.y);
 float3 sky=lerp(float3(.045,.052,.068),float3(.009,.016,.032),smoothstep(0,.65,h));
 float3 moonDir=normalize(float3(.39,.3,1));float moonDot=dot(d,moonDir);
 float2 starUV=float2(atan2(d.x,d.z),asin(d.y))*300;
 float stars=step(.997,hash(floor(starUV)))*pow(saturate(1-length(frac(starUV)-.5)*2),14)*smoothstep(0,.25,h);
 sky+=stars*.75;
 float2 uv=d.xz/(max(d.y,0)*1.2+.22)*1.6+float2(_Time.y*.0015,0);
 float cloud=fbm(uv+fbm(uv*1.1+6)*1.4);
 float density=smoothstep(.4,.72,cloud)*smoothstep(-.04,.15,d.y);
 float lit=pow(saturate(moonDot),18);
 float edge=saturate((fbm(uv+float2(.15,.08))-cloud)*5+.35);
 float3 cloudColor=lerp(float3(.033,.041,.056),float3(.16,.18,.21),edge*.6+lit*.4);
 sky=lerp(sky,cloudColor,density*.86);
 float disk=smoothstep(.99912,.99918,moonDot);
 float2 moonUV=float2(dot(d-moonDir,normalize(cross(float3(0,1,0),moonDir))),d.y-moonDir.y)*35;
 float craters=.35+.5*fbm(moonUV*9)+.15*noise(moonUV*45);
 float3 moonColor=float3(.95,1.0,1.05)*craters*.7;
 sky=lerp(sky,moonColor,disk*(1-density*.75));
 sky+=float3(.035,.043,.055)*pow(saturate(moonDot),180)*(1-density*.5);
 return float4(sky,1);
 } ENDCG } } Fallback Off
}
