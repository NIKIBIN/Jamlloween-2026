# Minigames na Zatan_Test

Os minigames da `RepairMinigames` foram copiados (não movidos) para a `Zatan_Test`. A `RepairMinigames` não mudou.

| Minigame | Onde está | Item que pede |
|---|---|---|
| Rádio (`RadioMinigame` + mesa `Mesa_Radio`) | Dentro do `PF_caravan`, mesa no piso, encostada na parede do fundo. A porta do trailer abre com E. | Pilhas, que aparecem em `LocalPilhas_Radio`, em cima da mesa |
| Quadro de fusíveis (`FuseBoxMinigame`) | Em `PF_power_panel_Carrossel`, o painel ao lado do carrossel. Concluir liga o carrossel | Um fusível (`Fusivel`, em `LocalCabeca_Carrossel`), pela progressão |
| Gerador (`GeneratorFuelMinigame`) | Filho do `PF_generator_trailer`. Prefab em `Prefabs/Minigames/GeneratorFuelMinigame.prefab`, montado por `Jamlloween > Build Generator Fuel Minigame` | O galão (`PF_jerry_can`, `GasCan`), carregado pelo `GasCanCarrier` do jogador |

- **Gerador:** tudo na face da frente, com uma câmera só, frontal e levemente pela esquerda. Passos:
  1. Clicar na tampa: ela desenrosca sozinha e vai para o gancho.
  2. O galão passa a seguir o cursor, com a ponta do bico no cursor. Segurar o clique em qualquer lugar inclina o galão, e a gasolina cai na vertical, saindo do bico; soltar endireita.
     - Com o bico sobre a boca (há um ímã leve), o tanque enche.
     - Fora da boca, a gasolina cai no chão, com respingo, som e `NoiseSystem`.
     - Despejando mais rápido do que o bocal engole, a espuma sobe; na borda, transborda, também com barulho.
  3. Com o tanque cheio, o galão volta para a lateral. Clicar na tampa fecha o bocal.
  4. Puxar a corda para a direita até o fim, sem limite de tempo. A 1ª puxada tosse, solta fumaça e pisca a lâmpada; a 2ª liga, com a lâmpada acesa.
  O medidor tem vazio à esquerda e cheio à direita. O galão só é exigido enquanto o tanque não está cheio. Montado por `Jamlloween > Build Generator Fuel Minigame`; rodar de novo refaz só as peças.
- **Roda-gigante (final, provisório):** prefab `Prefabs/FerrisWheelRide.prefab`, com a base e o anel do kit, o motor e o carrinho do jogador. Montado por `Jamlloween > Ferris Wheel > Build Prefab`. `Place In Open Scene` troca a roda solta do kit pela do prefab, na mesma posição. Numa cena nova, basta arrastar o prefab.
  - **Estados (`FerrisWheelRide`):** `Off` (parada e apagada) → conserto do motor → `Running` (dá uma volta e para com o carrinho do jogador embaixo) → `Waiting` (porta aberta, "[E] Entrar no carrinho") → embarque → `Running` (sobe até o topo) → `Off`.
  - **Motor (`FerrisWheelMotorMinigame`):** provisório, só um interruptor. Clicar conclui. O minigame de verdade troca só o `Tick`.
  - **Carrinho (`FerrisWheelSeat`, no `ferris_wheel_ring_car_7`):** ao embarcar, o movimento do jogador desliga e não volta. O mouse continua olhando em volta, e a porta fecha.
  - **Eventos para a progressão:** `onRepaired`, `onWaiting`, `onBoarded`, `onReachedTop` e `onStateChanged`. A queda de energia no topo e o susto ainda não estão ligados.
  - O anel do prefab não tem o `Machine` nem o `Animation` do kit: o `FerrisWheelRide` gira a roda. Com eles, o E do `LevelWalker` faria a roda girar sozinha.
  - Ainda não tem `StepInteraction`: o motor funciona em qualquer etapa.
  - **Testar isolado:** cena `Scenes/FerrisWheel_Test`, com a roda, o `Player`, o HUD, o `WorldLighting` e o `MinigameFlashlightLink`. O jogador começa com a lanterna, ao lado do motor.

## Jogador (`Prefabs/Player.prefab`)

Prefab Variant do `Player_Local` do PSX Carnival Kit (movimento, câmera e `Inspect`), com o que o jogo adiciona: `PlayerInteractor`, `LevelWalkerZatanTarget`, `GasCanCarrier` e a lanterna (`Flashlight`, com `PlayerFlashlight`). Foi tirado do jogador da `Zatan_Test`, que ainda não usa o prefab.

Ao colocar numa cena nova:
1. Ligue o `promptLabel` do `PlayerInteractor` ao texto do HUD (o HUD ainda não é prefab).
2. Para os minigames, adicione um `MinigameFlashlightLink` apontando para a lanterna.
3. A lanterna começa sem dono (`startOwned` desligado). Para testes, ligue `startOwned` na instância.
- **Galão:** E pega. Carregando, anda a 80% e não corre (`GasCanCarrier`), a lanterna segue normal e a gasolina balança a cada passo. G larga (configurável); ele fica no chão e pode ser pego de novo. Ao ligar o gerador o galão fica vazio no chão, perto do gerador.
- **Cabeça do cavalo (etapas 07 a 09):**
  - Ao entrar perto do carrossel (`Trigger_CavaloSemCabeca`) ou examinar o cavalo, aparece a legenda "Que estranho... tá faltando a cabeça desse cavalo. Preciso achar e colocar de volta."
  - A cabeça (`HorseHead`) fica no chão, atrás do carrossel. E pega, e ela fica visível na mão.
  - Com a cabeça na mão, E no cavalo (`HorseNeckSocket` no `Horse_Headless`) encaixa. Só depois disso vem a etapa do quadro de fusíveis.
- **Realce:** `HighlightTint` é o realce compartilhado: usa instância de material, porque MaterialPropertyBlock não aparece com o GPU Resident Drawer. O gerador e os fusíveis usam a cor `HighlightTint.Contrast` (ciano); o Zatan mantém o amarelo dele.
- **Controlador do carrossel:** `CarouselRepair` liga o carrossel quando o `FuseBoxMinigame` é concluído. O `Machine` do `PF_carousel` precisa de `PF_power_panel_Carrossel` e de `CarouselRepair/RepairedGate`, então ele só gira depois do conserto. O cavalo (`Horse_Headless`) ganha a cabeça nas etapas 07 a 09.
- **Interação:** o `Player` ganhou o `PlayerInteractor` (E nos minigames), e a cena ganhou o `HUD` (mira e texto de interação) da `RepairMinigames`. E continua abrindo portas e pegando itens pelo `LevelWalker`.
- **Pego durante um minigame:** o evento `onFinished` do `ZatanCatchSequence` chama `Exit()` dos minigames (rádio, caixa de fusíveis e gerador). Assim, depois do jump scare, o jogador volta com câmera, cursor e controles normais.
- **Sair:** Esc, Q ou qualquer tecla de movimento sai do minigame, mantendo o progresso.

## Testar sem Zatan

Na `Zatan_Test`, **F10** (ou o botão no painel de teste) desliga Zatan inteiro: IA, passos, sons, luz dos olhos e captura. Apertar de novo traz Zatan de volta no `Zatan_Spawn`, patrulhando. Durante o jump scare a tecla é ignorada.
