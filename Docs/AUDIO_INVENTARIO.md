# Inventário de áudio (estado em 2026-11-06)

Esta lista mostra os sons que o jogo toca. Use as colunas **Nota** e **Ação** para marcar o que fica e o que você vai trocar.

Em 2026-11-06, gravações reais do Freesound (todas CC0) substituíram os placeholders (`_PH`) e os sons gerados por código. Também entraram sons novos: passos do jogador, lanterna, pegar item, porta do trailer e caixa de fusíveis. As músicas e os nossos arquivos próprios (`window breaking.wav`, `steps-on-gravel.wav`) não mudaram.

Para trocar um som:

- Substitua o `.wav` no mesmo caminho e com o mesmo nome. O Unity mantém o `.meta`, então as referências continuam certas.
- Os scripts que antes sintetizavam som agora têm um campo de clipe para cada som. Com o campo vazio, o som sintético volta a tocar.

## Volume (equalização)

Cada arquivo foi normalizado para um alvo de loudness (LUFS integrado, pico máximo -1 dBFS). O alvo considera o volume que o componente aplica no jogo, para o jogador ouvir cada som no nível certo:

| Nível no jogo | Sons |
|---|---|
| Mais alto (susto) | captura do Zatan (agarrão, mordida, sting), batidas na porta, estouro do gerador |
| Alto | alerta do Zatan, susto do labirinto, lata derrubada, queda de energia, motor engasgando |
| Médio | passos do Zatan, resmungos, porta da gôndola, roda-gigante, correia, alavanca dos fusíveis |
| Baixo | passos do jogador, pegar item, porta do trailer, cliques, gasolina, loops do carrossel e do gerador |
| Bem baixo (fundo) | compressor, zumbido e solenoide do painel do Zatan, tique-taque, mecanismo de corda, estática do rádio |

Se um som ficar alto ou baixo demais, ajuste o volume no componente (Inspector), não no arquivo.

## 1. Zatan (`Assets/Zatan/Audio/`)

| Som | Quando toca | Arquivo | Nota | Ação |
|---|---|---|---|---|
| Mecanismo de corda (loop) | Perto do Zatan | `SFX_Zatan_Clockwork_Loop` | | |
| Corda acelerada (loop) | Durante a perseguição | `SFX_Zatan_Chase_Loop` | | |
| Tique-taque (loop) | Zatan perto do jogador | `SFX_Zatan_NearTick_Loop` | | |
| Passos (4) | Zatan andando | `SFX_Zatan_Step_01..04` | | |
| Passos pesados (2) | Zatan correndo | `SFX_Zatan_StepHeavy_01..02` | | |
| Sussurros (2) | Enquanto ronda | `SFX_Zatan_Voice_Mutter_01..02` | | |
| Engrenagem girando | Vai investigar um barulho | `SFX_Zatan_Investigate_Servo` | | |
| Alerta | Vê o jogador | `SFX_Zatan_Alert_Sting` | | |
| Corda parando | Desiste da perseguição | `SFX_Zatan_Lost_WindDown` | | |
| Cartas + whoosh | Some | `SFX_Zatan_Vanish_Cards` | | |
| Caixinha de música dando corda | Reaparece | `SFX_Zatan_Reappear_MusicBox` | | |
| Sting de captura | Pega o jogador | `SFX_Zatan_Caught_Sting` | | |
| Agarrão | Jump scare, início | `SFX_Zatan_Catch_Grab` | | |
| Metal chacoalhando (loop) | Jump scare, meio | `SFX_Zatan_Catch_Shake_Loop` | | |
| Mordida (armadilha de aço + osso) | Jump scare, fim | `SFX_Zatan_Catch_Bite` | | |
| Entrar na lata | Esconder | `SFX_Hide_TrashCan_Enter` | | |
| Sair da lata | Sair do esconderijo | `SFX_Hide_TrashCan_Exit` | | |
| Lata derrubada | Zatan derruba a lata | `SFX_Hide_TrashCan_Tip` | | |

