# Asset Ledger

Lista dos sons do jogo, com o status de cada um:

- **Placeholder**: já toca no jogo, mas é temporário e precisa ser substituído pelo som final.
- **Faltando**: ainda não existe nenhum som. Onde o código ainda não tem como disparar o som, a coluna Gatilho diz "sem gatilho".

Hoje nenhum som é final: tudo que toca é placeholder.

As prioridades para a jam (Must, Should, Could) estão em [AUDIO_MOSCOW](AUDIO_MOSCOW.md).

Seções: [Zatan](#zatan), [Jogador](#jogador), [Minigames](#minigames), [Modelo e animação de Zatan](#modelo-e-animação-de-zatan).

Formato dos arquivos finais: WAV, 48 kHz, mono para sons 3D e loops emendando sem estalo.

## Zatan

Cartomante mecânico amaldiçoado: corda, engrenagens, madeira velha, latão, cartas de tarô e caixa de música.

Os placeholders são WAVs sintetizados em `Assets/Zatan/Audio/Placeholder/`, com o prefixo `SFX_Zatan_` e o sufixo `_PH.wav`, mono e em 3D. Para trocar, substitua o clipe no Inspector de `ZatanAudio`, `ZatanFootsteps` ou `ZatanCatchSequence`, no objeto `Zatan`. O volume segue a tensão (0,2 com tensão 0 e 1,0 com tensão 100), exceto onde a tabela indica outra regra.

| Som | Quando toca | Tipo | Som final desejado | Placeholder | Status |
|---|---|---|---|---|---|
| Passo | Andando, tensão abaixo de 60 | One-shot (4 variações) | Pé de madeira e latão, com rangido de junta | `Step_01` a `Step_04` | Placeholder |
| Passo pesado | Andando, tensão de 60 para cima | One-shot (2 variações) | Pisada pesada, com peças soltas chacoalhando | `StepHeavy_01`, `StepHeavy_02` | Placeholder |
| Mecanismo | Patrulha, investigação e busca | Loop | Tique-taque de corda com zumbido de engrenagens | `Clockwork_Loop` | Placeholder |
| Perseguição | Durante a perseguição | Loop | Fole ofegante e engrenagens em disparada | `Chase_Loop` | Placeholder |
| Tique próximo | Até 10 m do jogador; volume pela distância | Loop | Tique seco de relógio de bolso | `NearTick_Loop` | Placeholder |
| Alerta | Avista o jogador, no início da pausa de 0,5 s; volume fixo em 1 | One-shot | Metais dissonantes com pancada metálica | `Alert_Sting` | Placeholder |
| Servo | Começa a investigar um barulho | One-shot | Pescoço mecânico girando e travando | `Investigate_Servo` | Placeholder |
| Corda acabando | Perde o jogador | One-shot | Mola perdendo a força, com tiques cada vez mais lentos | `Lost_WindDown` | Placeholder |
| Murmúrio | Patrulha e busca, a cada 15–35 s | One-shot (2 variações) | Voz rachada de autômato murmurando previsões | `Voice_Mutter_01`, `Voice_Mutter_02` | Placeholder |
| Cartas | Some | One-shot | Baralho de tarô embaralhado, terminando em um corte seco | `Vanish_Cards` | Placeholder |
| Caixa de música | Reaparece | One-shot | Melodia menor, desafinada e desacelerando | `Reappear_MusicBox` | Placeholder |
| Captura | Pega o jogador; volume fixo em 1 | One-shot | Golpe pesado com guincho de metal | `Caught_Sting` | Placeholder |
| Agarrão | Início do jump scare; 2D | One-shot | Mãos de metal batendo nos ombros, com pancada grave | `Catch_Grab` | Placeholder |
| Chacoalhão | Durante o chacoalhão; 2D | Loop | Peças soltas, engrenagens e madeira batendo | `Catch_Shake_Loop` | Placeholder |
| Mordida | Impacto da mordida; 2D | One-shot | Dentes de latão estalando, com mastigada | `Catch_Bite` | Placeholder |
| Passo correndo | Correndo na perseguição (hoje usa o passo de andar) | One-shot (2 a 4 variações) | Pisada rápida e pesada, com o corpo batendo | — | Faltando |
| Rangido de junta | Nos trancos da caminhada mecânica | One-shot (3 variações) | Junta de madeira e latão rangendo | — | Faltando (sem gatilho) |
| Engrenagem travando | Ao parar para encarar o jogador | One-shot | Engrenagens freando de uma vez | — | Faltando |
| Cabeça girando | Giro rápido da cabeça antes de correr | One-shot | Pescoço mecânico girando rápido | — | Faltando |
| Frases do cartomante | Eventos roteirizados, jogador à vista | One-shot | Falas gravadas, com voz de autômato de parque | — | Faltando (sem gatilho) |
| Carta virada | Revelação de carta em evento | One-shot | Carta batendo na mesa | — | Faltando (sem gatilho) |
| Arrombar porta | Quando Zatan tratar portas trancadas | Loop + one-shot | Pancadas de madeira e latão, depois a porta cedendo | — | Faltando (sem gatilho) |
| Vasculhar esconderijo | Quando houver esconderijos | One-shot | Dedos de metal abrindo e tateando | — | Faltando (sem gatilho) |
| Risada | Jogador escapa ou evento roteirizado | One-shot | Gargalhada mecânica que trava e repete | — | Faltando (sem gatilho) |

## Jogador

Hoje o jogador não tem nenhum som próprio. Zatan "ouve" os passos por um raio invisível (`walkNoiseRadius` e `runNoiseRadius` em `ZatanConfig`), mas nada toca. Sons do jogador são 2D, exceto onde a tabela diz 3D. Prefixo sugerido: `SFX_Player_`.

| Som | Quando toca | Tipo | Som final desejado | Gatilho | Status |
|---|---|---|---|---|---|
| Passo na midway | Andando no cascalho | One-shot (4 variações) | Bota em cascalho e terra batida | `LevelWalker`, pelo tile do `Tilemap_Chao` | Faltando |
| Passo na grama | Andando na grama | One-shot (4 variações) | Bota em grama úmida | Idem | Faltando |
| Passo na serragem | Andando sob a lona | One-shot (4 variações) | Pisada abafada em serragem | Idem | Faltando |
| Passo na floresta | Andando nas folhas | One-shot (4 variações) | Folhas secas e galhos | Idem | Faltando |
| Passo em madeira | Dentro do trailer e da casa maluca | One-shot (4 variações) | Tábua oca rangendo | Idem, pelo collider do piso | Faltando |
| Passo correndo | Correndo, em cada superfície acima | One-shot (2 a 4 variações por superfície) | Pisada mais forte e rápida | `LevelWalker` com Shift | Faltando |
| Respiração cansada | Depois de correr alguns segundos | Loop | Ofegar contido | Tempo correndo (`LevelWalker`) | Faltando |
| Respiração de pânico | Durante a perseguição | Loop | Ofegar curto e assustado | `ZatanBehaviourBrain.StateChanged` (Chase) | Faltando |
| Susto | Zatan avista o jogador | One-shot | Inspiração brusca | `StateChanged` (Spotted) | Faltando |
| Batimento | Zatan a menos de uns 10 m | Loop | Coração acelerando com a distância | Distância de Zatan | Faltando |
| Grito abafado | Início do jump scare | One-shot | Grito cortado pela mão de Zatan | `ZatanCatchSequence` | Faltando |
| Lanterna ligando | Tecla F, ligando | One-shot | Clique de interruptor de lanterna antiga | `PlayerFlashlight.Toggled(true)` | Faltando |
| Lanterna desligando | Tecla F, desligando | One-shot | Clique mais seco | `PlayerFlashlight.Toggled(false)` | Faltando |
| Pegar a lanterna | Ao achá-la no trailer | One-shot | Lanterna pega da mesa, com o primeiro clique | `PlayerFlashlight.Acquired` | Faltando |
| Pegar item | Tecla E em um item | One-shot | Mão pegando objeto pequeno | `LevelWalker.Open` (Pickup) | Faltando |
| Porta abrindo | Tecla E numa porta; 3D | One-shot | Dobradiça enferrujada | `LevelWalker.Open` (Animation) | Faltando |
| Porta fechando | Segundo E na porta; 3D | One-shot | Batida de madeira | Idem | Faltando |
| Bloqueado | Interação sem o item certo | One-shot | Maçaneta travada | `ProgressionDirector.TryUse` | Faltando |
| Prancheta subindo | Abrir a prancheta | One-shot | Papel e prancheta erguidos | `Clipboard` | Faltando |
| Prancheta descendo | Fechar a prancheta | One-shot | Papel abaixando | `Clipboard` | Faltando |
| Tarefa riscada | Tarefa concluída na prancheta | One-shot | Lápis riscando papel | `ProgressionDirector` (passo concluído) | Faltando |
| Entrar no minigame | Ao começar um minigame | One-shot | Mãos e ferramentas se ajeitando | `PlayerInteractor` | Faltando |
| Sair do minigame | Esc no minigame | One-shot | Ferramentas guardadas | Saída do minigame | Faltando |

## Minigames

Os placeholders dos minigames não são arquivos: cada componente de áudio (`RadioAudio`, `CarouselAudio`, `ZatanRepairAudio`) gera o som por código quando a cena abre. Onde a coluna "Campo para o som final" tem um nome, já existe um campo no Inspector para arrastar o clipe final; onde diz "—", o campo ainda precisa ser criado no código. Todos os sons de minigame são 3D.

### Rádio (`RadioAudio`, no rádio do trailer)

| Som | Quando toca | Tipo | Som final desejado | Campo para o som final | Status |
|---|---|---|---|---|---|
| Clique de encaixe | Tampa traseira e pilhas encaixando | One-shot | Plástico e mola de rádio antigo encaixando | — | Placeholder |
| Estática | Rádio ligado fora da frequência; diminui perto da frequência certa | Loop | Chiado de rádio AM com ondulação | — | Placeholder |
| Voz do patrão | Ao sintonizar a frequência certa | One-shot | Fala gravada do patrão, com filtro de rádio | `bossVoice` | Placeholder |
| Virar o rádio | Clicar para ver as costas ou a frente | One-shot | Rádio de plástico raspando na mesa | — | Faltando |
| Girar o dial | Enquanto o dial gira | Loop | Dial de plástico com cliques de sintonia | — | Faltando |
| Tampa saindo | Puxão que solta a tampa traseira | One-shot | Plástico estalando ao soltar | — | Faltando |

### Caixa de fusíveis (`FuseBoxMinigame`, no painel de energia)

O minigame inteiro está sem som.

| Som | Quando toca | Tipo | Som final desejado | Campo para o som final | Status |
|---|---|---|---|---|---|
| Parafuso girando | Segurando o botão em um parafuso | Loop | Chave de fenda rangendo no metal | — | Faltando |
| Parafuso solto | Cada um dos 4 parafusos ao soltar | One-shot | Parafuso caindo da rosca | — | Faltando |
| Tampa saindo | Clicar na tampa | One-shot | Chapa de metal sendo tirada | — | Faltando |
| Tirar fusível queimado | Clicar no fusível queimado | One-shot | Fusível de vidro saindo do encaixe | — | Faltando |
| Encaixar fusível novo | Clicar no encaixe vazio | One-shot | Fusível encaixando com estalo | — | Faltando |
| Alavanca descendo | Arrastando a alavanca | Loop | Alavanca pesada rangendo | — | Faltando |
| Alavanca voltando | Soltar antes do fim | One-shot | Mola puxando a alavanca de volta | — | Faltando |
| Energia voltando | Alavanca no fim | One-shot + loop | Estalo elétrico, depois zumbido de transformador | — | Faltando |

### Carrossel (`CarouselAudio`, no carrossel)

Usado pela fase da cabeça (`CarouselHeadMinigame`) e pela fase do mecanismo (`CarouselMechanismMinigame`). Os sons de motor forçando e correia estourando saíram da lista: o código ainda gera esses placeholders, mas eles não terão som final.

| Som | Quando toca | Tipo | Som final desejado | Campo para o som final | Status |
|---|---|---|---|---|---|
| Clique de encaixe | Cabeça do cavalo travando, tampa, correia, tensor e alavanca | One-shot | Peça de metal encaixando | — | Placeholder |
| Rangido | Ao tirar o objeto preso nas engrenagens | One-shot | Metal raspando em metal, alto | `creakClip` | Placeholder |
| Zumbido do motor | Motor ligado | Loop | Motor elétrico velho | — | Placeholder |
| Música do carrossel | Carrossel consertado girando | Loop | Valsa de órgão de carrossel, desafinada e lenta | `Zatan-carousel_temp` (com `CreepyMusicGlitch`: tom oscilando, quedas e falhas) | Em uso (temporário) |
| Cabeça girando | Girando a cabeça do cavalo no pescoço | Loop | Madeira e metal raspando | — | Faltando |
| Tensor girando | Esticando a correia | Loop | Catraca de tensor | — | Faltando |
| Carrossel girando | Plataforma em movimento | Loop | Estrutura de madeira rangendo e rodas no trilho | — | Faltando |

### Painel de Zatan (`ZatanRepairAudio`, nas costas da máquina da cartomante)

| Som | Quando toca | Tipo | Som final desejado | Campo para o som final | Status |
|---|---|---|---|---|---|
| Clique | Mangueira encaixando e engrenagem travando no zero | One-shot | Engate pneumático e trava metálica | — | Placeholder |
| Clunk da chave | Ligar a chave geral; mais fraco ao virar de novo | One-shot | Chave geral pesada dos anos 80 | `clunkClip` | Placeholder |
| Respiração do compressor | Compressor ligado; acelera com a manivela | Loop | Compressor pneumático respirando | `breathClip` | Placeholder |
| Solenoide | Junto com a respiração | One-shot (repetido) | Cliques de válvula solenoide | `solenoidClip` | Placeholder |
| Zumbido | Compressor ligado | Loop | Zumbido elétrico baixo | — | Placeholder |
| Rangido da manivela | Girando a manivela; mais grave perto do fim | Loop | Manivela de ferro rangendo | `creakClip` | Placeholder |
| Vazamento | Junta vazando piche | Loop | Líquido grosso escorrendo e borbulhando | `leakClip` | Placeholder |
| Estopa esfregando | Esfregando com a estopa | Loop | Pano áspero raspando no metal | — | Placeholder |
| Suspiro final | Engrenagem travando no zero | One-shot | Compressor soltando o ar de uma vez | `sighClip` | Placeholder |
| Tampa saindo | Puxão que solta a tampa | One-shot | Chapa de metal estalando | — | Faltando |
| Cabeça virada (jump scare) | A câmera revela a cabeça virada 180° | One-shot | Silêncio cortado por um estalo grave | — | Faltando |

### Gerador (galão de gasolina e gerador)

Ainda não existe código para o galão nem para ligar o gerador (`PF_generator_trailer` e `PF_jerry_can` são só objetos do kit), então todos estes sons estão sem gatilho até esse minigame ser feito.

| Som | Quando toca | Tipo | Som final desejado | Campo para o som final | Status |
|---|---|---|---|---|---|
| Líquido no galão | Andando com o galão de gasolina | Loop (volume pelo movimento) | Gasolina balançando dentro de um galão de plástico | — | Faltando (sem gatilho) |
| Colocar gasolina | Despejando o galão no gerador | Loop | Líquido caindo num tanque de metal, borbulhando | — | Faltando (sem gatilho) |
| Puxão de corda | Cada puxada na corda de partida | One-shot (2 a 3 variações) | Corda de partida sendo puxada com força | — | Faltando (sem gatilho) |
| Gerador falhando | Puxada que não pega | One-shot (2 variações) | Motor engasgando e morrendo | — | Faltando (sem gatilho) |
| Gerador ligado | Depois que pega | One-shot + loop | Partida pegando, depois motor a gasolina em marcha lenta | SFX_Roteiro_Generator_Running_Loop_PH (loop; sem a partida) | Placeholder |

### Esconderijos (lixeiras, `ZatanHidingSpot`)

Placeholders sintetizados em `Assets/Zatan/Audio/Placeholder/`, ligados no prefab `Assets/Zatan/Hiding/PF_TrashCan_Hide.prefab` (3D, em cada lixeira).

| Som | Quando toca | Tipo | Som final desejado | Campo para o som final | Status |
|---|---|---|---|---|---|
| Lixeira tombando | Zatan derruba a lixeira | One-shot | Lata de metal raspando e caindo, com lixo chacoalhando | `tipClip` (`SFX_Hide_TrashCan_Tip_PH`) | Placeholder |
| Entrar na lixeira | Jogador se esconde | One-shot | Tampa batendo e lixo se mexendo | `enterClip` (`SFX_Hide_TrashCan_Enter_PH`) | Placeholder |
| Sair da lixeira | Jogador sai | One-shot | Tampa abrindo e lixo raspando | `exitClip` (`SFX_Hide_TrashCan_Exit_PH`) | Placeholder |
| Tampa tremendo | Jogador escondido com Zatan perto | Loop | Tampa de metal vibrando de leve | — | Faltando (sem gatilho) |

### Progressão (`RadioDialogue`, `ProgressionDirector`, `StepTrigger`)

| Som | Quando toca | Tipo | Som final desejado | Campo para o som final | Status |
|---|---|---|---|---|---|
| Falas do rádio | As 8 falas da progressão na `Zatan_Test`; hoje só aparecem como legenda | One-shot (8 falas) | Voz do patrão gravada, com filtro de rádio | `audio` em cada fala | Faltando |
| Sons das etapas | Ao começar ou concluir uma etapa, se configurado | One-shot | Definir por etapa | `clip` em cada etapa e `sound` no `StepTrigger` | Faltando |

### Roteiro (`Level_Design`, em `Assets/Audio/Roteiro/Placeholder`)

Sons dos eventos do roteiro ([ROTEIRO](ROTEIRO.md)). Todos são placeholders gerados por código.

| Som | Quando toca | Tipo | Som final desejado | Placeholder | Status |
|---|---|---|---|---|---|
| Gerador engasgando | Passo 3: o jogador passa ao lado do gerador a caminho do Zatan | One-shot (3D) | Motor tossindo e estourando, com metal tremendo | `SFX_Roteiro_Generator_Backfire_PH` | Placeholder |
| Energia caindo | Passo 4: ao consertar Zatan | One-shot (2D) | Zumbido elétrico morrendo e lâmpadas estalando em sequência | `SFX_Roteiro_PowerDown_PH` | Placeholder |
| Zatan escapando | Passo 6: na volta com a gasolina, vindo da cabine | One-shot (3D) | Vidro quebrando, madeira estalando e engrenagens disparando | `SFX_Roteiro_ZatanEscape_PH` | Placeholder |
| Batida na porta | Passos 10 e 14: Zatan na porta do trailer | One-shot (2 variações, 3D) | Punho de madeira e latão contra porta de metal do trailer | `SFX_Roteiro_DoorBang_01_PH`, `_02_PH` | Placeholder |
| Susto no labirinto | Passo 13: metade do labirinto, início da perseguição | One-shot (3D) | Estalo de galhos e metais dissonantes | `SFX_Roteiro_MazeChase_Sting_PH` | Placeholder |
| Conserto da roda-gigante | Passo 16: enquanto o jogador conserta o painel na base da roda | Loop (3D) | Catraca e chave inglesa em metal | `SFX_Roteiro_RodaGigante_Repair_Loop_PH` | Placeholder |
| Roda-gigante pronta | Passo 16: fim do conserto | One-shot (3D) | Clunk do disjuntor e motor subindo | `SFX_Roteiro_RodaGigante_Start_PH` | Placeholder |
| Porta da gôndola | Passo 18: a porta abre sozinha | One-shot (3D) | Dobradiça enferrujada rangendo devagar | `SFX_Roteiro_Gondola_Door_PH` | Placeholder |
| Roda-gigante girando | Passo 18–19: durante a subida | Loop (3D) | Estrutura rangendo e motor grave | `SFX_Roteiro_Wheel_Loop_PH` | Placeholder |
| Roda-gigante parando | Passo 19: a roda trava no alto | One-shot (3D) | Freio metálico batendo e a gôndola balançando | `SFX_Roteiro_Wheel_Stop_PH` | Placeholder |
| Tensão no alto | Passo 19–20: depois que a roda para | Loop (2D) | Drone grave com batimento e vento | `SFX_Roteiro_Tension_Loop_PH` | Placeholder |
| Rangido atrás | Passo 20: dica para o jogador olhar para trás | One-shot (3D) | Metal rangendo bem atrás da cabeça | `SFX_Roteiro_Gondola_Creak_PH` | Placeholder |
| Música dos créditos | Passo 21 | Loop (2D) | Caixa de música do parque, desafinada | `MUS_Roteiro_Credits_PH` | Placeholder |
| Ambientação geral | Sempre fora do trailer; abafada dentro da tenda do circo | Loop (2D) | Paisagem sonora noturna do parque | `DEMO_SOUNDSCAPE_Zatan` (`_Sistema/Ambientacao`, `AmbienceZones`) | Em uso (demo) |
| Música do trailer | Sempre, no rádio da segunda mesa do trailer | Loop (3D, até 6 m) | Música de rádio velho, com chiado | `safe_room_demo` + `SFX_Roteiro_RadioStatic_Loop_PH` (filtros de baixa qualidade) | Em uso (demo) |

## Modelo e animação de Zatan

| Asset | Arquivo | Status |
|---|---|---|
| Zatan com rig | `Assets/Zatan/Run.fbx` (esqueleto Mixamo, 3,3 m), materiais `M_ZatanModel.mat` e `M_ZatanRoupa.mat` | Em uso na `Level_Design` (`_Zatan/Zatan/ModelRig`); sem osso de mandíbula: a mordida não abre a boca |
| Corrida de Zatan | `Zatan_Rig_Run.anim`, tirada do FBX (no lugar, tronco e cabeça endireitados) | Em uso; animação real do Mixamo |
| Idle, Walk, Stare, Stalk, Bang, Catch | `Assets/Zatan/Animation/Rig/Zatan_Rig_*.anim` e `Zatan_Rig.controller`, gerados pelo menu `Jamlloween/Zatan/Gerar animações do rig` | Placeholder gerado por código a partir da pose da corrida; substituir por animações feitas à mão (Walk e Bang são os mais visíveis) |
| Zatan V00 (antigo) | `Assets/Zatan/Model/Zatan_V00.fbx` e as animações procedurais `Assets/Zatan/Animation/Zatan_*.anim` | Fora da `Level_Design` (backup em `Assets/Zatan/Prefabs/Zatan_Backup_AntesDoRig.prefab`); ainda pode ser usado em outras cenas |
