# Minigames na Zatan_Test

Os minigames da `RepairMinigames` foram copiados (não movidos) para a `Zatan_Test`. A `RepairMinigames` não mudou.

| Minigame | Onde está | Item que pede |
|---|---|---|
| Rádio (`RadioMinigame` + mesa `Mesa_Radio`) | Dentro do `PF_caravan`, mesa no piso, encostada na parede do fundo. A porta do trailer abre com E. | Pilhas, que aparecem em `LocalPilhas_Radio`, em cima da mesa |
| Quadro de fusíveis (`FuseBoxMinigame`) | Em `PF_power_panel_Carrossel`, o painel ao lado do carrossel. Concluir liga o carrossel | Um fusível (`Fusivel`, em `LocalCabeca_Carrossel`), pela progressão |
| Gerador (`GeneratorFuelMinigame`) | Filho do `PF_generator_trailer`. Prefab em `Prefabs/Minigames/GeneratorFuelMinigame.prefab`, montado por `Jamlloween > Build Generator Fuel Minigame` | O galão (`PF_jerry_can`, `GasCan`), carregado pelo `GasCanCarrier` do jogador |

- **Gerador:** tudo na face da frente, com uma câmera só, frontal e levemente pela esquerda. São 4 passos:
  1. Abrir o bocal lateral: segurar a tampa e girar em círculo, anti-horário. No sentido errado a tampa cede um pouco e estala.
  2. Despejar: segurar o clique no galão, que entra pela esquerda da tela. Quanto mais tempo segurando, mais ele inclina e mais rápido sai; soltar volta o galão. Despejando mais rápido do que o bocal engole, a espuma sobe na boca. Quando ela chega na borda, transborda: respingo, som e `NoiseSystem`.
  3. Fechar a tampa, sentido horário.
  4. Puxar a corda para a direita até o fim, sem limite de tempo. A 1ª puxada tosse e solta fumaça pela grade; a 2ª tosse mais e a lâmpada piloto pisca; a 3ª liga, com a lâmpada acesa.
  O medidor tem vazio à esquerda e cheio à direita. O galão só é exigido enquanto o tanque não está cheio. Montado por `Jamlloween > Build Generator Fuel Minigame`; rodar de novo refaz só as peças.
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