## 2. Roteiro (`Assets/Audio/Roteiro/`)

| Som | Quando toca | Arquivo | Nota | Ação |
|---|---|---|---|---|
| Estática do rádio (loop) | Rádio do trailer | `SFX_Roteiro_RadioStatic_Loop` | | |
| Queda de energia | R02 e R19 | `SFX_Roteiro_PowerDown` | | |
| Gerador engasgando | Barulho que chama o Zatan | `SFX_Roteiro_Generator_Backfire` | | |
| Gerador funcionando (loop) | Depois que o gerador pega (também dentro do minigame) | `SFX_Roteiro_Generator_Running_Loop` | | |
| Batidas na porta (2) | R10 e R14 | `SFX_Roteiro_DoorBang_01..02` | | |
| Susto do labirinto | R13, pegar o fusível | `SFX_Roteiro_MazeChase_Sting` | | |
| Porta da gôndola | R18 | `SFX_Roteiro_Gondola_Door` | | |
| Roda-gigante girando (loop) | R18–R19 | `SFX_Roteiro_Wheel_Loop` | | |
| Roda-gigante parando | R19 | `SFX_Roteiro_Wheel_Stop` | | |
| Rangido da gôndola | R19–R20 | `SFX_Roteiro_Gondola_Creak` | | |
| Vidro quebrando (nosso) | Zatan escapa | `Assets/Audio/window breaking.wav` | | |
| Passos no cascalho (nosso) | Susto no minigame do gerador | `Assets/Audio/steps-on-gravel.wav` | | |

## 3. Minigames (`Assets/Audio/SFX/`)

| Som | Quando toca | Arquivo | Nota | Ação |
|---|---|---|---|---|
| Estática do dial (loop) | Rádio, girar a sintonia | `Radio/SFX_Radio_Static_Loop` | | |
| Clique | Rádio, encaixes | `Radio/SFX_Radio_Click` | | |
| Voz do patrão | Rádio sintonizado (voz AM real, sem palavras claras) | `Radio/SFX_Radio_BossVoice` | | |
| Respiração do compressor (loop) | Painel do Zatan | `ZatanRepair/SFX_ZatanRepair_Breath_Loop` | | |
| Zumbido do motor (loop) | Painel do Zatan | `ZatanRepair/SFX_ZatanRepair_Hum_Loop` | | |
| Solenoide | Painel do Zatan | `ZatanRepair/SFX_ZatanRepair_Solenoid` | | |
| Clique | Painel do Zatan | `ZatanRepair/SFX_ZatanRepair_Click` | | |
| Clunk da chave | Painel do Zatan | `ZatanRepair/SFX_ZatanRepair_Clunk` | | |
| Manivela (loop) | Girar a manivela | `ZatanRepair/SFX_ZatanRepair_Crank_Loop` | | |
| Vazamento (loop) | Junta vazando | `ZatanRepair/SFX_ZatanRepair_Leak_Loop` | | |
| Estopa (loop) | Limpar | `ZatanRepair/SFX_ZatanRepair_Scrub_Loop` | | |
| Fita (loop) | Passar a fita | `ZatanRepair/SFX_ZatanRepair_Tape_Loop` | | |
| Suspiro final | Fim do conserto | `ZatanRepair/SFX_ZatanRepair_Sigh` | | |
| Faíscas (4) | Painel atrás da cabine | `ZatanRepair/SFX_ZatanPanel_Spark_01..04` | | |
| Clique | Carrossel | `Carousel/SFX_Carousel_Click` | | |
| Engrenagens rangendo | Carrossel | `Carousel/SFX_Carousel_Creak` | | |
| Motor forçando | Carrossel | `Carousel/SFX_Carousel_Strain` | | |
| Correia estourando | Carrossel | `Carousel/SFX_Carousel_Snap` | | |
| Zumbido do motor (loop) | Carrossel | `Carousel/SFX_Carousel_Hum_Loop` | | |
| Gasolina no galão (3) | Andar com o galão | `Generator/SFX_GasCan_Slosh_01..03` | | |
| Despejar (loop) | Gerador | `Generator/SFX_Gas_Pour_Loop` | | |
| Derramar (loop) | Gerador, errar o funil | `Generator/SFX_Gas_Spill_Loop` | | |
| Tampa | Gerador | `Generator/SFX_Generator_Click` | | |
| Puxar a corda | Gerador | `Generator/SFX_Generator_CordPull` | | |
| Motor engasgando (2) | Gerador, puxadas que falham | `Generator/SFX_Generator_Sputter_01..02` | | |
| Motor funcionando (loop) | Reserva: a cena usa o `SFX_Roteiro_Generator_Running_Loop` | `Generator/SFX_Generator_Run_Loop` | | |
| Parafuso caindo | Caixa de fusíveis | `FuseBox/SFX_FuseBox_ScrewDrop` | | |
| Tampa de metal | Caixa de fusíveis | `FuseBox/SFX_FuseBox_Cover` | | |
| Fusível (tirar e pôr) | Caixa de fusíveis | `FuseBox/SFX_FuseBox_Fuse` | | |
| Alavanca | Caixa de fusíveis, energia volta | `FuseBox/SFX_FuseBox_Lever` | | |

