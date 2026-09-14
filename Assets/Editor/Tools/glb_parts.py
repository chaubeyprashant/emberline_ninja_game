"""GLB -> one OBJ per part (usemtl per material) + base-colour textures. Dependency-free.
A 'part' is a mesh-bearing node; parts can be merged into one OBJ or written separately."""
import json, struct, sys, os, math, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gltf2obj import accessor, mat_mul, node_matrix, xform_p, xform_n

def load_glb(path):
    data = open(path, 'rb').read(); off = 12; g = None; bins = []
    while off < len(data):
        clen, ct = struct.unpack_from('<II', data, off); off += 8
        chunk = data[off:off+clen]; off += clen
        if ct == 0x4E4F534A: g = json.loads(chunk)
        elif ct == 0x004E4942: bins.append(chunk)
    return g, bins

def parts(g, bins):
    out = []
    I = [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]
    def walk(ni, parent):
        n = g['nodes'][ni]; m = mat_mul(parent, node_matrix(n))
        if 'mesh' in n:
            prims = []
            for pr in g['meshes'][n['mesh']]['primitives']:
                if pr.get('mode', 4) != 4: continue
                a = pr['attributes']
                pos = [xform_p(m, p) for p in accessor(g, bins, a['POSITION'])]
                nrm = [xform_n(m, p) for p in accessor(g, bins, a['NORMAL'])] if 'NORMAL' in a else None
                uv = accessor(g, bins, a['TEXCOORD_0']) if 'TEXCOORD_0' in a else None
                idx = [t[0] for t in accessor(g, bins, pr['indices'])] if 'indices' in pr else list(range(len(pos)))
                prims.append((pos, nrm, uv, idx, pr.get('material', 0)))
            out.append((n.get('name', f'node{ni}'), prims))
        for c in n.get('children', []): walk(c, m)
    for r in g['scenes'][g.get('scene', 0)]['nodes']: walk(r, I)
    return out

def write_obj(dst, plist, g):
    V=[]; N=[]; UV=[]; faces=[]
    for name, prims in plist:
        for pos, nrm, uv, idx, mi in prims:
            base = len(V)
            V += pos; N += (nrm or [(0,1,0)]*len(pos)); UV += (uv or [(0,0)]*len(pos))
            mname = re.sub(r'\W', '_', g['materials'][mi].get('name', f'mat{mi}')) if g.get('materials') else 'mat'
            faces.append((mname, [(base+idx[i], base+idx[i+1], base+idx[i+2]) for i in range(0, len(idx)-2, 3)]))
    mn = [min(v[k] for v in V) for k in range(3)]; mx = [max(v[k] for v in V) for k in range(3)]
    with open(dst, 'w') as o:
        o.write("# converted from glTF by glb_parts.py\n")
        for v in V: o.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        for t in UV: o.write(f"vt {t[0]:.6f} {1-t[1]:.6f}\n")
        for n in N: o.write(f"vn {n[0]:.6f} {n[1]:.6f} {n[2]:.6f}\n")
        tris = 0
        for mname, fs in faces:
            o.write(f"g {mname}\nusemtl {mname}\n")
            for a,b,c in fs:
                a+=1; b+=1; c+=1; o.write(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}\n"); tris += 1
    print(f"  {os.path.basename(dst):32} tris={tris:6} size=({mx[0]-mn[0]:.3f},{mx[1]-mn[1]:.3f},{mx[2]-mn[2]:.3f}) minY={mn[1]:.3f} centre=({(mx[0]+mn[0])/2:.3f},{(mx[2]+mn[2])/2:.3f})")

def textures(g, bins, outdir, stem):
    for mi, m in enumerate(g.get('materials', [])):
        pbr = m.get('pbrMetallicRoughness', {})
        sg = m.get('extensions', {}).get('KHR_materials_pbrSpecularGlossiness', {})
        slots = {'basecolor': pbr.get('baseColorTexture') or sg.get('diffuseTexture'), 'normal': m.get('normalTexture'),
                 'emissive': m.get('emissiveTexture')}
        for role, ref in slots.items():
            if not ref: continue
            img = g['images'][g['textures'][ref['index']]['source']]
            bv = g['bufferViews'][img['bufferView']]
            raw = bins[bv['buffer']][bv.get('byteOffset', 0): bv.get('byteOffset', 0) + bv['byteLength']]
            ext = 'jpg' if img.get('mimeType') == 'image/jpeg' else 'png'
            mname = re.sub(r'\W', '_', m.get('name', f'mat{mi}'))
            fn = f"{stem}_{mname}_{role}.{ext}"
            open(os.path.join(outdir, fn), 'wb').write(raw)
            print(f"  tex {fn} ({len(raw)//1024} KB) alpha={m.get('alphaMode','OPAQUE')} emissiveFactor={m.get('emissiveFactor')}")

if __name__ == '__main__':
    src, outdir, stem, mode = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
    skip = re.compile(sys.argv[5]) if len(sys.argv) > 5 and sys.argv[5] else None
    os.makedirs(outdir, exist_ok=True)
    g, bins = load_glb(src)
    ps = [p for p in parts(g, bins) if not (skip and skip.search(p[0]))]
    print(f"{src}: parts={[p[0] for p in ps]}")
    if mode == 'merge': write_obj(os.path.join(outdir, f"{stem}.obj"), ps, g)
    else:
        for i, p in enumerate(ps): write_obj(os.path.join(outdir, f"{stem}_{i}.obj"), [p], g)
    textures(g, bins, outdir, stem)
