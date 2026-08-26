import zlib, struct, math, random

W, H = 1024, 500
random.seed(21)
ink1=(15,19,29); ink2=(24,30,44); ember=(232,85,53); ember_hi=(255,176,138)
pale=(239,237,230)

img=[[None]*W for _ in range(H)]
def lerp(a,b,t): return a+(b-a)*t
def cl(c1,c2,t): t=max(0,min(1,t)); return tuple(int(lerp(c1[i],c2[i],t)) for i in range(3))

for y in range(H):
    for x in range(W):
        col = cl(ink1, ink2, y/H)
        g = max(0,1-math.hypot(x-260,y-250)/620)**2.2*0.5
        img[y][x] = cl(col, ember, g*0.45)
for _ in range(70):
    sx,sy=random.randrange(W),random.randrange(int(H*0.62))
    img[sy][sx]=cl(img[sy][sx],pale,random.uniform(0.2,0.7))

# crescent moon: body + offset shadow bite, one shared box so nothing clips square
mx,my,mr=890,95,44
for y in range(my-mr-18, my+mr+19):
    for x in range(mx-mr-18, mx+mr+34):
        if not(0<=x<W and 0<=y<H): continue
        d=math.hypot(x-mx,y-my)
        if mr<d<mr+16:
            img[y][x]=cl(img[y][x],pale,max(0,1-(d-mr)/16)*0.25)
        elif d<mr:
            img[y][x]=cl(pale,(215,212,200),0.4)
        db=math.hypot(x-(mx+16),y-(my-9))
        if db<mr-4 and d<mr+2:
            img[y][x]=cl(ink1,ink2,0.35)

def roofline(base,height,color,step=170,off=0):
    peaks=[]; x=-40+off
    while x<W+80:
        pw=random.randint(110,step); ph=random.randint(20,height)
        peaks.append((x,pw,ph)); x+=pw
    for y in range(H):
        for x in range(W):
            for (px_,pw,ph) in peaks:
                if px_<=x<px_+pw:
                    apex=px_+pw/2
                    if y>base-ph*(1-abs(x-apex)/(pw/2)): img[y][x]=color
                    break
roofline(H-52,70,cl(ink1,(10,13,20),0.5))
roofline(H-8,56,(8,11,17),step=150,off=60)

for lx in (140, 470, 720, 980):
    ly=H-70
    for y in range(ly-14,ly+14):
        for x in range(lx-14,lx+14):
            if 0<=x<W and 0<=y<H:
                d=math.hypot(x-lx,y-ly)
                if d<13: img[y][x]=cl(img[y][x],(255,150,90),max(0,1-d/13)*0.55)
    for y in range(ly-4,ly+4):
        for x in range(lx-4,lx+4):
            if 0<=x<W and 0<=y<H: img[y][x]=(255,140,80)

cx,cy,R=200,235,120
crack=[(cx-78,cy-84)]
for i in range(1,6):
    t=i/6
    crack.append((lerp(cx-78,cx+78,t)+random.uniform(-14,14), lerp(cy-84,cy+84,t)+random.uniform(-9,9)))
crack.append((cx+78,cy+84))
csegs=list(zip(crack[:-1],crack[1:]))
def dseg(px_,py_,a,b):
    ax,ay=a; bx,by=b; dx,dy=bx-ax,by-ay; L2=dx*dx+dy*dy or 1e-6
    t=max(0,min(1,((px_-ax)*dx+(py_-ay)*dy)/L2))
    return math.hypot(px_-(ax+t*dx),py_-(ay+t*dy))
for y in range(cy-R-20,cy+R+20):
    for x in range(cx-R-20,cx+R+20):
        if not(0<=x<W and 0<=y<H): continue
        man=abs(x-cx)+abs(y-cy)
        if man<R:
            ft=(y-(cy-R))/(2*R)
            col=cl(ember_hi,ember,ft*1.25)
            d=min(dseg(x,y,a,b) for a,b in csegs)
            if d<6: col=ink1
            elif d<9: col=cl(ink1,col,(d-6)/3)
            img[y][x]=col
        elif man<R+7: img[y][x]=pale
        elif man<R+12: img[y][x]=cl(pale,img[y][x],(man-R-7)/5)

