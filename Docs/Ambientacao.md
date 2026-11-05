# Ambientação e lanterna

Aplicado na cena `Zatan_Test`.

## Luz do mundo (`WorldLighting`)
Um só componente controla a névoa, o brilho do cenário, a luz ambiente e o fundo da câmera: `WorldLighting`, em `Assets/Lighting/`. Nenhum outro sistema escreve esses valores.

Cada visual é um asset `LightingProfile`, em `Assets/Lighting/Profiles/`:

| Profile | Névoa exp (visível até) | Brilho | Uso |
|---|---|---|---|
| `LP_Noite` | 0,16 (~19 m) | 0,42 | Início da cena: escuro, mas dá para ver o caminho com dificuldade |
| `LP_Apagao` | 0,6 (~5 m) | 0,3 | Fim do minigame do Zatan: a energia cai |
| `LP_EnergiaRestaurada` | 0,13 (~23 m) | 0,5 | Fim do minigame do gerador: um pouco mais claro que o início |

A névoa fora do feixe é **exponencial**: ela começa a escurecer logo perto do jogador e some devagar. Assim, não há um círculo iluminado em volta do jogador com uma borda escura. Dentro do feixe da lanterna, a névoa é exp2, para o feixe mostrar bem o que está perto.

A cor da névoa (`fogColor`, azul-acinzentado escuro) é um pouco mais clara que o céu (`skyColor`, o fundo da câmera do jogador). Assim, o que está longe aparece como silhueta contra o céu. O `WorldLighting` pinta só a câmera do jogador (campo `cameras`), porque a cena tem duas câmeras com a tag `MainCamera`.

**Cor global:** o Volume `_Sistema/PostProcess_Global` usa o `Assets/Lighting/PP_Global.asset`. Ele baixa a saturação em 10 e baixa mais os vermelhos (para ~70%), para as tendas terem um tom normal de noite.

O Inspector do profile atualiza a cena na hora, em Edit Mode e em Play Mode. O Lighting Settings da cena segue o profile inicial: não edite a névoa ali.

**Usar em outra cena:**
1. Arraste `Assets/Lighting/WorldLighting.prefab` para a cena.
2. Escolha o `initialProfile`.
3. Para trocar o visual por código, chame `WorldLighting.Set(profile, segundos)`.

**Ligado à progressão:** o `LightGroup` tem `profileOn` e `profileOff`. No `LightGroup_Park`, desligar (`Step_02_Zatan`) aplica `LP_Apagao` em 0,3 s, e ligar (`Step_05_Gerador`) aplica `LP_EnergiaRestaurada` em 2 s. O `startOn` não troca o profile: no início, manda o `initialProfile`.

## Névoa
Névoa radial de verdade, calculada pela distância real até a câmera em `Assets/Flashlight/Shaders/FlashlightFog.hlsl`. A densidade, a cor e o brilho fora do feixe vêm do profile atual do `WorldLighting`.

`PlayerFlashlight` liga a névoa radial, só enquanto está ativo, e controla só o feixe. Sem ele, `PSX Unlit` usa a névoa nativa, que o `WorldLighting` deixa igual à radial.

## Noite
- Directional Light: intensidade `0,08`.
- Far clip da câmera do jogador: `250` (o mapa inteiro). A névoa esconde o que está longe, e as lâmpadas dos beacons aparecem de longe sem "poppar".

O chão, os objetos que usavam o Lit padrão e o corpo de Zatan usam `Jamlloween/Flashlight Fog Surface`, com a mesma névoa e sem luz direcional, recebendo só a lanterna e os olhos de Zatan. O brilho do chão é ajustado em `M_FogSurface_Default`.

## Lanterna
Tecla **F**. `PlayerFlashlight` fica em `Player/Main Camera/Flashlight`, e os parâmetros em `Assets/Flashlight/FlashlightConfig.asset`:

| Parâmetro | Valor | Efeito |
|---|---|---|
| `coneFogDensity` | 0,07 | Névoa dentro do feixe (~25 m) |
| `coneBrightness` | 0,8 | Brilho do cenário dentro do feixe |
| `transitionSeconds` | 0,2 | Duração da transição |
| `intensity` | 2,5 | Intensidade da luz |
| `spotAngle` / `innerSpotAngle` | 38 / 18 | Abertura do feixe e da borda suave |

A névoa só recua dentro do feixe, e fora dele tudo continua como com a lanterna desligada. O alcance da luz é o mesmo do feixe. O feixe nunca deixa o cenário mais escuro que o profile: se o profile for mais claro que a lanterna, o shader usa o valor do profile.

Outros sistemas podem usar `IsOn` e o evento `Toggled`. A lanterna não altera a percepção nem a tensão de Zatan.

## Luzes do carrossel e da roda-gigante (`StepBeacon`)
As duas atrações acendem desde o início do jogo e usam a energia do `LightGroup` `park` (`powerGroup`). No apagão, elas apagam, e as lâmpadas ficam como vidro apagado.

| Atração | Quebrada até (`brokenUntil`) | Depois do conserto |
|---|---|---|
| Carrossel | `R16_FalaRodaGigante` (fusível no R15) | Todas as lâmpadas acesas e firmes |
| Roda-gigante | `R18_Encurralado` (motor no R17) | O padrão próprio do beacon (`Flicker`) |

**Quebrada:** a luz fica apagada quase sempre (acesa ~5–10% do tempo). Ela acende em surtos curtos e irregulares, com engasgos, e não chega ao brilho máximo:
- `brokenLevel` (0,3): a força da luz no chão no pico de um surto.
- `brokenBulbLevel` (0,4): o brilho das lâmpadas no pico de um surto.
- `brokenGap` (1 a 2,5 s): o intervalo entre os surtos.
- Cerca de 40% dos segmentos de lâmpadas ficam sempre mortos.

Os beacons com o mesmo `flickerSeed` piscam juntos: os três do carrossel usam 1, e a roda-gigante usa 2. Assim, as lâmpadas e a luz no chão piscam ao mesmo tempo.
