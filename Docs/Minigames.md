# Minigames na Zatan_Test

Os minigames da `RepairMinigames` foram copiados (não movidos) para a `Zatan_Test`. A `RepairMinigames` não mudou.

| Minigame | Onde está | Item que pede |
|---|---|---|
| Rádio (`RadioMinigame` + mesa `Mesa_Radio`) | Dentro do `PF_caravan`, mesa no piso, encostada na parede do fundo. A porta do trailer abre com E. | Pilhas, que aparecem em `LocalPilhas_Radio`, em cima da mesa |
| Quadro de fusíveis (`FuseBoxMinigame`) | Em `PF_power_panel_Carrossel`, o painel ao lado do carrossel. Concluir liga o carrossel | Um fusível (`Fusivel`, em `LocalCabeca_Carrossel`), pela progressão |
| Gerador (`GeneratorFuelMinigame`) | Filho do `PF_generator_trailer`. Prefab em `Prefabs/Minigames/GeneratorFuelMinigame.prefab`, montado por `Jamlloween > Build Generator Fuel Minigame` | O galão (`PF_jerry_can`, `GasCan`), carregado pelo `GasCanCarrier` do jogador |

- **Gerador:** abrir a tampa (girar em círculo, anti-horário), arrastar o funil até a boca, segurar o mouse e arrastar para baixo para inclinar o galão (passou de `overflowLimit` transborda: respingo, som e `NoiseSystem`), fechar a tampa (horário) e puxar a corda rápido para trás (1ª e 2ª engasgam, a 3ª liga). O medidor no gerador mostra o nível. Sem galão na mão o minigame não abre.
- **Galão:** E pega. Carregando, anda a 80% e não corre (`GasCanCarrier`), a lanterna segue normal e a gasolina balança a cada passo. G larga (configurável); ele fica no chão e pode ser pego de novo. Ao ligar o gerador o galão fica vazio no chão, perto do gerador.
- **Controlador do carrossel:** `CarouselRepair` liga o carrossel quando o `FuseBoxMinigame` é concluído. O `Machine` do `PF_carousel` precisa de `PF_power_panel_Carrossel` e de `CarouselRepair/RepairedGate`, então ele só gira depois do conserto. O cavalo (`Horse_Headless`) continua sem cabeça.
- **Interação:** o `Player` ganhou o `PlayerInteractor` (E nos minigames), e a cena ganhou o `HUD` (mira e texto de interação) da `RepairMinigames`. E continua abrindo portas e pegando itens pelo `LevelWalker`.
- **Pego durante um minigame:** o evento `onFinished` do `ZatanCatchSequence` chama `Exit()` dos minigames (rádio, caixa de fusíveis e gerador). Assim, depois do jump scare, o jogador volta com câmera, cursor e controles normais.
- **Sair:** Esc, Q ou qualquer tecla de movimento sai do minigame, mantendo o progresso.

## Testar sem Zatan

Na `Zatan_Test`, **F10** (ou o botão no painel de teste) desliga Zatan inteiro: IA, passos, sons, luz dos olhos e captura. Apertar de novo traz Zatan de volta no `Zatan_Spawn`, patrulhando. Durante o jump scare a tecla é ignorada.
