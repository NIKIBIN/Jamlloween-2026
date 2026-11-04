# Ambientação e lanterna

Aplicado na cena `Zatan_Test`.

## Luz do mundo (`WorldLighting`)
Um só componente controla a névoa, o brilho do cenário, a luz ambiente e o fundo da câmera: `WorldLighting`, em `Assets/Lighting/`. Nenhum outro sistema escreve esses valores.

Cada visual é um asset `LightingProfile`, em `Assets/Lighting/Profiles/`:

| Profile | Névoa (visível até) | Brilho | Uso |
|---|---|---|---|
| `LP_Noite` | 0,10 (~17 m) | 0,9 | Início da cena |
| `LP_Apagao` | 0,42 (~4 m) | 0,35 | Fim do minigame do Zatan: a energia cai |
| `LP_EnergiaRestaurada` | 0,165 (~10,5 m) | 0,65 | Fim do minigame do gerador: ponto intermediário |

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
- Far clip da câmera do jogador: `40`.

O chão, os objetos que usavam o Lit padrão e o corpo de Zatan usam `Jamlloween/Flashlight Fog Surface`, com a mesma névoa e sem luz direcional, recebendo só a lanterna e os olhos de Zatan. O brilho do chão é ajustado em `M_FogSurface_Default`.

## Lanterna
Tecla **F**. `PlayerFlashlight` fica em `Player/Main Camera/Flashlight`, e os parâmetros em `Assets/Flashlight/FlashlightConfig.asset`:

| Parâmetro | Valor | Efeito |
|---|---|---|
| `coneFogDensity` | 0,102 | Névoa dentro do feixe (~17 m) |
| `coneBrightness` | 0,8 | Brilho do cenário dentro do feixe |
| `transitionSeconds` | 0,2 | Duração da transição |
| `intensity` | 2,5 | Intensidade da luz |
| `spotAngle` / `innerSpotAngle` | 38 / 18 | Abertura do feixe e da borda suave |

A névoa só recua dentro do feixe, e fora dele tudo continua como com a lanterna desligada. O alcance da luz é o mesmo do feixe. O feixe nunca deixa o cenário mais escuro que o profile: se o profile for mais claro que a lanterna, o shader usa o valor do profile.

Outros sistemas podem usar `IsOn` e o evento `Toggled`. A lanterna não altera a percepção nem a tensão de Zatan.
