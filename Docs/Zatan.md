# Zatan

Inimigo de perseguição com dois cérebros. Código em `Assets/Zatan`, testado na cena `Assets/Scenes/Zatan_Test.unity`.

## Como funciona

**Cérebro geral (`ZatanBehaviourBrain`)**

| Estado | O que faz | Sai quando |
|---|---|---|
| Patrol | Anda até pontos em um anel ao redor do jogador | Vê o jogador, ouve o jogador ou some |
| Investigate | Vai até o som e olha por 3 s | Vê o jogador ou termina de olhar |
| Chase | Corre até o jogador ou até a última posição vista | Captura ou fica sem ver pelo tempo de perda |
| Search | Procura perto da última posição vista por 8 s | Vê o jogador, ouve o jogador ou termina |
| Caught | Evento `PlayerCaught` | `Respawn` |
| Vanished | Invisível, sem colisão e sem sentidos | Reaparece em um ponto fora da visão do jogador |

**Tensão (`ZatanTension`, 0 a 100)**: é invisível para o jogador e só aparece no overlay da cena de teste.

- **Sobe com:** perseguição (3/s), jogador por perto (até 2/s dentro de 12 m), jogador olhando para Zatan (3/s), evento roteirizado (+20).
- **Desce com:** jogador em interior (3/s), 45 s sem perseguição (2/s), objetivo concluído (−15). As descidas contínuas não valem durante a perseguição.

| Parâmetro | Tensão 0 | Tensão 100 |
|---|---|---|
| Alcance de visão | 22 m | 10 m |
| Ângulo de visão | 110° | 80° |
| Audição | x1,4 | x0,6 |
| Raio da patrulha | 8–14 m | 20–45 m |
| Volume dos passos | 0,2 | 1,0 |
| Tempo para perder o jogador | 6 s | 2 s |

Tensão alta não encerra uma perseguição; ela só faz Zatan perder o jogador mais rápido.

**Velocidade**: é calculada a partir do jogador. Na cena de teste, o `LevelWalker` anda a 2,2 m/s e corre a 4 m/s.

- **Patrulha:** andar + 0,25 = 2,45 m/s.
- **Investigação:** média entre andar e correr = 3,1 m/s.
- **Perseguição:** corrida + 5% = 4,2 m/s, caindo aos poucos a partir de 5 m até igualar a corrida (4 m/s) a 2 m.
- **Captura:** a 1,2 m, ou 1,8 m se o caminho terminou porque o jogador está encostado em um obstáculo.

Correndo em linha reta, o jogador não é alcançado. Ele é pego quando para, anda ou erra o caminho.

**Sumiço**: acontece só na patrulha, a 30 m ou mais, fora da visão do jogador, depois de 4 s nessas condições. Dura de 4 a 15 s conforme a tensão, com 20 s de intervalo mínimo entre um sumiço e outro. `VanishSuspended` bloqueia o sumiço durante eventos roteirizados.

**Luz dos olhos (`ZatanEyeLights`)**: dois cones visíveis, um por olho, e uma luz única no chão e nas paredes, vinda do ponto entre os olhos. Essa luz é calculada nos shaders (`Assets/Zatan/Beam/ZatanEyeLight.hlsl`) porque o cenário em `PSX Unlit` ignora luzes reais.

| Estado | Cor | Direção |
|---|---|---|
| Patrol, Investigate, Search | Branca | 28° para o chão |
| Chase, a 15 m ou mais | Amarela | 6° para baixo, iluminando o caminho |
| Chase, de 15 m a 5 m | Amarela indo para vermelha | 6° para baixo |
| Chase, a 5 m ou menos | Vermelho máximo | 6° para baixo |
| Caught | Vermelho máximo | 6° para baixo |
| Vanished | Apagada | — |

O vermelho máximo usa `attenuationStartDistance`, o mesmo valor que inicia a desaceleração. As mudanças de estado levam 0,35 s, e ao reaparecer ele já volta branco. Os olhos usam a mesma cor e ignoram a névoa. Na `Level_Design`, os pontos `EyeBeam_L` e `EyeBeam_R` ficam nos olhos do modelo, dentro de `Model/.../Cabeca_ctrl`.

## Captura (jump scare)

`ZatanCatchSequence`, no objeto `Zatan`, roda quando ele pega o jogador, em cerca de 2,1 s:

1. **Agarrão (0,25 s):** o mundo fica preto em 0,05 s, o controle do jogador é desligado, a câmera vira para Zatan e ele vem para 0,75 m do rosto.
2. **Chacoalhão (1 s):** câmera e Zatan tremem em direções opostas, com a boca entreaberta.
3. **Mordida (0,5 s):** a boca abre e ele avança até a boca encher a tela, com som e tranco no impacto.
4. **Preto (0,35 s):** nem Zatan aparece. Depois tudo volta ao normal e o evento `Finished` dispara; na `Zatan_Test`, o harness reinicia nesse momento.

O escuro vem do global `_ZatanBlackout`, lido por `PSX Unlit` e `Flashlight Fog Surface`. Materiais com `Stays Visible In Catch Blackout` ligado (corpo e cabeça de Zatan) recebem a luz `scareLightColor` no lugar do mundo. Os feixes ficam apagados e os olhos, vermelhos.

