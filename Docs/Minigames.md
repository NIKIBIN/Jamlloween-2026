# Minigames na Zatan_Test

Os minigames da `RepairMinigames` foram copiados (não movidos) para a `Zatan_Test`. A `RepairMinigames` não mudou.

| Minigame | Onde está | Item que pede |
|---|---|---|
| Rádio (`RadioMinigame` + mesa `Mesa_Radio`) | Dentro do `PF_caravan`, mesa no piso, encostada na parede do fundo. A porta do trailer abre com E. | Pilhas, que aparecem em `LocalPilhas_Radio`, em cima da mesa |
| Caixa de fusíveis (`FuseBoxMinigame`) | Na frente do `PF_power_panel`, abaixo da alavanca, em escala 0,85 para caber no painel | — |
| Carrossel, fase 1 (`Horse_Headless`) | No `carousel_horse_3` do `PF_carousel`, que fica escondido | Cabeça do cavalo, em `LocalCabeca_Carrossel` |
| Carrossel, fase 2 (`CarouselMechanismHatch`) | Em `PF_power_panel_Carrossel`, um painel novo do kit ao lado do carrossel | — |

- **Controlador do carrossel:** `CarouselRepair` junta as duas fases. O `Machine` do `PF_carousel` agora precisa de `PF_power_panel_Carrossel` e de `CarouselRepair/RepairedGate`, então ele só gira depois do conserto.
- **Interação:** o `Player` ganhou o `PlayerInteractor` (E nos minigames), e a cena ganhou o `HUD` (mira e texto de interação) da `RepairMinigames`. E continua abrindo portas e pegando itens pelo `LevelWalker`.
- **Pego durante um minigame:** o evento `onFinished` do `ZatanCatchSequence` chama `Exit()` dos quatro minigames. Assim, depois do jump scare, o jogador volta com câmera, cursor e controles normais.
- **Sair:** Esc, Q ou qualquer tecla de movimento sai do minigame, mantendo o progresso.

## Testar sem Zatan

Na `Zatan_Test`, **F10** (ou o botão no painel de teste) desliga Zatan inteiro: IA, passos, sons, luz dos olhos e captura. Apertar de novo traz Zatan de volta no `Zatan_Spawn`, patrulhando. Durante o jump scare a tecla é ignorada.
