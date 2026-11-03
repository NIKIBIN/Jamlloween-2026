# Texturas do ZatanRepairPanel no estilo do atlas do PSX Carnival Kit.
# Coordenadas tiradas do prefab. Mostrador: face de 0.18 m, tracos de 0 a 180 em semicirculo (esquerda, topo,
# direita) com raio ~0.077 m, numeros 30-150 em raio 0.056, "0" impresso em (-0.056, 0) riscado em vermelho e "0"
# escrito a mao em (0.058, 0.002) girado -14 graus. Manometro: face de 0.094 m, 9 tracos de +125 a -125 graus
# (a partir de +x) em raio 0.037, os dois ultimos vermelhos.
# Os cilindros mostram a tampa de baixo, que inverte a imagem na vertical: mostrador e manometro saem invertidos.
import math, random, sys, os
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from pxfont import text, width
random.seed(31)
OUT, ATLAS = sys.argv[1], sys.argv[2]
atlas = Image.open(ATLAS).convert('RGB')
GREEN = atlas.crop((1920, 448, 1984, 512))
INK = (34, 30, 30); CREAM = (214, 205, 182); RED = (172, 39, 31); YELLOW = (208, 171, 49)
RUST = [(122, 66, 38), (98, 52, 30), (140, 84, 44)]

def noise(img, base, j=(0.94, 0.97, 1, 1, 1.03)):
    p = img.load()
    for y in range(img.height):
        for x in range(img.width): p[x, y] = tuple(int(min(255, c * random.choice(j))) for c in base)
    return p
def tile(src, w, h, tint=(1, 1, 1)):
    im = Image.new('RGB', (w, h))
    for y in range(0, h, src.height):
        for x in range(0, w, src.width):
            t = src
            if random.random() < .5: t = t.transpose(Image.FLIP_LEFT_RIGHT)
            if random.random() < .5: t = t.transpose(Image.FLIP_TOP_BOTTOM)
            im.paste(t, (x, y))
    p = im.load()
    for y in range(h):
        for x in range(w): p[x, y] = tuple(int(c * k) for c, k in zip(p[x, y], tint))
    return im, p
def blots(p, w, h, n, cols, r=(1, 3), a=0.8):
    for _ in range(n):
        cx, cy, rr = random.randrange(w), random.randrange(h), random.uniform(*r); col = random.choice(cols)
        for y in range(int(cy - rr), int(cy + rr) + 1):
            for x in range(int(cx - rr), int(cx + rr) + 1):
                if 0 <= x < w and 0 <= y < h and (x - cx) ** 2 + (y - cy) ** 2 <= rr * rr and random.random() < a: p[x, y] = col
def edges(p, w, h, band, k):
    for y in range(h):
        for x in range(w):
            d = min(x, y, w - 1 - x, h - 1 - y)
            if d < band: p[x, y] = tuple(int(c * (1 - k * (1 - d / band))) for c in p[x, y])
def line(p, w, h, x0, y0, x1, y1, col, thick=1):
    n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    for i in range(n + 1):
        x, y = x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n
        for a in range(thick):
            for b in range(thick):
                X, Y = int(round(x)) + a, int(round(y)) + b
                if 0 <= X < w and 0 <= Y < h: p[X, Y] = col

# ---- mostrador 128x128 (0.18 m)
N = 128; S = N / 0.18; C = (N - 1) / 2
dial = Image.new('RGB', (N, N), (40, 34, 30)); p = dial.load()
for y in range(N):
    for x in range(N):
        d = math.hypot(x - C, y - C)
        if d <= N / 2:
            k = (1 - 0.15 * max(0, (d - 40) / 24)) * random.choice([1, 1, .97, .94, 1.03])
            p[x, y] = tuple(int(min(255, c * k)) for c in CREAM)
blots(p, N, N, 10, [(176, 160, 120), (150, 136, 100)], (2, 6), 0.35)
def P(xm, ym): return C + xm * S, C - ym * S          # metros (x direita, y cima) -> pixel
for a in range(0, 181, 10):
    t = math.radians(180 - a); major = a % 30 == 0
    r0 = 0.066 if major else 0.071
    x0, y0 = P(math.cos(t) * r0, math.sin(t) * r0); x1, y1 = P(math.cos(t) * 0.084, math.sin(t) * 0.084)
    line(p, N, N, x0, y0, x1, y1, INK, 2 if major else 1)
for v in (30, 60, 90, 120, 150):
    t = math.radians(180 - v); x, y = P(math.cos(t) * 0.052, math.sin(t) * 0.052)
    text(p, str(v), int(round(x - width(str(v)) / 2)), int(round(y - 2)), INK, 1, (N, N))
