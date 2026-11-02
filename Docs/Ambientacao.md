# Ambientação e lanterna

Aplicado na cena `Zatan_Test`.

## Névoa
Névoa radial de verdade, calculada pela distância real até a câmera em `Assets/Flashlight/Shaders/FlashlightFog.hlsl`. Com 5% de visibilidade como limite, o jogador enxerga cerca de 5 m sem lanterna. A cor é `(0,01; 0,01; 0,014)`, igual ao fundo da câmera do jogador.

`PlayerFlashlight` liga essa névoa por valores globais de shader, só enquanto está ativo. Sem ele, `PSX Unlit` usa a névoa nativa como antes, então as outras cenas não mudam. O Lighting Settings da cena usa a mesma densidade (`0,346`), para o Scene View ficar parecido.

## Noite
- Luz ambiente: Flat `(0,03; 0,03; 0,045)`.
- Directional Light: intensidade `0,08`.
- Far clip da câmera do jogador: `40`.
- `baseBrightness` escurece as cores do `PSX Unlit` em volta do jogador.

O chão, os objetos que usavam o Lit padrão e o corpo de Zatan usam `Jamlloween/Flashlight Fog Surface`, com a mesma névoa e sem luz direcional, recebendo só a lanterna e os olhos de Zatan. O brilho do chão é ajustado em `M_FogSurface_Default`.

## Lanterna
Tecla **F**. `PlayerFlashlight` fica em `Player/Main Camera/Flashlight`, e os parâmetros em `Assets/Flashlight/FlashlightConfig.asset`:

| Parâmetro | Valor | Efeito |
|---|---|---|
| `baseFogDensity` | 0,346 | Névoa em volta do jogador (~5 m) |
| `coneFogDensity` | 0,102 | Névoa dentro do feixe (~17 m) |
| `baseBrightness` / `coneBrightness` | 0,45 / 0,8 | Brilho do cenário fora e dentro do feixe |
| `transitionSeconds` | 0,2 | Duração da transição |
| `intensity` | 2,5 | Intensidade da luz |
| `spotAngle` / `innerSpotAngle` | 38 / 18 | Abertura do feixe e da borda suave |

A névoa só recua dentro do feixe, e fora dele tudo continua como com a lanterna desligada. O alcance da luz é o mesmo do feixe. `baseFogDensity` deve bater com a densidade do Lighting Settings da cena.

Outros sistemas podem usar `IsOn` e o evento `Toggled`. A lanterna não altera a percepção nem a tensão de Zatan.
