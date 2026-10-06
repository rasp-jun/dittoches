"""Replace only Attack/Skill channels; preserve every existing mesh and other clip."""
import json,struct,copy,hashlib
from pathlib import Path

def chunks(path):
    raw=Path(path).read_bytes();size=struct.unpack_from('<I',raw,12)[0]
    return json.loads(raw[20:20+size]),raw[28+size:]

def merge(original,authored,output):
    doc,old=chunks(original);new,source=chunks(authored);payload=bytearray(old)
    names={n.get('name'):i for i,n in enumerate(doc['nodes'])}
    assert len(names)==len(doc['nodes']),'Ambiguous original node names'
    mapped={i:names[n['name']] for i,n in enumerate(new['nodes']) if n.get('name') in names}
    copied={}
    def accessor(index):
        if index in copied:return copied[index]
        record=copy.deepcopy(new['accessors'][index]);view=copy.deepcopy(new['bufferViews'][record['bufferView']])
        assert 'sparse' not in record and view['buffer']==0
        start=view.get('byteOffset',0);size=view['byteLength'];payload.extend(b'\0'*((-len(payload))%4))
        view['byteOffset']=len(payload);payload.extend(source[start:start+size]);record['bufferView']=len(doc['bufferViews']);doc['bufferViews'].append(view)
        copied[index]=len(doc['accessors']);doc['accessors'].append(record);return copied[index]
    replacements={}
    for action in new['animations']:
        if action['name'] not in ('Attack','Skill'):continue
        action=copy.deepcopy(action)
        for channel in action['channels']:channel['target']['node']=mapped[channel['target']['node']]
        for sampler in action['samplers']:
            sampler['input']=accessor(sampler['input']);sampler['output']=accessor(sampler['output'])
        replacements[action['name']]=action
    assert set(replacements)=={'Attack','Skill'}
    doc['animations']=[replacements.get(a['name'],a) for a in doc['animations']]
    doc['buffers'][0]['byteLength']=len(payload);payload.extend(b'\0'*((-len(payload))%4))
    encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
    result=struct.pack('<4sII',b'glTF',2,28+len(encoded)+len(payload))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(payload),0x004e4942)+payload
    Path(output).write_bytes(result)
    assert payload[:len(old)]==old
    return hashlib.sha256(result).hexdigest()
