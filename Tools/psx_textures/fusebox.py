# Texturas do FuseBoxMinigame: tampa, painel interno e parafuso. Estilo do atlas do PSX Carnival Kit.
import random, sys, os
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from pxfont import text, width
random.seed(11)
OUT = sys.argv[1]
atlas = Image.open(sys.argv[2]).convert('RGB')
GREEN_SRC = atlas.crop((1920, 448, 1984, 512))          # pintura verde do power_panel (recorte PinturaVerde)
RUST = [(122, 66, 38), (98, 52, 30), (140, 84, 44)]
INK = (30, 26, 26); CREAM = (214, 205, 182); YELLOW = (208, 171, 49)

def tile(src, w, h):
    # espelha cada ladrilho ao acaso: quebra a repeticao das manchas do recorte
    im = Image.new('RGB', (w, h))
    for y in range(0, h, src.height):
        for x in range(0, w, src.width):
            t = src
            if random.random() < 0.5: t = t.transpose(Image.FLIP_LEFT_RIGHT)
            if random.random() < 0.5: t = t.transpose(Image.FLIP_TOP_BOTTOM)
            im.paste(t, (x, y))
    return im

def grime(px, w, h, spots, rust=True):
    for _ in range(spots):
        cx, cy, r = random.randrange(w), random.randrange(h), random.uniform(1, 3.5)
        col = random.choice(RUST) if rust else (25, 25, 25)
        for y in range(int(cy - r), int(cy + r) + 1):
            for x in range(int(cx - r), int(cx + r) + 1):
                if 0 <= x < w and 0 <= y < h and (x - cx) ** 2 + (y - cy) ** 2 <= r * r and random.random() < 0.8: px[x, y] = col

def streak(px, x, y0, length, h):
    for k in range(length):
        y = y0 + k
        if 0 <= y < h and random.random() < 0.85 - k * 0.04: px[x, y] = random.choice(RUST)

def darken_edges(px, w, h, band, k):
    for y in range(h):
        for x in range(w):
            d = min(x, y, w - 1 - x, h - 1 - y)
            if d < band:
                f = 1 - k * (1 - d / band)
                px[x, y] = tuple(int(c * f) for c in px[x, y])

# ---- tampa 128x96 (0.8 x 0.6 m)
W, H = 128, 96
cov = tile(GREEN_SRC, W, H); p = cov.load()
darken_edges(p, W, H, 6, 0.35)
grime(p, W, H, 18)
for _ in range(4): streak(p, random.randrange(10, W - 10), random.randrange(10, 50), random.randrange(6, 16), H)
# faixa DANGER amarela com texto preto
for y in range(14, 25):
    for x in range(28, 100): p[x, y] = tuple(int(c * random.choice([1, 1, 0.94, 1.04])) for c in YELLOW)
text(p, "DANGER", (W - width("DANGER", 2)) // 2, 15, INK, 2, (W, H))
# FUSES em estencil creme
text(p, "FUSES", (W - width("FUSES", 3)) // 2, 40, CREAM, 3, (W, H))
# raspados na pintura do texto
for _ in range(25):
    x, y = random.randrange(20, 108), random.randrange(40, 56)
    if p[x, y] == CREAM: p[x, y] = tuple(int(c * 0.75) for c in CREAM)
text(p, "240V", (W - width("240V", 1)) // 2, 70, CREAM, 1, (W, H))
cov.save(f"{OUT}/T_FuseBoxCover.png")

# ---- painel interno 128x100 (0.9 x 0.7 m); borda verde de 7 px aparece em volta da tampa
W, H = 128, 100
pan = tile(GREEN_SRC, W, H); p = pan.load()
def u(x): return int((x + 0.45) / 0.9 * W)
def v(y): return int((0.35 - y) / 0.7 * H)
for y in range(7, H - 7):
    for x in range(7, W - 7):
        g = random.choice([58, 60, 62, 55, 64])
        p[x, y] = (g, g + 2, g + 2)
grime(p, W, H, 14)
darken_edges(p, W, H, 4, 0.3)
# faixa MAIN FUSES no alto
for y in range(11, 19):
    for x in range(20, W - 20): p[x, y] = tuple(int(c * random.choice([1, 1, 0.94])) for c in YELLOW)
text(p, "MAIN FUSES", (W - width("MAIN FUSES")) // 2, 12, INK, 1, (W, H))
# numeros 1..5 acima de cada slot (slots em x = -0.28..0.28, topo do slot em y = 0.12)
for i, sx in enumerate([-0.28, -0.14, 0.0, 0.14, 0.28]):
    cx, ty = u(sx), v(0.155)
    for y in range(ty - 1, ty + 6):
        for x in range(cx - 3, cx + 4): p[x, y] = CREAM
    text(p, str(i + 1), cx - 1, ty, INK, 1, (W, H))
# furos dos parafusos e ferrugem escorrendo
for sx in (-0.36, 0.36):
    for sy in (-0.26, 0.26):
        cx, cy = u(sx), v(sy)
        for y in range(cy - 2, cy + 3):
            for x in range(cx - 2, cx + 3): p[x, y] = (24, 22, 22)
        streak(p, cx, cy + 3, random.randrange(4, 9), H)
text(p, "RIDES", (W - width("RIDES")) // 2, v(-0.17), CREAM, 1, (W, H))
pan.save(f"{OUT}/T_FuseBoxPanel.png")

# ---- parafuso 16x16: aco claro, fenda horizontal
scr = Image.new('RGB', (16, 16)); p = scr.load()
for y in range(16):
    for x in range(16):
        g = random.choice([176, 180, 184, 170, 188]); p[x, y] = (g, g + 2, g + 5)
for x in range(2, 14):
    p[x, 7] = (30, 28, 28); p[x, 8] = (52, 50, 50)
for _ in range(5): p[random.randrange(16), random.randrange(16)] = random.choice(RUST)
scr.save(f"{OUT}/T_FuseScrew.png")
print("ok")
