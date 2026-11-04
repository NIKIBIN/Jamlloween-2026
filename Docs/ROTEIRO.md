# Roteiro do jogo (cena Level_Design)

O roteiro abaixo é o guia de eventos do jogo. Cada passo diz o que já funciona na cena `Level_Design` e o que falta para a versão final.

- **Pronto:** funciona com a mecânica real.
- **Placeholder:** funciona, mas com som, objeto ou ação temporária.
- **Faltando:** ainda não existe.

O roteiro inteiro, do trailer até os créditos, foi testado no Play Mode em sequência, sem erros no console. Os minigames foram concluídos pelo atalho do debug, não jogados à mão.

## Como funciona

- **Progressão:** usa o sistema do time (`ProgressionDirector`), com 22 etapas só do roteiro em `Assets/Data/Progression/Roteiro` (R00 a R21). As etapas da `Zatan_Test` não mudaram.
- **Mudar as etapas:** edite `Assets/Editor/RoteiroSetup.cs` e rode o menu **Jamlloween > Progression > Roteiro: Rebuild Steps On Selected Director**, com o `ProgressionDirector` selecionado. O menu reescreve os assets sem trocar o arquivo de cada etapa, então as ligações no `ProgressionEventRelay` continuam valendo.
- **Debug:** F4 abre a lista de etapas e pula direto para qualquer uma. O pulo conclui os minigames das etapas anteriores e põe Zatan para dormir nas etapas calmas.
- **Zatan na história:** `_Zatan/Zatan_Roteiro` tem o `ZatanDirector` e o `ZatanSafeRoom` (o trailer).
  - `ZatanDirector` deixa Zatan fora do mundo, começa uma perseguição, faz ele se aproximar devagar ou atacar no final, e devolve o jogador ao checkpoint depois de uma captura.
  - `ZatanSafeRoom` cuida do trailer: com o jogador dentro, Zatan bate na porta, ronda e some.
  - Os eventos da progressão chegam até eles pelo `ProgressionEventRelay`, ligado no Inspector em `_Sistema/Progression`.
- **Objetos do roteiro:** ficam no grupo `_Roteiro` da Hierarchy: carrossel, labirinto, conserto e passeio na roda-gigante, créditos, pontos de spawn e checkpoints.
- **Teste:** o `ZatanTestHarness` está desligado na Level_Design, então F10 e o painel de teste não funcionam nela.

## Passo a passo

