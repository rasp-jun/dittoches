"""Check exported GLB contact shading, material colours and DTM3 vertex colours.

Run with ordinary Python, without Blender or third-party dependencies:
    python Tools/validate_model_surfaces.py --ids koromon,tentomon
    python Tools/validate_model_surfaces.py

This deliberately checks the *exported* buffers.  A Blender material/attribute
existing in memory is not evidence that the GLB actually carries that data.
"""
import argparse
import json
import math
import re
import struct
from pathlib import Path

from roster_designs import DESIGNS

ROOT=Path(__file__).resolve().parent.parent
COMPONENTS={5120:('b',1),5121:('B',1),5122:('h',2),5123:('H',2),5125:('I',4),5126:('f',4)}
WIDTHS={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}
EPSILON=4e-5


class Checks:
    def __init__(self,ident):self.ident=ident;self.count=0
    def __call__(self,condition,message):
        self.count+=1
        if not condition:raise AssertionError(self.ident+': '+message)


class GLB:
    def __init__(self,path,check):
        self.check=check;blob=path.read_bytes()
        check(len(blob)>=20,'truncated GLB header')
        magic,version,total=struct.unpack_from('<4sII',blob)
        check(magic==b'glTF' and version==2 and total==len(blob),'invalid GLB header')
        cursor=12;doc=None;self.binary=None
        while cursor<len(blob):
            check(cursor+8<=len(blob),'truncated GLB chunk header')
            size,kind=struct.unpack_from('<II',blob,cursor);cursor+=8
            check(cursor+size<=len(blob),'truncated GLB chunk payload')
            chunk=blob[cursor:cursor+size];cursor+=size
            if kind==0x4e4f534a:
                check(doc is None,'duplicate GLB JSON chunk');doc=json.loads(chunk)
            elif kind==0x004e4942:
                check(self.binary is None,'duplicate GLB BIN chunk');self.binary=chunk
        check(doc is not None and self.binary is not None,'missing GLB JSON/BIN payload')
        self.doc=doc;self.cache={}
        buffers=doc.get('buffers',[])
        check(len(buffers)==1 and 'uri' not in buffers[0],'expected embedded single-buffer GLB')
        check(buffers[0]['byteLength']<=len(self.binary),'GLB buffer length exceeds BIN payload')

    def rows(self,view_index,offset,count,component,width):
        check=self.check;views=self.doc.get('bufferViews',[])
        check(0<=view_index<len(views),'invalid accessor bufferView')
        view=views[view_index]
        check(view.get('buffer',0)==0 and component in COMPONENTS,'unsupported buffer/component type')
        code,size=COMPONENTS[component];packed=size*width;stride=view.get('byteStride',packed)
        check(stride>=packed and offset>=0,'invalid accessor stride/offset')
        end=offset+(count-1)*stride+packed if count else offset
        check(end<=view['byteLength'],'accessor exceeds its bufferView')
        origin=view.get('byteOffset',0)+offset
        check(origin+max(0,end-offset)<=len(self.binary),'accessor exceeds GLB BIN payload')
        return [struct.unpack_from('<'+code*width,self.binary,origin+i*stride) for i in range(count)]

    def accessor(self,index):
        if index in self.cache:return self.cache[index]
        check=self.check;accessors=self.doc.get('accessors',[])
        check(isinstance(index,int) and 0<=index<len(accessors),'invalid accessor index')
        accessor=accessors[index];component=accessor['componentType'];count=accessor['count']
        check(accessor['type'] in WIDTHS and count>0,'unsupported or empty mesh accessor')
        width=WIDTHS[accessor['type']]
        values=self.rows(accessor['bufferView'],accessor.get('byteOffset',0),count,component,width) if 'bufferView' in accessor else [(0,)*width for _ in range(count)]
        if 'sparse' in accessor:
            sparse=accessor['sparse'];indices=sparse['indices'];replacement=sparse['values']
            sparse_indices=self.rows(indices['bufferView'],indices.get('byteOffset',0),sparse['count'],indices['componentType'],1)
            sparse_values=self.rows(replacement['bufferView'],replacement.get('byteOffset',0),sparse['count'],component,width)
            for (at,),row in zip(sparse_indices,sparse_values):
                check(0<=at<count,'sparse accessor index outside vertex range');values[at]=row
        if accessor.get('normalized',False):
            check(component!=5126,'floating accessor cannot be normalized')
            divisor={5120:127,5121:255,5122:32767,5123:65535,5125:4294967295}[component]
            signed=component in (5120,5122)
            values=[tuple(max(-1,v/divisor) if signed else v/divisor for v in row) for row in values]
        self.cache[index]=values
        return values