x, y = P(-0.056, 0); text(p, "0", int(x - 1), int(y - 2), INK, 2, (N, N))          # zero impresso
line(p, N, N, x - 5, y - 6, x + 7, y + 8, RED, 2); line(p, N, N, x - 5, y + 8, x + 7, y - 6, RED, 2)   # riscado
x, y = P(0.058, 0.002)                                                              # zero a mao, torto
for k in range(28):
    a = k / 28 * 2 * math.pi
    X = x + 3 * math.cos(a) * math.cos(math.radians(-14)) - 4.5 * math.sin(a) * math.sin(math.radians(-14))
    Y = y + 3 * math.cos(a) * math.sin(math.radians(-14)) + 4.5 * math.sin(a) * math.cos(math.radians(-14))
    if 0 <= int(X) < N and 0 <= int(Y) < N: p[int(X), int(Y)] = (60, 48, 70)
text(p, "PSI", int(C - width("PSI") / 2), int(C + 0.03 * S), INK, 1, (N, N))
dial.transpose(Image.FLIP_TOP_BOTTOM).save(f"{OUT}/T_ZatanDial.png")

# ---- manometro 64x64 (0.094 m)
N = 64; S = N / 0.094; C = (N - 1) / 2
gauge = Image.new('RGB', (N, N), (30, 28, 28)); p = gauge.load()
for y in range(N):
    for x in range(N):
        if math.hypot(x - C, y - C) <= N / 2: p[x, y] = tuple(int(min(255, c * random.choice([1, 1, .95, 1.03]))) for c in CREAM)
blots(p, N, N, 5, [(170, 156, 120)], (1, 4), 0.35)
for k in range(9):
    t = math.radians(125 - k * 31.25)
    x0, y0 = C + math.cos(t) * 0.031 * S, C - math.sin(t) * 0.031 * S
    x1, y1 = C + math.cos(t) * 0.043 * S, C - math.sin(t) * 0.043 * S
    line(p, N, N, x0, y0, x1, y1, RED if k >= 7 else INK, 2)
text(p, "BAR", int(C - width("BAR") / 2), int(C + 9), INK, 1, (N, N))
gauge.transpose(Image.FLIP_TOP_BOTTOM).save(f"{OUT}/T_ZatanGauge.png")

# ---- placa do interruptor 32x56 (0.085 x 0.15 m), face da frente: sem inversao
W, H = 32, 56
sw = Image.new('RGB', (W, H)); p = noise(sw, (52, 54, 58))
blots(p, W, H, 4, RUST, (1, 2))
edges(p, W, H, 3, 0.35)
text(p, "ON", (W - width("ON", 2)) // 2, 4, CREAM, 2, (W, H))
text(p, "OFF", (W - width("OFF")) // 2, H - 10, CREAM, 1, (W, H))
for y in range(20, 36):
    for x in (14, 15, 16, 17): p[x, y] = (18, 18, 20)           # fenda da alavanca
sw.save(f"{OUT}/T_ZatanSwitch.png")

# ---- painel de fundo 128x90 (0.8 x 0.56 m): pintura clara gasta, rebites, faixa de aviso
W, H = 128, 90
bp, p = tile(GREEN, W, H, (1.45, 1.4, 1.3))
blots(p, W, H, 26, RUST, (1, 3))
edges(p, W, H, 5, 0.35)
for x in range(4, W - 3, 15):
    for y in (3, H - 4):
        p[x, y] = (60, 62, 58); p[x + 1, y] = (150, 152, 140)
for y in range(H - 14, H - 8):
    for x in range(30, W - 30): p[x, y] = YELLOW if ((x + y) // 4) % 2 else INK      # faixa zebrada
bp.save(f"{OUT}/T_ZatanBackplate.png")

# ---- tampa 128x92 (0.84 x 0.6 m): verde do kit, ZATAN em estencil, placa MERRIMAN BROS.
W, H = 128, 92
cv, p = tile(GREEN, W, H, (0.85, 0.9, 0.85))
blots(p, W, H, 22, RUST, (1, 3.5))
edges(p, W, H, 6, 0.35)
text(p, "ZATAN", (W - width("ZATAN", 3)) // 2, 12, CREAM, 3, (W, H))
for _ in range(30):
    x, y = random.randrange(20, 108), random.randrange(12, 27)
    if p[x, y] == CREAM: p[x, y] = tuple(int(c * 0.7) for c in CREAM)
text(p, "MECHANISM", (W - width("MECHANISM")) // 2, 31, CREAM, 1, (W, H))
for y in range(H - 22, H - 12):
    for x in range(34, W - 34): p[x, y] = tuple(int(c * random.choice([1, .95])) for c in (150, 120, 60))
text(p, "MERRIMAN", (W - width("MERRIMAN")) // 2, H - 20, INK, 1, (W, H))

cv.save(f"{OUT}/T_ZatanCover.png")

# ---- piche e fumaca: mesma forma, resolucao do kit e sem suavizacao
d = os.path.join(OUT)
for name, n in (("TarBlob", 64), ("SmokePuff", 32)):
    src = Image.open(f"{d}/{name}.png")
    if src.width != n: src.resize((n, n), Image.NEAREST).save(f"{d}/{name}.png")
print("ok")
