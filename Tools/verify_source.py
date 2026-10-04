#!/usr/bin/env python3
"""Source/data integrity only. Not a Unity compiler or physics test."""
from pathlib import Path
import hashlib,json,re,sys
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
def check(condition,message):
    if not condition: raise SystemExit(message)
json.loads((root/'Packages/manifest.json').read_text())
guids={}
for p in (root/'Assets').rglob('*.meta'):
    m=re.search(r'^guid: ([a-f0-9]{32})$',p.read_text(),re.M)
    check(m is not None,'Bad GUID '+str(p))
    check(m[1] not in guids,'Duplicate GUID '+str(p));guids[m[1]]=p
for p in (root/'Assets').rglob('*'):
    if p.suffix=='.meta' or p.name.startswith('.'):continue
    check(Path(str(p)+'.meta').exists(),'Missing meta '+str(p))
    if p.is_file() and p.suffix in ('.cs','.asset','.unity','.shader'):
        for g in re.findall(r'guid: ([a-f0-9]{32})',p.read_text()):
            check(g in guids,'Unresolved reference '+str(p))
check('m_EditorVersion: 6000.0.60f1\n' in (root/'ProjectSettings/ProjectVersion.txt').read_text(),'Unity version changed')
evidence=json.loads((root/'Docs/M2-SOURCE-EVIDENCE.json').read_text())
for path,digest in evidence['preserved'].items():
    content=(root/path).read_bytes()
    if path=='Assets/AetherWild/Scenes/Foundation.unity':
        content=re.sub(rb'^  productionArt:.*\n',b'',content,flags=re.M)
    check(hashlib.sha256(content).hexdigest()==digest,'Preserved source changed '+path)
check(hashlib.sha256((root/'Assets/AetherWild/Art/WildsDepth1.jpeg').read_bytes()).hexdigest()==evidence['art_sha256'],'Original artwork changed')
def field(text,key):
    m=re.search(r'^  '+key+r': (.+)$',text,re.M)
    check(m is not None,'Missing field '+key)
    return m[1]
names=['AetherBolt','LilBomb','Fault','Wall','SummonersStep','Brace']
uses=[0,3,2,2,2,2];classes=[4,4,3,2,0,1]
for i,name in enumerate(names):
    data=(root/('Assets/AetherWild/Data/'+name+'.asset')).read_text()
    check(int(field(data,'maxUses'))==uses[i],'Uses '+name)
    check(int(field(data,'classAffinity'))==classes[i],'Affinity '+name)
    check(int(field(data,'unlimitedUses'))==(1 if i==0 else 0),'Unlimited '+name)
    check(float(field(data,'resonanceBonusPercent'))==5,'Bonus '+name)
mae=(root/'Assets/AetherWild/Data/Mae.asset').read_text()
check(len(re.findall(r'^  - \{fileID: 11400000',mae,re.M))==6,'Six-Sigil loadout')
check(field(mae,'summonerClass')=='1','Mae must remain Conduit')
terrain=(root/'Assets/AetherWild/Data/Battlefield.asset').read_text()
points=[tuple(map(float,m)) for m in re.findall(r'^  - \{x: ([-.\d]+), y: ([-.\d]+)\}',terrain,re.M)]
check(len(points)==14,'Authored surface')
check(all(points[i][0]<points[i+1][0] for i in range(13)),'Surface ordering')
check(max(abs((b[1]-a[1])/(b[0]-a[0])) for a,b in zip(points,points[1:]))<=1,'Ramp steeper than 45 degrees')
check(points[6][1]>points[3][1]+1.1,'Low shot not blocked')
for p in root.rglob('*'):
    check(not any(c.lower() in ('library','temp','logs','obj','build','builds','buildcache','.git','__pycache__') for c in p.relative_to(root).parts),'Cache in source '+str(p))
print('PASS: Unity version, manifests, GUIDs, references, original art, preserved M1 files, six Sigil parameters, collision profile.')
print('Unity compilation, shaders, collision physics, WebGL and iPhone acceptance remain pending.')