def srgb_to_linear(value):
    return value/12.92 if value<=.04045 else ((value+.055)/1.055)**2.4


def check_glb(path,check):
    glb=GLB(path,check);doc=glb.doc;materials=doc.get('materials',[])
    check(bool(materials),'missing GLB materials')
    palette=set();material_colours={};default_white=0
    for index,material in enumerate(materials):
        pbr=material.get('pbrMetallicRoughness')
        check(isinstance(pbr,dict),'missing PBR material block: '+material.get('name',str(index)))
        factor=pbr.get('baseColorFactor',[1,1,1,1])
        check(len(factor)==4 and all(math.isfinite(x) and -EPSILON<=x<=1+EPSILON for x in factor),'invalid PBR baseColorFactor')
        check(abs(factor[3]-1)<EPSILON,'unexpected material alpha in opaque character export')
        # Roster material names encode the authoring colour.  This catches a
        # white Principled BSDF even when diffuse_color still looks correct.
        encoded=re.match(r'^#([0-9a-fA-F]{6})(?:\.|$)',material.get('name',''))
        if encoded:
            hex_colour=encoded.group(1)
            expected=[srgb_to_linear(int(hex_colour[i:i+2],16)/255) for i in (0,2,4)]
            check(max(abs(a-b) for a,b in zip(factor[:3],expected))<EPSILON,
                  'PBR colour differs from original '+material.get('name',''))
            if max(abs(v-1) for v in expected)>EPSILON:
                check('baseColorFactor' in pbr,'non-white source colour lost from PBR material')
        if 'baseColorFactor' not in pbr:default_white+=1
        material_colours[index]=factor
        palette.add(tuple(round(v,5) for v in factor[:3]))
    check(len(palette)>=2,'all PBR materials collapsed to one colour')
    check(any(max(rgb)-min(rgb)>.025 for rgb in palette),'original chromatic palette was lost')
    check(any(max(abs(c-1) for c in rgb)>.20 for rgb in palette),'all PBR colours are near white')
    primitives=0;vertices=0;minimum=1;maximum=0;normal_accessors=set();colour_accessors=set();effective_sum=[0.,0.,0.]
    for mesh in doc.get('meshes',[]):
        for primitive in mesh.get('primitives',[]):
            primitives+=1;attrs=primitive.get('attributes',{})
            label=mesh.get('name','mesh')
            check({'POSITION','NORMAL','COLOR_0'}.issubset(attrs),'missing NORMAL/COLOR_0 in '+label)
            check(not any(key.startswith('COLOR_') and key!='COLOR_0' for key in attrs),'unexpected extra colour set; ContactAO must be COLOR_0 only')
            check('material' in primitive and primitive['material'] in material_colours,'primitive has no valid material')
            normals=glb.accessor(attrs['NORMAL']);colours=glb.accessor(attrs['COLOR_0'])
            position=doc['accessors'][attrs['POSITION']]
            check(len(normals)==len(colours)==position['count'],'position/normal/colour vertex counts differ')
            if attrs['NORMAL'] not in normal_accessors:
                for normal in normals:
                    check(len(normal)==3 and all(math.isfinite(v) for v in normal),'non-finite GLB normal')
                    check(abs(sum(v*v for v in normal)-1)<.002,'GLB normal is not unit length')
                normal_accessors.add(attrs['NORMAL'])
            if attrs['COLOR_0'] not in colour_accessors:
                for colour in colours:
                    check(len(colour) in (3,4) and all(math.isfinite(v) for v in colour),'non-finite AO colour')
                    check(all(.5-EPSILON<=v<=1+EPSILON for v in colour[:3]),'AO outside expected 0.5..1 range')
                    check(max(colour[:3])-min(colour[:3])<=EPSILON,'COLOR_0 contains tint instead of grayscale ContactAO')
                    check(len(colour)==3 or abs(colour[3]-1)<EPSILON,'AO alpha is not opaque')
                    minimum=min(minimum,colour[0]);maximum=max(maximum,colour[0])
                colour_accessors.add(attrs['COLOR_0'])
            tint=material_colours[primitive['material']]
            total_ao=sum(row[0] for row in colours)
            for i in range(3):effective_sum[i]+=tint[i]*total_ao
            vertices+=len(colours)
    check(primitives>0 and vertices>0,'GLB contains no character mesh primitives')
    check(minimum<.99,'ContactAO contains no contact shading anywhere')
    effective_mean=[x/vertices for x in effective_sum]
    check(min(effective_mean)<.90,'rendered vertex/material product washed out to white')
    return {'primitives':primitives,'vertices':vertices,'materials':len(materials),'palette_colours':len(palette),
            'default_white_materials':default_white,'ao_min':minimum,'ao_max':maximum,
            'effective_mean_rgb':effective_mean,'unique_normal_accessors':len(normal_accessors)}