## 4. Jogador e mundo (`Assets/Audio/SFX/`)

| Som | Quando toca | Arquivo | Nota | Ação |
|---|---|---|---|---|
| Passos no cascalho (8) | Andar e correr (`PlayerFoley`) | `Player/SFX_Player_Step_Gravel_01..08` | | |
| Lanterna | Ligar e desligar (mesmo clique, tom diferente) | `Player/SFX_Flashlight_Click` | | |
| Pegar item (4) | Pilhas, fusível, galão, lanterna, cabeça do cavalo | `Player/SFX_Pickup_01..04` | | |
| Porta do trailer abrindo | `MachineSwitchSound` no `PF_caravan` | `Trailer/SFX_TrailerDoor_Open` | | |
| Porta do trailer fechando | `MachineSwitchSound` no `PF_caravan` | `Trailer/SFX_TrailerDoor_Close` | | |

## 5. Não mudou

- Músicas: ambiente (`DEMO_SOUNDSCAPE_Zatan`), trailer (`safe_room_demo`), perseguição (`TRACK02...`), carrossel (`Zatan-carousel_temp`), créditos (`MUS_Roteiro_Credits_PH`) e drone da roda (`SFX_Roteiro_Tension_Loop_PH`).
- Sem uso no jogo: `SFX_Roteiro_RodaGigante_Repair_Loop_PH`, `SFX_Roteiro_RodaGigante_Start_PH`, `SFX_Roteiro_ZatanEscape_PH`.
- Os passos do jogador são sempre de cascalho, mesmo dentro do trailer.

## 6. Fontes