| # | Roteiro | Status | O que já tem | O que falta |
|---|---|---|---|---|
| 1 | Começa na safe zone, dentro do trailer | Pronto | Jogador nasce dentro do trailer (`_Roteiro/PlayerSpawn_Trailer`) | Abertura: fade de entrada e primeira fala |
| 2 | Minigame do rádio e pegar a lanterna | Pronto | Rádio e lanterna na mesa do trailer. Rádio primeiro, depois a lanterna | Sons finais do rádio |
| 3 | Consertar Zatan na entrada do circo; evento sonoro no gerador | Placeholder | Painel de conserto na cabine da entrada. Um gatilho no caminho toca um estouro no gerador | Som final; efeito visual no gerador (fumaça, luz piscando) |
| 4 | Ao consertar Zatan, a energia cai | Placeholder | Luz direcional e 3 lâmpadas do parque desligam, com som de queda de energia | Som final; mais luzes do parque no `LightGroup_Park` |
| 5 | Pegar a gasolina no fundo do circo | Placeholder | Galão no fundo da tenda, atrás da arquibancada | Posição e iluminação finais |
| 6 | Na volta, ouve Zatan escapando da caixa | Placeholder | Gatilho na saída da tenda toca o som de Zatan escapando. A cabeça da cartomante some da máquina | Som final; cabine quebrada (porta aberta, vidro) |
| 7 | Voltar para a safe zone, onde está o gerador | Pronto | Gerador do lado de fora do portão oeste | Nada |
| 8 | Minigame do gerador | Pronto | Minigame do time ligado ao galão | Sons do gerador (corda, falha, ligado) |
| 9 | Ao ligar o gerador, Zatan avista o jogador de longe e persegue | Placeholder | Zatan aparece a cerca de 28 m (`_Roteiro/Zatan_StareSpawn_Gerador`, com linha de visão do gerador), parado, só encarando o jogador, com os holofotes ainda brancos. Quando o jogador olha para ele (câmera a até 14° dele, sem nada no meio), os olhos ficam amarelos, ele espera 2 s e corre atrás do jogador. Se o jogador nunca olhar, a perseguição começa sozinha depois de 20 s. Ajustes no `ZatanDirector` (Stare). Captura devolve o jogador ao checkpoint e a perseguição recomeça | Ajuste de distância e velocidade; música de perseguição |
| 10 | Zatan não entra no trailer, bate na porta, ronda e some | Placeholder | Ele vai até a porta, bate 4 vezes, ronda 8 s, se afasta e some. O rádio fala do carrossel | Animação de bater (hoje ele só fica de frente para a porta); sons finais |
| 11 | Consertar o carrossel | Pronto | Carrossel em `PF_carousel`, com a sequência do time: notar o cavalo sem cabeça, pegar a cabeça atrás do carrossel, encaixar e examinar o quadro, que diz que falta um fusível | Luzes de trabalho no carrossel; posição final da cabeça |
| 12 | Fusível numa clareira no meio do labirinto | Placeholder | Labirinto de sebes altas (`_Roteiro/PH_Labirinto`, 90 paredes `PF_HedgeWall_4m_Alta`, corredores de 4 m). A primeira metade, da entrada sul em (36, 26) até a clareira, tem bifurcações e becos (≈ 45 m). O fusível fica numa clareira aberta de ≈ 6,5 × 6,5 m em (38, 58) | Arte final do labirinto; caixa de peças em volta do fusível |
| 13 | Perseguição ao pegar o fusível | Placeholder | Pegar o fusível começa a perseguição: Zatan surge na primeira metade, ≈ 22 m atrás, e entra na clareira atrás do jogador. Da clareira até a saída é um corredor único e sinuoso, sem bifurcações nem becos (≈ 82 m de caminho), até a saída oeste em (10, 84). Captura volta o jogador para a clareira e a perseguição recomeça | Som final |
| 14 | Fugir para o trailer por outra entrada | Placeholder | A saída do labirinto dá na `Porta_2` da safe zone (portão leste). Ela fica trancada até o fusível ser pego; nessa hora abre sozinha e fica aberta de vez (não dá para fechar), e Zatan passa por ela. O trailer repete a desistência de Zatan | Deixar claro que é o caminho (luz, placa) |
| 15 | Consertar o carrossel | Pronto | Quadro de fusíveis do time, ao lado do carrossel. Consertado, o carrossel gira com música | Nada |
| 16 | Consertar a roda-gigante | Placeholder | `_Roteiro/RodaGigante_Conserto/PH_PainelRodaGigante`: painel greybox na base da roda-gigante que já existe. E no painel trava o jogador 4 s com som de ferramenta, e a roda fica pronta (ela só gira no passo 18) | Painel modelado da roda-gigante; minigame próprio |
| 17 | Após consertar a roda-gigante, Zatan começa a se aproximar da posição do player, que está encurralado | Placeholder | Zatan aparece ao norte e anda devagar (1,3 m/s) até o jogador, parando a 4 m. Ele nunca captura nesse passo | Animação de andar lento e ameaçador; um bloqueio real que deixe a gôndola como única saída |
| 18 | A porta de uma gôndola abre, o jogador entra e a roda gira | Placeholder | Na roda-gigante que já existe, a gôndola mais baixa ganha uma porta greybox que abre sozinha. E entra: o jogador fica preso na gôndola, a câmera vira só 50° para cada lado e a roda gira | Porta modelada na gôndola; um pouco de balanço da gôndola |
| 19 | A roda para e começa o som de tensão | Placeholder | A roda para quando a gôndola chega ao topo (cerca de 17 s), com som de freio e um drone de tensão. A câmera passa a virar quase 180° | Sons finais; vento lá em cima |
| 20 | Zatan ataca quando o jogador olha para trás | Placeholder | 3 s depois da parada, virar a câmera mais de 125° para trás chama o jump scare de Zatan, do lado de fora da gôndola. Se o jogador demorar, um rangido atrás dele dá a dica | Pose de Zatan agarrado na gôndola; som final do rangido |
| 21 | Créditos | Placeholder | Tela preta com texto subindo e música de caixinha. No fim, R joga de novo e Esc sai | Nomes da equipe; arte e música finais |

## O que falta para finalizar o jogo

Prioridade para a jam (MoSCoW): **Must** sem isso o roteiro não fecha ou não dá medo; **Should** melhora muito; **Could** polimento; **Won't** fica para depois da jam.

### Arte e modelos

| Item | Passo | Prioridade |
|---|---|---|
| Painel de controle modelado da roda-gigante no lugar do greybox | 16 | Should |
| Porta de verdade na gôndola da roda-gigante | 18 | Should |
| Labirinto final de sebes, com saída e entrada bem marcadas | 12–13 | Must |
| Cabine de Zatan quebrada depois da fuga (vidro, porta aberta) | 6 | Should |
| Caixa de peças em volta do fusível na clareira do labirinto | 12 | Could |
| Fumaça e luz piscando no gerador quando ele engasga | 3 | Could |
| Boca articulada no modelo de Zatan (já pendente no [ASSET_LEDGER](ASSET_LEDGER.md)) | 9–20 | Should |

### Animações

| Item | Passo | Prioridade |
|---|---|---|
| Zatan batendo na porta do trailer | 10, 14 | Should |
| Zatan andando devagar e ameaçador | 17 | Should |
| Zatan agarrado do lado de fora da gôndola antes do ataque | 20 | Should |
| Animação de andar e correr feita à mão (hoje é procedural) | todos | Should |
| Gôndola balançando quando a roda para | 19 | Could |
| Porta da cabine abrindo quando Zatan escapa | 6 | Could |

### Áudio

