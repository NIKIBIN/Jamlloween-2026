# Texturas do RadioMinigame no estilo do atlas do PSX Carnival Kit.
# Coordenadas tiradas do prefab: corpo 0.09 x 0.26 m (x, z), dial em (0, 0.012), escala 88-108 MHz em 270 graus
# a partir de baixo-esquerda no sentido horario (RadioMinigame.ApplyDial), raio 0.0297 m.
import math, random, sys, os
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from pxfont import text, width
random.seed(23)
OUT = sys.argv[1]
INK = (34, 30, 28); CREAM = (214, 205, 182); RUST = [(122, 66, 38), (98, 52, 30)]

def noise_fill(img, base, jitter=(0.94, 0.97, 1, 1, 1.03)):
    p = img.load()
    for y in range(img.height):
        for x in range(img.width):
            k = random.choice(jitter); p[x, y] = tuple(int(min(255, c * k)) for c in base)
    return p

def stains(p, w, h, n, col, r=(1, 3)):
    for _ in range(n):
        cx, cy, rr = random.randrange(w), random.randrange(h), random.uniform(*r)
        for y in range(int(cy - rr), int(cy + rr) + 1):
            for x in range(int(cx - rr), int(cx + rr) + 1):
                if 0 <= x < w and 0 <= y < h and (x - cx) ** 2 + (y - cy) ** 2 <= rr * rr and random.random() < 0.7:
                    p[x, y] = tuple(int(a * 0.8 + b * 0.2) for a, b in zip(p[x, y], col))

def edges(p, w, h, band, k):
    for y in range(h):
        for x in range(w):
            d = min(x, y, w - 1 - x, h - 1 - y)
            if d < band: p[x, y] = tuple(int(c * (1 - k * (1 - d / band))) for c in p[x, y])

# ---- frente do radio, 64 x 184 (0.09 x 0.26 m), face de cima do corpo
W, H = 64, 184
def U(x): return (x + 0.045) / 0.09 * W
def V(z): return (0.13 - z) / 0.26 * H
front = Image.new('RGB', (W, H)); p = noise_fill(front, (160, 165, 122))     # caqui sujo (RadioBody #a8ad84)
stains(p, W, H, 22, (90, 80, 50), (1, 4))
edges(p, W, H, 4, 0.3)
# grade do alto-falante: fendas de 4 mm, 5 cm de largura
for z in (0.075, 0.087, 0.099, 0.111, 0.123, 0.135):
    y0 = int(V(z + 0.002)); y1 = int(V(z - 0.002))
    for y in range(max(0, y0), min(H, y1 + 1)):
        for x in range(int(U(-0.025)), int(U(0.025)) + 1): p[x, y] = (26, 24, 22)
# escala do dial
cx, cy = U(0.0), V(0.012)
pxm = W / 0.09
for f in range(88, 109):
    a = math.radians((f - 88) / 20 * 270 - 135)
    major = f % 5 == 0
    r0, r1 = (0.0262 if major else 0.0275) * pxm, 0.032 * pxm
    for k in range(8):
        r = r0 + (r1 - r0) * k / 7
        x, y = int(round(cx + math.sin(a) * r)), int(round(cy - math.cos(a) * r))
        if 0 <= x < W and 0 <= y < H: p[x, y] = INK
for label, (x, z) in {"90": (-0.037, 0.008), "95": (-0.025, 0.042), "100": (0.018, 0.047), "105": (0.039, -0.008)}.items():
    w_ = width(label); x0 = int(round(U(x) - w_ / 2)); x0 = max(1, min(W - w_ - 1, x0))
    text(p, label, x0, int(round(V(z) - 2)), INK, 1, (W, H))
text(p, "MHZ", int(cx - width("MHZ") / 2), int(cy + 0.03 * pxm), INK, 1, (W, H))
# marca na base do radio
text(p, "FM", 3, H - 9, (60, 58, 40), 1, (W, H))
front.save(f"{OUT}/T_RadioFront.png")

# ---- bilhete, 64 x 52 (0.072 x 0.058 m): papel, fitas nos cantos de cima, titulo FREQ.
W, H = 64, 52
note = Image.new('RGB', (W, H)); p = noise_fill(note, (212, 204, 178))
stains(p, W, H, 8, (150, 130, 90), (1, 3))
edges(p, W, H, 3, 0.12)
def tape(cx, cy, ang):
    a = math.radians(ang)
    for j in range(-4, 5):
        for i in range(-9, 10):
            x = int(round(cx + i * math.cos(a) - j * math.sin(a))); y = int(round(cy + i * math.sin(a) + j * math.cos(a)))
            if 0 <= x < W and 0 <= y < H: p[x, y] = tuple(int(c * 0.55 + t * 0.45) for c, t in zip(p[x, y], (200, 180, 110)))
tape(int((0.036 - 0.028) / 0.072 * W), 2, -12)
tape(int((0.036 + 0.028) / 0.072 * W), 2, 12)
text(p, "FREQ", (W - width("FREQ")) // 2, 10, (70, 62, 70), 1, (W, H))
note.save(f"{OUT}/T_RadioNote.png")

# ---- botao do dial, 32 x 32: tampa de cima do prisma; +z (topo da imagem) e para onde o ponteiro aponta
W = H = 32
knob = Image.new('RGB', (W, H)); p = noise_fill(knob, (58, 58, 58))
c = (W - 1) / 2
for y in range(H):
    for x in range(W):
        d = math.hypot(x - c, y - c)
        if d > 12.5 and (int(math.degrees(math.atan2(y - c, x - c))) // 20) % 2 == 0: p[x, y] = (38, 38, 38)   # serrilhado
for y in range(2, 15):
    for x in (15, 16): p[x, y] = CREAM
knob.save(f"{OUT}/T_RadioKnob.png")

# ---- tampa da pilha, 32 x 64 (0.06 x 0.116 m), vista com o radio virado (giro de 180 graus em z):
# pintada como aparece na tela e girada 180 graus no fim
W, H = 32, 64
cov = Image.new('RGB', (W, H)); p = noise_fill(cov, (101, 104, 96))
stains(p, W, H, 6, (60, 60, 55), (1, 2))
edges(p, W, H, 2, 0.25)
for z in (-0.030, -0.022, -0.014):           # garras, perto do lado -z da tampa
    y = int(round((0.058 - z) / 0.116 * H))
    for x in range(5, 27):
        p[x, y] = (40, 40, 38); p[x, y + 1] = (130, 132, 124)
text(p, "OPEN", (W - width("OPEN")) // 2, 8, (60, 62, 58), 1, (W, H))
cov.rotate(180).save(f"{OUT}/T_RadioCover.png")
print("ok")
