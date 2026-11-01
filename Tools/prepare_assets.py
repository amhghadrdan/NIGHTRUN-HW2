#!/usr/bin/env python3
"""Convert the attributed glTF car to Unity-native OBJ and author original loopable audio.
Requires Python 3 + numpy. All transforms are baked; no runtime glTF dependency.
"""
from pathlib import Path
import json, struct, wave, zipfile
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
p=ROOT/'Assets/ThirdParty/Vehicles/CarConcept.glb'
b=p.read_bytes(); n=struct.unpack_from('<I',b,12)[0]; doc=json.loads(b[20:20+n]); binary=b[28+n:]
types={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
dtypes={5120:'i1',5121:'u1',5122:'<i2',5123:'<u2',5125:'<u4',5126:'<f4'}
def acc(index):
 a=doc['accessors'][index]; v=doc['bufferViews'][a['bufferView']];dt=np.dtype(dtypes[a['componentType']]);c=types[a['type']];off=v.get('byteOffset',0)+a.get('byteOffset',0)
 return np.ndarray((a['count'],c),dtype=dt,buffer=binary,offset=off,strides=(v.get('byteStride',c*dt.itemsize),dt.itemsize)).copy()
def matrix(node):
 if 'matrix' in node:return np.array(node['matrix']).reshape(4,4).T
 x,y,z,w=node.get('rotation',[0,0,0,1]);m=np.eye(4);m[:3,:3]=[[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]];m[:3,:3]=m[:3,:3]@np.diag(node.get('scale',[1,1,1]));m[:3,3]=node.get('translation',[0,0,0]);return m
parts={};allpoints=[]
def visit(i,parent):
 node=doc['nodes'][i];m=parent@matrix(node)
 if 'mesh' in node:
  for prim in doc['meshes'][node['mesh']]['primitives']:
   attrs=prim['attributes'];p=acc(attrs['POSITION']);p=(np.c_[p,np.ones(len(p))]@m.T)[:,:3];norm=acc(attrs['NORMAL'])@np.linalg.inv(m[:3,:3]);norm/=np.maximum(np.linalg.norm(norm,axis=1)[:,None],1e-8);uv=acc(attrs['TEXCOORD_0']) if 'TEXCOORD_0'in attrs else np.zeros((len(p),2));idx=acc(prim['indices']).astype(np.int64).reshape(-1,3)
   if np.linalg.det(m[:3,:3])<0:idx=idx[:,::-1]
   mat=prim.get('material',0);parts.setdefault(mat,[]).append((p,norm,uv,idx));allpoints.append(p)
 for child in node.get('children',[]):visit(child,m)
for node in doc['scenes'][doc.get('scene',0)]['nodes']:visit(node,np.eye(4))
points=np.concatenate(allpoints);lo,hi=points.min(0),points.max(0);print('Car bounds:',lo,hi,'size',hi-lo)
# Uniformly normalize to a 2.05m width. glTF car faces +Z; Unity's OBJ import flips X.
scale=2.05/(hi[0]-lo[0]);center=np.array([(lo[0]+hi[0])/2,lo[1],(lo[2]+hi[2])/2])
out=ROOT/'Assets/ThirdParty/Vehicles/Sportscar.obj';mtl=out.with_suffix('.mtl');offset=1
with out.open('w') as f,mtl.open('w') as mf:
 f.write('# Car Concept / Eric Chadwick, DGG / CC BY 4.0\nmtllib Sportscar.mtl\n')
 for mat,meshes in sorted(parts.items()):
  material=doc['materials'][mat];name=material.get('name','Dark').replace(' ','_');f.write(f'\no {name}\nusemtl {name}\n')
  pbr=material.get('pbrMetallicRoughness',{});col=pbr.get('baseColorFactor',[.16,.16,.16,1]);mf.write('newmtl '+name+'\nKd '+' '.join(str(max(c,.02)**(1/2.2)) for c in col[:3])+'\nKs 0.6 0.6 0.6\nNs 160\n\n')
  for pos,norm,uv,idx in meshes:
   pos=(pos-center)*scale
   for x in pos:f.write('v %.6f %.6f %.6f\n'%tuple(x))
   for x in uv:f.write('vt %.6f %.6f\n'%tuple(x))
   for x in norm:f.write('vn %.6f %.6f %.6f\n'%tuple(x))
   for face in idx:
    ids=face+offset;f.write('f '+' '.join(f'{i}/{i}/{i}' for i in ids)+'\n')
   offset+=len(pos)
print('Exported',offset-1,'vertices;',out.stat().st_size,'bytes')
# The source GLB is retained for reproducibility. Only the referenced OBJ enters player builds.
# Source URLs and attribution are documented in THIRD_PARTY.txt.
audio=ROOT/'Assets/NightRun/Resources/Audio';audio.mkdir(exist_ok=True,parents=True)
sr=24000;rng=np.random.default_rng(42)
def wav(name,a):
 a=np.clip(a,-.97,.97);p=audio/(name+'.wav')
 with wave.open(str(p),'wb')as w:w.setnchannels(1 if a.ndim==1 else a.shape[1]);w.setsampwidth(2);w.setframerate(sr);w.writeframes((a*32767).astype('<i2').tobytes())
def tone(freq,duration,decay=10):
 t=np.arange(int(sr*duration))/sr;return np.sin(2*np.pi*freq*t)*np.exp(-decay*t)
t=np.arange(sr*2)/sr
engine=sum(np.sin(2*np.pi*freq*t+.15*np.sin(2*np.pi*8*t))*(.28/i) for i,freq in enumerate((48,96,144,192,288),1));engine+=.035*rng.normal(0,1,len(t));wav('engine',engine*.7)
for name,freq in [('coin',1046.5),('fuel',659.25),('countdown',880),('click',420)]:
 a=tone(freq,.32,16)+.45*tone(freq*1.5,.32,13);a*=np.minimum(np.arange(len(a))/120,1);wav(name,a*.3)
t=np.arange(int(sr*.5))/sr;wav('impact',(.65*rng.normal(0,1,len(t))+.4*np.sin(2*np.pi*63*t))*np.exp(-11*t))
t=np.arange(sr)/sr;noise=rng.normal(0,1,sr);noise=np.convolve(noise,np.ones(6)/6,mode='same');wav('nitro',noise*.35+np.sin(2*np.pi*96*t)*.08)
# Original 16-bar synthwave loop, 128 BPM, F# minor. Deterministic and seamless at bar boundaries.
beat=60/128;duration=16*4*beat;length=int(duration*sr);music=np.zeros((length,2));chords=[(42,54,57,61),(38,50,54,57),(45,57,61,64),(40,52,56,59)]
def note(midi):return 440*2**((midi-69)/12)
def add(a,start,pan=0):
 i=int(start*sr);end=min(length,i+len(a));a=a[:end-i];music[i:end,0]+=a*np.sqrt((1-pan)/2);music[i:end,1]+=a*np.sqrt((1+pan)/2)
for bar in range(16):
 root,*chord=chords[(bar//2)%4]
 for bidx in range(4):
  start=(bar*4+bidx)*beat;tt=np.arange(int(sr*.3))/sr
  kick=np.sin(2*np.pi*(45*tt+2.6*(1-np.exp(-tt*35))))*np.exp(-tt*20);add(kick*.37,start)
  if bidx%2:
   tt=np.arange(int(sr*.16))/sr;snare=(rng.normal(0,1,len(tt))*.5+np.sin(2*np.pi*180*tt)*.25)*np.exp(-tt*29);add(snare*.23,start)
 for eighth in range(8):
  start=(bar*4+eighth*.5)*beat;tt=np.arange(int(sr*beat*.42))/sr;f=note(root+(12 if eighth%4==3 else 0));bass=(np.sin(2*np.pi*f*tt)+.3*np.sin(2*np.pi*f*2*tt))*np.exp(-tt*10);add(bass*.18,start)
  tt=np.arange(int(sr*.06))/sr;hat=np.diff(rng.normal(0,1,len(tt)+1))*np.exp(-tt*70);add(hat*.023,start,.3 if eighth%2 else -.3)
 for sixteenth in range(16):
  start=(bar*4+sixteenth*.25)*beat;f=note(chord[sixteenth%3]+12);tt=np.arange(int(sr*.27))/sr;a=(np.sin(2*np.pi*f*tt)+.25*np.sin(2*np.pi*f*2*tt))*np.exp(-tt*18)*.075;add(a,start,(-.5 if sixteenth%2 else .5));add(a*.25,(start+beat*.75)%duration,-.5 if sixteenth%2 else .5)
# A soft sustained chorus with per-beat sidechain breathing.
tt=np.arange(int(sr*beat*4))/sr
for bar in range(16):
 a=np.zeros(len(tt));root,*chord=chords[(bar//2)%4]
 for m in chord:
  f=note(m);a+=(np.sin(2*np.pi*f*tt)+.22*np.sin(2*np.pi*(f*1.003)*tt))*.017
 env=np.minimum(tt/.18,1)*np.minimum((beat*4-tt)/.2,1);side=.35+.65*(np.mod(tt,beat)/beat);add(a*env*side,bar*4*beat)
music=np.tanh(music*1.3)*.85;wav('nightdrive',music)
# Kenney UI click, public domain. Keep original included license.
z=Path('/tmp/nightrun-ui-audio.zip')
if z.exists():
 with zipfile.ZipFile(z)as archive:
  for name in archive.namelist():
   if name.endswith('click1.ogg')or name.lower().endswith('license.txt'):
    target=ROOT/'Assets/ThirdParty/Audio'/Path(name).name;target.write_bytes(archive.read(name));print('Kenney:',target.name)
print('Original audio authored:',duration,'seconds of music')
