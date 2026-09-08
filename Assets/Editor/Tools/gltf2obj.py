"""glTF 2.0 (.gltf+.bin or .glb) -> OBJ. Positions, normals, UVs, triangles; applies node transforms.
Dependency-free on purpose: this Mac has no Blender, assimp or glTF library, and installing any of
them just to unpack a spear is not a decision to make on someone else's machine."""
import json, struct, sys, os, math

def load(path):
    if path.lower().endswith('.glb'):
        with open(path, 'rb') as f: data = f.read()
        magic, ver, length = struct.unpack_from('<4sII', data, 0)
        off = 12; gltf = None; bins = []
        while off < length:
            clen, ctype = struct.unpack_from('<II', data, off); off += 8
            chunk = data[off:off+clen]; off += clen
            if ctype == 0x4E4F534A: gltf = json.loads(chunk.decode('utf-8'))
            elif ctype == 0x004E4942: bins.append(chunk)
        buffers = bins
    else:
        gltf = json.load(open(path))
        base = os.path.dirname(path)
        buffers = []
        for b in gltf['buffers']:
            uri = b['uri']
            if uri.startswith('data:'):
                import base64; buffers.append(base64.b64decode(uri.split(',',1)[1]))
            else: buffers.append(open(os.path.join(base, uri),'rb').read())
    return gltf, buffers

CT = {5120:('b',1),5121:('B',1),5122:('h',2),5123:('H',2),5125:('I',4),5126:('f',4)}
NC = {'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}

def accessor(gltf, buffers, idx):
    a = gltf['accessors'][idx]; bv = gltf['bufferViews'][a['bufferView']]
    buf = buffers[bv['buffer']]; fmt, sz = CT[a['componentType']]; n = NC[a['type']]
    start = bv.get('byteOffset',0) + a.get('byteOffset',0)
    stride = bv.get('byteStride', sz*n)
    out = []
    for i in range(a['count']):
        o = start + i*stride
        out.append(struct.unpack_from('<'+fmt*n, buf, o))
    return out

def mat_mul(a, b):  # column-major 4x4 lists
    r = [0.0]*16
    for c in range(4):
        for rr in range(4):
            r[c*4+rr] = sum(a[k*4+rr]*b[c*4+k] for k in range(4))
    return r

def node_matrix(n):
    if 'matrix' in n: return n['matrix']
    t = n.get('translation',[0,0,0]); q = n.get('rotation',[0,0,0,1]); s = n.get('scale',[1,1,1])
    x,y,z,w = q
    R = [1-2*(y*y+z*z), 2*(x*y+z*w), 2*(x*z-y*w), 0,
         2*(x*y-z*w), 1-2*(x*x+z*z), 2*(y*z+x*w), 0,
         2*(x*z+y*w), 2*(y*z-x*w), 1-2*(x*x+y*y), 0,
         0,0,0,1]
    S = [s[0],0,0,0, 0,s[1],0,0, 0,0,s[2],0, 0,0,0,1]
    T = [1,0,0,0, 0,1,0,0, 0,0,1,0, t[0],t[1],t[2],1]
    return mat_mul(T, mat_mul(R, S))

def xform_p(m, p):
    x,y,z = p
    return (m[0]*x+m[4]*y+m[8]*z+m[12], m[1]*x+m[5]*y+m[9]*z+m[13], m[2]*x+m[6]*y+m[10]*z+m[14])
def xform_n(m, p):
    x,y,z = p
    v = (m[0]*x+m[4]*y+m[8]*z, m[1]*x+m[5]*y+m[9]*z, m[2]*x+m[6]*y+m[10]*z)
    l = math.sqrt(sum(c*c for c in v)) or 1.0
    return tuple(c/l for c in v)

def convert(src, dst):
    gltf, buffers = load(src)
    V=[]; N=[]; UV=[]; F=[]; groups=[]
    def walk(ni, parent):
        n = gltf['nodes'][ni]; m = mat_mul(parent, node_matrix(n))
        if 'mesh' in n:
            mesh = gltf['meshes'][n['mesh']]
            for pi, prim in enumerate(mesh.get('primitives', [])):
                if prim.get('mode',4) != 4: continue
                base = len(V)
                pos = accessor(gltf, buffers, prim['attributes']['POSITION'])
                nrm = accessor(gltf, buffers, prim['attributes']['NORMAL']) if 'NORMAL' in prim['attributes'] else None
                tex = accessor(gltf, buffers, prim['attributes']['TEXCOORD_0']) if 'TEXCOORD_0' in prim['attributes'] else None
                for i,p in enumerate(pos):
                    V.append(xform_p(m, p))
                    N.append(xform_n(m, nrm[i]) if nrm else None)
                    UV.append(tex[i] if tex else None)
                idx = [t[0] for t in accessor(gltf, buffers, prim['indices'])] if 'indices' in prim else list(range(len(pos)))
                groups.append((f"{mesh.get('name','mesh')}_{pi}", len(F)))
                for i in range(0, len(idx)-2, 3):
                    F.append((base+idx[i], base+idx[i+1], base+idx[i+2]))
        for c in n.get('children', []): walk(c, m)
    I = [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]
    scene = gltf['scenes'][gltf.get('scene',0)]
    for r in scene['nodes']: walk(r, I)
    with open(dst,'w') as o:
        o.write("# converted from glTF by gltf2obj.py\n")
        for v in V: o.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        for t in UV: o.write(f"vt {t[0]:.6f} {1-t[1]:.6f}\n" if t else "vt 0 0\n")
        for n in N: o.write(f"vn {n[0]:.6f} {n[1]:.6f} {n[2]:.6f}\n" if n else "vn 0 1 0\n")
        gi = 0
        for fi, f in enumerate(F):
            while gi < len(groups) and groups[gi][1] == fi:
                o.write(f"g {groups[gi][0]}\n"); gi += 1
            a,b,c = (x+1 for x in f)
            o.write(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}\n")
    print(f"{os.path.basename(src)}: {len(V)} verts, {len(F)} tris, {len(groups)} groups -> {dst}")

if __name__ == '__main__':
    convert(sys.argv[1], sys.argv[2])