Todos os sons vêm do [Freesound](https://freesound.org), com licença **CC0** (domínio público): não precisam de crédito, mas a lista fica aqui para referência. Os arquivos foram feitos a partir das prévias de alta qualidade (MP3), cortados, misturados e normalizados.

| Som original | Autor | Licença |
|---|---|---|
| [12 Volt Solenoid Clicks](https://freesound.org/people/shdwtek/sounds/679204/) | shdwtek | CC0 |
| [Air Brake Applied](https://freesound.org/people/chungus43A/sounds/705390/) | chungus43A | CC0 |
| [belt snap](https://freesound.org/people/clothespeg/sounds/621939/) | clothespeg | CC0 |
| [Big robot footstep 002](https://freesound.org/people/AudioPapkin/sounds/813053/) | AudioPapkin | CC0 |
| [Big robot footstep 003](https://freesound.org/people/AudioPapkin/sounds/813054/) | AudioPapkin | CC0 |
| [Big robot footstep 004](https://freesound.org/people/AudioPapkin/sounds/813055/) | AudioPapkin | CC0 |
| [Big robot footstep 005](https://freesound.org/people/AudioPapkin/sounds/815039/) | AudioPapkin | CC0 |
| [Big robot footstep](https://freesound.org/people/AudioPapkin/sounds/813056/) | AudioPapkin | CC0 |
| [Boat Ramp.wav](https://freesound.org/people/Rmutt/sounds/147266/) | Rmutt | CC0 |
| [Clip In.wav](https://freesound.org/people/slappy13/sounds/151778/) | slappy13 | CC0 |
| [Clock-Egg timer.wav](https://freesound.org/people/knufds/sounds/490327/) | knufds | CC0 |
| [Clockwork motor](https://freesound.org/people/robindouglasjohnson/sounds/698991/) | robindouglasjohnson | CC0 |
| [closing plastic cap](https://freesound.org/people/gecop/sounds/522121/) | gecop | CC0 |
| [Creepy Whispering](https://freesound.org/people/RaspberryTickle/sounds/234044/) | RaspberryTickle | CC0 |
| [Cutting Power](https://freesound.org/people/swag1773/sounds/636578/) | swag1773 | CC0 |
| [Dark Whoosh](https://freesound.org/people/The-Sacha-Rush/sounds/657795/) | The-Sacha-Rush | CC0 |
| [door metal knock heavy bang from other side.flac](https://freesound.org/people/kyles/sounds/637480/) | kyles | CC0 |
| [door wood int trailer rv deadbolt flimsy hollow roomy slow thump various.wav](https://freesound.org/people/kyles/sounds/452597/) | kyles | CC0 |
| [Duct tape.wav](https://freesound.org/people/Marissrar/sounds/366893/) | Marissrar | CC0 |
| [Electric Motor 2](https://freesound.org/people/onursamli/sounds/846905/) | onursamli | CC0 |
| [Electric Motor Struggeling](https://freesound.org/people/Janosch-JR/sounds/479417/) | Janosch-JR | CC0 |
| [Electricity, Arcs, Sparks, long](https://freesound.org/people/julianmateo_/sounds/522690/) | julianmateo_ | CC0 |
| [Elevator Sounds - Elevator Stopping.wav](https://freesound.org/people/jchamusantana/sounds/634357/) | jchamusantana | CC0 |
| [Flashlight Switch Click](https://freesound.org/people/Ralph0o7/sounds/690300/) | Ralph0o7 | CC0 |
| [FOL-gas-leak-01.wav](https://freesound.org/people/henrikcederblad/sounds/615668/) | henrikcederblad | CC0 |
| [Foley_Object_Fridge_Motor_Humming_Loop_Mono_NTG4.wav](https://freesound.org/people/Nox_Sound/sounds/556369/) | Nox_Sound | CC0 |
| [Footsteps_Mountain_Boots_Gravel_Mono.wav](https://freesound.org/people/Nox_Sound/sounds/556002/) | Nox_Sound | CC0 |
| [funnel filling jerry-can](https://freesound.org/people/gecop/sounds/522118/) | gecop | CC0 |
| [Fuse Box (klick)](https://freesound.org/people/Syndicate12/sounds/141581/) | Syndicate12 | CC0 |
| [G27-01-Ratchet Crank.wav](https://freesound.org/people/craigsmith/sounds/438347/) | craigsmith | CC0 |
| [gate big rusty metal door garage open heavy creak rattle.flac](https://freesound.org/people/kyles/sounds/637583/) | kyles | CC0 |
| [Generic Game Pick Up](https://freesound.org/people/IENBA/sounds/831642/) | IENBA | CC0 |
| [Horror / Zombie - Biting on bones](https://freesound.org/people/Breviceps/sounds/445987/) | Breviceps | CC0 |
| [Impact 1b](https://freesound.org/people/MathewHenry/sounds/636880/) | MathewHenry | CC0 |
| [Jumpscare](https://freesound.org/people/IENBA/sounds/830244/) | IENBA | CC0 |
| [Machinery Inside Top Of Windmill (Veendermolen in Roelofarendsveen).aif](https://freesound.org/people/RutgerMuller/sounds/148292/) | RutgerMuller | CC0 |
| [Metal Trash Bin](https://freesound.org/people/cb9382/sounds/873900/) | cb9382 | CC0 |
| [Metal Trashcan Opening](https://freesound.org/people/PhilllChabbb/sounds/268860/) | PhilllChabbb | CC0 |
| [metal_wrenches_general_handling_foley.wav](https://freesound.org/people/cjolley/sounds/488592/) | cjolley | CC0 |
| [MOTRComb_Diesel Generator 03_KVV AUDIO_FREE](https://freesound.org/people/KVV_Audio/sounds/748274/) | KVV_Audio | CC0 |
| [music box - wind up 05.wav](https://freesound.org/people/Anthousai/sounds/336610/) | Anthousai | CC0 |
| [OBJHsehld_trashcan_knocked_Fallen](https://freesound.org/people/owhorvath/sounds/816400/) | owhorvath | CC0 |
| [Old-Timey Saloon Sign](https://freesound.org/people/SoundsCoolio/sounds/820118/) | SoundsCoolio | CC0 |
| [Paint can shake](https://freesound.org/people/elliott.klein/sounds/194914/) | elliott.klein | CC0 |
| [plastic gas container pour gas or water on ground wet sloppy.wav](https://freesound.org/people/kyles/sounds/452656/) | kyles | CC0 |
| [Plucked Horror Stinger](https://freesound.org/people/zombieallen/sounds/725225/) | zombieallen | CC0 |
| [power cut](https://freesound.org/people/ninjatrappeur/sounds/400194/) | ninjatrappeur | CC0 |
| [pull-recoil.wav](https://freesound.org/people/gthall/sounds/222809/) | gthall | CC0 |
| [R03-12-Antique Auto Sputter and Backfire.wav](https://freesound.org/people/craigsmith/sounds/481663/) | craigsmith | CC0 |
| [Radio Noise Loop](https://freesound.org/people/loganzsound/sounds/875945/) | loganzsound | CC0 |
| [Rag on glass.wav](https://freesound.org/people/mareyeslo/sounds/609191/) | mareyeslo | CC0 |
| [Riffle Card Shuffle](https://freesound.org/people/Kodack/sounds/256508/) | Kodack | CC0 |
| [Robot Footsteps](https://freesound.org/people/IENBA/sounds/611355/) | IENBA | CC0 |
| [S012_Engine_Backfire_Mono.wav](https://freesound.org/people/P%C3%B3l/sounds/385935/) | Pól | CC0 |
| [S41-15a Mechanical noise of a merry-go-round; carousel — take 2; wooden machine.wav](https://freesound.org/people/craigsmith/sounds/675548/) | craigsmith | CC0 |
| [sideband AM voice.wav](https://freesound.org/people/parabolix/sounds/362448/) | parabolix | CC0 |
| [Slips air release v1](https://freesound.org/people/mcpable/sounds/131934/) | mcpable | CC0 |
| [Sounds For Earthquakes - Radiator Metal Rumbling.wav](https://freesound.org/people/RutgerMuller/sounds/51097/) | RutgerMuller | CC0 |
| [spark2.ogg](https://freesound.org/people/Kneeling/sounds/643861/) | Kneeling | CC0 |
| [static typical 590 kHz.wav](https://freesound.org/people/cognito%20perceptu/sounds/117142/) | cognito perceptu | CC0 |
| [Steel Spring Bear Trap](https://freesound.org/people/fractionalist/sounds/644245/) | fractionalist | CC0 |
| [Stingers and Stabs 004](https://freesound.org/people/OverlookHotelRecords/sounds/840810/) | OverlookHotelRecords | CC0 |
| [switch big breaker metal click on, off.flac](https://freesound.org/people/kyles/sounds/451933/) | kyles | CC0 |
| [when the toys go winding down.wav](https://freesound.org/people/N.A.N.I./sounds/127967/) | N.A.N.I. | CC0 |
| [Wind-up2.aif](https://freesound.org/people/timgormly/sounds/162806/) | timgormly | CC0 |