Os sons do roteiro estão no [ASSET_LEDGER](ASSET_LEDGER.md), seção **Roteiro**, todos como Placeholder. A prioridade dos sons que já existiam está no [AUDIO_MOSCOW](AUDIO_MOSCOW.md).

| Item | Passo | Prioridade |
|---|---|---|
| Ambiente noturno (ainda falta, item 1 do AUDIO_MOSCOW) | todos | Must |
| Batida na porta do trailer (`DoorBang`) | 10, 14 | Must |
| Zatan escapando da cabine (`ZatanEscape`) | 6 | Must |
| Susto ao pegar o fusível (`MazeChase_Sting`) | 13 | Must |
| Tensão no alto da roda (`Tension_Loop`) e rangido atrás (`Gondola_Creak`) | 19–20 | Must |
| Gerador engasgando (`Generator_Backfire`) e energia caindo (`PowerDown`) | 3–4 | Should |
| Roda-gigante girando e parando (`Wheel_Loop`, `Wheel_Stop`), porta da gôndola (`Gondola_Door`) | 18–19 | Should |
| Conserto da roda-gigante (`RodaGigante_Repair_Loop`, `RodaGigante_Start`) | 16 | Should |
| Música dos créditos (`MUS_Roteiro_Credits`) | 21 | Should |
| Falas do rádio gravadas (hoje só legenda) | 2, 3, 10, 12, 16 | Could |
| Música de perseguição própria do roteiro | 9, 13 | Could |

### Level design

| Item | Passo | Prioridade |
|---|---|---|
| Labirinto definitivo: o greybox já tem o tamanho e o traçado (sebes altas, clareira do fusível no meio, corredor único até o portão leste); falta arte, iluminação e ajuste de duração | 12–13 | Should |
| Caminho do labirinto até o portão leste da safe zone sinalizado (luz, placa, cerca guiando) | 14 | Must |
| Portão leste: hoje começa fechado (o jogador abre com E) e o NavMesh foi gerado com ele fechado, então Zatan dá a volta pelo outro portão | 14 | Should |
| Encurralar de verdade perto da roda-gigante (barreiras), para a gôndola ser a única saída | 17–18 | Should |
| Mais luzes do parque no `LightGroup_Park` para a queda de energia aparecer | 4 | Should |
| Altura de Zatan (3,3 m) maior que o teto da tenda (2,9 m) | 5–6 | Should |
| Grupo `_Gameplay` (lixeiras-esconderijo) desligado na cena: ligar ou tirar | todos | Could |

### Sistemas e código

| Item | Passo | Prioridade |
|---|---|---|
| Minigame próprio da roda-gigante no lugar do `PlaceholderRepair` | 16 | Should |
| Percepção de Zatan não reinicia ao reaparecer (pendência antiga) | 9, 13 | Should |
| Picos de velocidade de Zatan acima do esperado (pendência antiga) | 9, 13 | Should |
| Salvar progresso ou checkpoints entre sessões | todos | Won't |
| Remover os grupos antigos desligados (`_Minigames`, `Ambiente/Atracoes`), que têm cópias velhas do carrossel e do quadro | — | Could |

### UI

| Item | Passo | Prioridade |
|---|---|---|
| Nomes da equipe nos créditos (hoje "[nome]") | 21 | Must |
| Menu inicial (jogar, sair, volume) | — | Should |
| Menu de pausa | — | Should |
| Dica de controles no começo (WASD, Shift, E, F, G) | 1 | Should |
| Tela de título antes do trailer | 1 | Could |

### Polimento e QA

| Item | Prioridade |
|---|---|
| Jogar o roteiro inteiro à mão, sem o atalho F4, e medir a duração | Must |
| Testar a captura em cada perseguição: checkpoint certo e recomeço justo | Must |
| Testar a build (Level_Design já está em primeiro na lista de cenas da build) | Must |
| Ajustar velocidade e distância de Zatan em cada perseguição | Should |
| Conferir a leitura das falas do rádio no tempo certo | Should |
| Balancear escuridão e névoa ao longo do roteiro | Could |

## Sons placeholder do roteiro

Em `Assets/Audio/Roteiro/Placeholder`. A lista com o som final desejado de cada um está no [ASSET_LEDGER](ASSET_LEDGER.md), seção **Roteiro**.

| Arquivo | Passo |
|---|---|
| SFX_Roteiro_Generator_Backfire_PH | 3 |
| SFX_Roteiro_PowerDown_PH | 4 |
| SFX_Roteiro_ZatanEscape_PH | 6 |
| SFX_Roteiro_DoorBang_01_PH, _02_PH | 10, 14 |
| SFX_Roteiro_MazeChase_Sting_PH | 13 |
| SFX_Roteiro_RodaGigante_Repair_Loop_PH, RodaGigante_Start_PH | 16 |
| SFX_Roteiro_Gondola_Door_PH | 18 |
| SFX_Roteiro_Wheel_Loop_PH, Wheel_Stop_PH | 18–19 |
| SFX_Roteiro_Tension_Loop_PH, Gondola_Creak_PH | 19–20 |
| MUS_Roteiro_Credits_PH | 21 |
