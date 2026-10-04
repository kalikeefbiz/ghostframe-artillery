#!/usr/bin/env python3
"""Byte/reference/source regression checks. Does not execute Unity."""
from pathlib import Path
import hashlib,json,re,subprocess,sys
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
def require(value,message):
    if not value:raise SystemExit(message)
def blob(data):return hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
manifest=json.loads((root/'Docs/M2.1-SOURCE-MANIFEST.json').read_text())
for path,digest in manifest['production_pngs'].items():
    p=root/path
    require(p.is_file(),'Missing repository production art: '+path)
    require(blob(p.read_bytes())==digest,'Production PNG changed: '+path)
    data=(root/(path+'.meta')).read_text()
    for setting in ['textureType: 8','spriteMode: 1','alphaIsTransparency: 1','textureCompression: 0','maxTextureSize: 4096','spriteMeshType: 0']:
        require(setting in data,'Bad import setting '+path+': '+setting)
for path,digest in manifest['unchanged_main'].items():
    require(blob((root/path).read_bytes())==digest,'Unexpected change to validated source: '+path)
for name in ['AetherBolt','LilBomb','Fault','Wall','SummonersStep','Brace']:
    path='Assets/AetherWild/Data/'+name+'.asset'
    data=re.sub(rb'^  icon:.*$',b'  icon: {fileID: 0}',(root/path).read_bytes(),flags=re.M)
    require(blob(data)==manifest['baseline_blobs'][path],'Sigil mechanics changed: '+name)
scene='Assets/AetherWild/Scenes/Foundation.unity'
data=re.sub(rb'^  productionArt:.*\n',b'',(root/scene).read_bytes(),flags=re.M)
require(blob(data)==manifest['baseline_blobs'][scene],'Scene changed beyond art reference')
presentation=(root/'Assets/AetherWild/Runtime/MaePresentation.cs').read_text()
require('body.position =' not in presentation and 'ResetPosition(' not in presentation,'Presentation must not move physics')
require('Loadout.Spend(' not in presentation and '.Damage(' not in presentation,'Presentation must not perform combat')
require('AssetDatabase' not in presentation,'Runtime art must use serialized references')
ai=(root/'Assets/AetherWild/Runtime/Combat/SliceAI.cs').read_text()
require('position.y<target.y-2' not in ai,'AI trench teleport trigger returned')
shader=(root/'Assets/AetherWild/Shaders/WildsTerrain.shader').read_text()
require('if(solid<.5) return fixed4(0,0,0,0)' in shader,'Terrain must reveal background')
require('.045,.065,.075' not in shader,'Dark filler returned')
require(len(manifest['production_pngs'])==28,'Production asset inventory changed')
subprocess.run([sys.executable,str(root/'Tools/verify_source.py'),str(root)],check=True)
print('PASS: 28 repository PNGs unchanged, sprite metadata/references, preserved main files, all six Sigil mechanics and presentation separation.')
print('Unity compilation, rendering, physics and iPhone tests remain pending.')