F={
 'E':[[(1,0),(0,0),(0,1),(1,1)],[(0,.5),(.75,.5)]],
 'M':[[(0,1),(0,0),(.5,.6),(1,0),(1,1)]],
 'B':[[(0,0),(0,1)],[(0,0),(.7,0),(.92,.12),(.92,.36),(.7,.48),(0,.48)],[(.7,.48),(.95,.62),(.95,.86),(.7,1),(0,1)]],
 'R':[[(0,1),(0,0),(.7,0),(.95,.14),(.95,.36),(.7,.5),(0,.5)],[(.55,.5),(1,1)]],
 'L':[[(0,0),(0,1),(1,1)]],
 'I':[[(.5,0),(.5,1)],[(.15,0),(.85,0)],[(.15,1),(.85,1)]],
 'N':[[(0,1),(0,0),(1,1),(1,0)]],
 'A':[[(0,1),(.5,0),(1,1)],[(.22,.62),(.78,.62)]],
 'J':[[(.85,0),(.85,.75),(.65,1),(.3,1),(.1,.82)]],
 'C':[[(1,.12),(.62,0),(.16,.14),(0,.5),(.16,.86),(.62,1),(1,.88)]],
 'T':[[(0,0),(1,0)],[(.5,0),(.5,1)]],
 'O':[[(.5,0),(.06,.28),(.06,.72),(.5,1),(.94,.72),(.94,.28),(.5,0)]],
 ' ':[],
}
def draw_text(text,x0,y0,lh,lw,gap,color,weight,glow=None):
    x=x0
    for ch in text:
        segs=F.get(ch,[])
        pts=[[(x+px_*lw, y0+py_*lh) for px_,py_ in poly] for poly in segs]
        allsegs=[s for poly in pts for s in zip(poly[:-1],poly[1:])]
        if allsegs:
            xs=[p[0] for poly in pts for p in poly]; ys=[p[1] for poly in pts for p in poly]
            for yy in range(int(min(ys)-weight-10),int(max(ys)+weight+11)):
                for xx in range(int(min(xs)-weight-10),int(max(xs)+weight+11)):
                    if not(0<=xx<W and 0<=yy<H): continue
                    d=min(dseg(xx,yy,a,b) for a,b in allsegs)
                    if d<weight: img[yy][xx]=color
                    elif d<weight+1.5: img[yy][xx]=cl(color,img[yy][xx],(d-weight)/1.5)
                    elif glow and d<weight+9: img[yy][xx]=cl(img[yy][xx],glow,max(0,1-(d-weight)/9)*0.3)
        x+=lw+gap

draw_text("EMBERLINE", 372, 160, 96, 56, 14, pale, 6.5, glow=ember)
draw_text("ANIME NINJA ACTION", 380, 305, 30, 19, 8, cl(ember,ember_hi,0.25), 2.6)

for _ in range(24):
    sx=random.randrange(80,W-40); sy=random.randrange(60,H-110)
    if abs(sx-cx)+abs(sy-cy)<R+30: continue
    r=random.choice([2,2,3]); b=random.uniform(0.5,1)
    for yy in range(sy-r,sy+r+1):
        for xx in range(sx-r,sx+r+1):
            if 0<=xx<W and 0<=yy<H and math.hypot(xx-sx,yy-sy)<=r:
                img[yy][xx]=cl(img[yy][xx],ember_hi,b)

raw=b''.join(b'\x00'+b''.join(struct.pack('3B',*p) for p in row) for row in img)
def chunk(tag,data): return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data))
png=(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',W,H,8,2,0,0,0))
     +chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
open('feature-graphic-1024x500.png','wb').write(png)
print('done',len(png))