def check_dtm3(path,check):
    blob=path.read_bytes();cursor=4
    check(len(blob)>=24 and blob[:4]==b'DTM3','missing DTM3 surface payload')
    version,bones,count,triangles,clips=struct.unpack_from('<5i',blob,cursor);cursor+=20
    check(version==1 and bones>0 and count>0,'invalid DTM3 header')
    for _ in range(bones):
        check(cursor+4<=len(blob),'truncated DTM3 bone header')
        length=struct.unpack_from('<i',blob,cursor)[0];cursor+=4
        check(0<length<1024 and cursor+length+16<=len(blob),'truncated DTM3 bone')
        cursor+=length+16
    stride=struct.calcsize('<10f4i4f')
    check(cursor+count*stride<=len(blob),'truncated DTM3 vertex payload')
    minimum=[1.,1.,1.];maximum=[0.,0.,0.];sums=[0.,0.,0.];palette=set();chromatic=0
    for index in range(count):
        values=struct.unpack_from('<10f',blob,cursor+index*stride)
        normal=values[3:6];colour=values[6:10]
        check(all(math.isfinite(v) for v in normal+colour),'non-finite DTM3 surface')
        check(abs(sum(v*v for v in normal)-1)<.002,'DTM3 normal is not unit length')
        check(all(-EPSILON<=v<=1+EPSILON for v in colour),'DTM3 vertex colour outside 0..1')
        check(abs(colour[3]-1)<EPSILON,'DTM3 colour alpha is not opaque')
        for channel in range(3):
            minimum[channel]=min(minimum[channel],colour[channel]);maximum[channel]=max(maximum[channel],colour[channel]);sums[channel]+=colour[channel]
        palette.add(tuple(round(v,3) for v in colour[:3]))
        if max(colour[:3])-min(colour[:3])>.025:chromatic+=1
    means=[v/count for v in sums]
    check(len(palette)>=3,'DTM3 colours collapsed to a flat colour')
    check(chromatic>0,'DTM3 original chromatic palette was lost')
    check(min(means)<.90,'DTM3 colours washed out to white')
    return {'vertices':count,'colour_min_rgb':minimum,'colour_max_rgb':maximum,'mean_rgb':means,
            'quantized_colours':len(palette),'chromatic_vertices':chromatic}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ids',default='',help='Comma-separated subset; default validates all 34 characters')
    parser.add_argument('--report',type=Path,help='Optional JSON report path')
    args=parser.parse_args();known=[row[0] for row in DESIGNS]
    requested=[part.strip() for part in args.ids.split(',') if part.strip()] if args.ids else known
    unknown=set(requested)-set(known)
    if unknown:parser.error('Unknown character IDs: '+','.join(sorted(unknown)))
    ids=list(dict.fromkeys(requested));results=[];failures=[];total=0
    for ident in ids:
        check=Checks(ident)
        try:
            glb=ROOT/'ArtSource'/('Agumon/Agumon.glb' if ident=='agumon' else 'Roster/'+ident+'/'+ident+'.glb')
            dtm=ROOT/'Assets/Resources/Models'/('Agumon/Agumon.bytes' if ident=='agumon' else 'Roster/'+ident+'.bytes')
            surface=check_glb(glb,check);portable=check_dtm3(dtm,check)
            result={'id':ident,'checks':check.count,'glb':surface,'dtm3':portable};results.append(result)
            print('SURFACE PASS',ident,'primitives='+str(surface['primitives']),
                  'AO='+format(surface['ao_min'],'.4f')+'..'+format(surface['ao_max'],'.4f'),
                  'palette='+str(surface['palette_colours']),flush=True)
        except (AssertionError,ValueError,KeyError,IndexError,OSError,struct.error) as exc:
            failures.append({'id':ident,'error':str(exc)});print('SURFACE FAIL',ident,str(exc),flush=True)
        total+=check.count
    report={'passed':not failures,'characters':len(results),'requested_characters':len(ids),'checks':total,
            'models':results,'failures':failures,'scope':'Exported GLB surface buffers/materials and portable DTM3 vertex colours; no visual or motion equivalence claim.'}
    destination=args.report or ROOT/'ArtSource/Roster'/('surface-validation-report-subset.json' if args.ids else 'surface-validation-report.json')
    destination.parent.mkdir(parents=True,exist_ok=True);destination.write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('MODEL SURFACES '+('PASS' if not failures else 'FAIL'),total,'checks;',len(results),'/',len(ids),'characters;',len(failures),'failures')
    raise SystemExit(1 if failures else 0)


if __name__=='__main__':main()
