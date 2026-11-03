# Mostrador do medidor de combustivel do gerador, 64x64, estilo do atlas do PSX Carnival Kit.
import math, random, sys
from PIL import Image
random.seed(7)
N = 64
C = (N - 1) / 2
img = Image.new('RGB', (N, N), (40, 36, 32))
px = img.load()
CREAM = (214, 205, 182)      # abaixo do creme maximo #ddd5c2
INK = (38, 32, 34)
RED = (172, 39, 31)          # vermelho do kit #ac271f
GREEN = (74, 118, 77)        # verde do kit #4a764d
R_FACE = 29.5

def put(x, y, c):
    if 0 <= x < N and 0 <= y < N: px[x, y] = c

# face creme com grao, manchas e borda escurecida
stains = [(random.uniform(8, 56), random.uniform(8, 56), random.uniform(3, 9)) for _ in range(5)]
for y in range(N):
    for x in range(N):
        d = math.hypot(x - C, y - C)
        if d > R_FACE: continue
        k = 1.0 - 0.18 * max(0.0, (d - 18) / (R_FACE - 18))          # escurece perto da borda
        k *= random.choice([1, 1, 1, 0.96, 0.93, 1.03])                # grao
        for sx, sy, sr in stains:
            if math.hypot(x - sx, y - sy) < sr: k *= 0.9               # manchas de sujeira
        px[x, y] = tuple(int(min(255, v * k)) for v in CREAM)

def ray(angle_deg, r0, r1, color, width=1):
    # angulo do ponteiro: 0 = para cima, positivo = anti-horario (vazio = +70, cheio = -70)
    a = math.radians(angle_deg)
    dx, dy = -math.sin(a), -math.cos(a)
    steps = int((r1 - r0) * 3) + 1
    for i in range(steps):
        r = r0 + (r1 - r0) * i / max(1, steps - 1)
        x, y = C + dx * r, C + dy * r
        for w in range(width):
            put(int(round(x + dy * w * 0.0 + (w * -dy if width > 1 else 0))), int(round(y + (w * dx if width > 1 else 0))), color)

# faixas de cor nas pontas da escala
for ang in range(56, 71, 1): ray(ang, 23, 27.5, RED)
for ang in range(-70, -55, 1): ray(ang, 23, 27.5, GREEN)
# tracos da escala: 9, de +70 a -70
for i in range(9):
    ang = 70 - i * 17.5
    big = i in (0, 4, 8)
    ray(ang, 20 if big else 22, 27, INK)

# letras 3x5: E (vazio, esquerda) e F (cheio, direita)
GLYPH = {
    'E': ["###", "#..", "##.", "#..", "###"],
    'F': ["###", "#..", "##.", "#..", "#.."],
}
def text(ch, x0, y0, color):
    for j, row in enumerate(GLYPH[ch]):
        for i, c in enumerate(row):
            if c == '#': put(x0 + i, y0 + j, color)
text('E', 12, 27, INK)
text('F', 49, 27, INK)
# "FUEL" pequeno embaixo do eixo, 3x5 com 1 px de espaco
G2 = {'U': ["#.#", "#.#", "#.#", "#.#", "###"], 'L': ["#..", "#..", "#..", "#..", "###"]}
GLYPH.update(G2)
for k, ch in enumerate("FUEL"): text(ch, 25 + k * 4 - 1, 42, INK)

img = img.rotate(180)   # a tampa do prisma mostra a imagem girada 180 graus (ver Assets/Editor/PSX/PSXPhaseGenerator.cs)
img.save(sys.argv[1])
print('ok', sys.argv[1])