Na `Level_Design`, a mandíbula é a peça `Boca` do modelo, e `MouthAnchor` e `Throat` ficam dentro de `Cabeca_ctrl`. Na `Zatan_Test` ainda é a cabeça provisória (`Head_Placeholder`). Ao trocar o modelo, religue a mandíbula, o centro da boca e o fundo da boca no Inspector.

Os ajustes ficam em "Captura" no `ZatanConfig`: tempos, distâncias, `scareHoldFaceRaise`, `scareLift`, `scareLightColor`, amplitudes do chacoalhão, ângulos da mandíbula, `scareBiteOpenFraction`, `scareBiteImpactFraction` e `scareImpactKick`.

## Ajustes

Todos os valores estão em `Assets/Zatan/ZatanConfig.asset`. Os da luz ficam em "Luz dos olhos": cores, `beamAlertDistance`, `beamTransitionTime`, `beamIdlePitch`, `beamChasePitch`, `beamLength`, `beamConeAngle`, `beamInnerConeAngle`, `beamSurfaceIntensity`, `beamSurfaceFogCut`, `beamSurfaceMinAlbedo`, `beamOpacity` e `beamFogResistance`. O brilho do cone com a câmera dentro dele é `_InsideDim`, em `Assets/Zatan/Beam/M_ZatanBeam.mat`.

O feixe para em obstáculos: cada olho mede a distância com raios e encurta o cone. Os ajustes ficam em "Luz dos olhos: obstáculos": `beamBlockers`, `beamFanRays`, `beamFanSpread`, `beamBlockFraction`, `beamFloorNormalY`, `beamProbeInterval`, `beamShrinkSpeed`, `beamGrowSpeed` e `beamMinLength`. O encontro suave do cone com o cenário é `_SoftIntersection`, no mesmo material.

A luz nas superfícies não tem sombra: raios horizontais procuram a parede à frente, mesmo com o feixe apontado para o chão, e a luz termina num plano nessa parede. Só as faces voltadas para os olhos recebem luz. Os ajustes ficam em "Luz dos olhos: paredes": `beamWallRays`, `beamWallSpread`, `beamWallProbeDrop`, `beamWallMinHits` e `beamWallClipMargin`.

## Testar

Abra `Zatan_Test` e dê Play. O overlay mostra tensão, estado, velocidade e distância.

| Tecla | Ação |
|---|---|
| F5 | Evento roteirizado |
| F6 | Objetivo concluído |
| F7 | Corrida automática para longe de Zatan |
| F8 / F9 | Tensão 0 / 100 |

Depois de uma captura, o jogador e Zatan voltam às posições iniciais quando o jump scare termina.

## Integrar em outra cena

1. Crie um adaptador herdando de `ZatanTarget` para o jogador da cena (`LevelWalkerZatanTarget` é o exemplo).
2. Faça o bake de um NavMesh e ligue as referências pelo Inspector.
3. Chame `ZatanTension.RaiseScriptedEvent()` e `CompleteObjective()` nos eventos do jogo.
4. Chame `ZatanPerception.ReportNoise(posição, raio)` para outros sons do jogo.
5. Assine `ZatanBehaviourBrain.PlayerCaught` para tratar a morte.

## Animação

`ZatanAnimation`, no objeto `Zatan`, passa a velocidade real do agente ao Animator de `Model`: parado ou encarando o jogador toca `Zatan_Idle`, na patrulha `Zatan_Walk` e na perseguição `Zatan_Run`, num blend tree em `Speed`. Os clipes pisam sem deslizar em `animWalkSpeed` (2,45 m/s) e `animRunSpeed` (4,2 m/s), no `ZatanConfig`; fora desses valores o parâmetro `Cadence` acelera ou freia o passo. Na captura o Animator desliga e o corpo volta à pose de repouso, para o jump scare controlar cabeça e boca; ele religa no reaparecimento. Os clipes não mexem na `Boca` e mantêm a cabeça nivelada, para os feixes não balançarem. São placeholders procedurais.

A versão anterior, sem animação, está em `Assets/Zatan/Prefabs/Zatan_V00_SemAnimacao.prefab`. Para voltar a ela, apague o `Zatan` da cena, arraste o prefab para `_Zatan` e religue no Inspector o `target` da percepção, da tensão, do cérebro e da captura, o jogador em `beamIgnored` e os minigames no `onFinished` da captura.

## Limites atuais

- Na `Level_Design`, Zatan usa o modelo `Assets/Zatan/Model/Zatan_V00.fbx` (3,3 m, peças rígidas), com a animação procedural acima. Na `Zatan_Test` ele ainda é a cápsula. Os sons continuam sintetizados. Lista de sons em [ASSET_LEDGER.md](ASSET_LEDGER.md).
- As tendas têm só uma zona interior, `Zatan_IndoorZone_BigTop`, com caixa aproximada.
- Portas fechadas e esconderijos ainda não são tratados.
- O sistema não usa os scripts dos Spikes 1 e 2.
