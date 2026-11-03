# Texturas dos minigames no padrão PSX

Scripts que pintam as texturas principais dos minigames no estilo do atlas do PSX Carnival Kit
(guideline: "Guideline visual dos minigames"). Precisam de Python 3 com Pillow.

| Script | Gera | Uso |
| --- | --- | --- |
| `gauge.py` | `T_GeneratorGauge.png` | `python3 gauge.py Assets/Materials/Minigames/Generator/T_GeneratorGauge.png` |
| `fusebox.py` | `T_FuseBoxCover.png`, `T_FuseBoxPanel.png`, `T_FuseScrew.png` | `python3 fusebox.py Assets/Materials/Minigames/FuseBox Assets/PSXCarnivalKit/Textures/T_atlas.png` |

`pxfont.py` é a fonte pixel 3×5 usada para escrever nas texturas.

Os scripts usam semente fixa: rodar de novo gera o mesmo PNG. Depois de gerar, rode a fase
correspondente em `Tools > PSX` no Unity, que reimporta a textura com filtro point e sem mipmap.
