# Asset Ledger: sons de Zatan

Zatan é um cartomante mecânico amaldiçoado: corda, engrenagens, madeira velha, latão, cartas de tarô e caixa de música.

Os placeholders foram sintetizados por código e estão em `Assets/Zatan/Audio/Placeholder/`, com o prefixo `SFX_Zatan_` e o sufixo `_PH.wav`. Todos são mono e tocam em 3D. Para trocar um som, substitua o clipe no Inspector de `ZatanAudio` ou `ZatanFootsteps`, no objeto `Zatan`.

O volume segue a tensão (0,2 com tensão 0 e 1,0 com tensão 100), exceto onde a tabela indica outra regra.

## Ligados na cena (placeholder)

| Som | Quando toca | Tipo | Som final desejado | Arquivo |
|---|---|---|---|---|
| Passo | Andando, tensão abaixo de 60 | One-shot (4 variações) | Pé de madeira e latão, com rangido de junta | `Step_01` a `Step_04` |
| Passo pesado | Andando, tensão de 60 para cima | One-shot (2 variações) | Pisada pesada, com peças soltas chacoalhando | `StepHeavy_01`, `StepHeavy_02` |
| Mecanismo | Patrol, Investigate e Search | Loop | Tique-taque de corda com zumbido de engrenagens | `Clockwork_Loop` |
| Perseguição | Chase | Loop | Fole ofegante e engrenagens em disparada | `Chase_Loop` |
| Tique próximo | Até 10 m do jogador; volume pela distância, não pela tensão | Loop | Tique seco de relógio de bolso | `NearTick_Loop` |
| Alerta | Entra em Chase; volume fixo em 1 | One-shot | Metais dissonantes com pancada metálica | `Alert_Sting` |
| Servo | Entra em Investigate | One-shot | Pescoço mecânico girando e travando | `Investigate_Servo` |
| Corda acabando | Entra em Search | One-shot | Mola perdendo a força, com tiques cada vez mais lentos | `Lost_WindDown` |
| Captura | Entra em Caught; volume fixo em 1 | One-shot | Golpe pesado com guincho de metal | `Caught_Sting` |
| Cartas | Some (Vanished) | One-shot | Baralho de tarô embaralhado, terminando em um corte seco | `Vanish_Cards` |
| Caixa de música | Reaparece | One-shot | Melodia menor, desafinada e desacelerando | `Reappear_MusicBox` |
| Murmúrio | Patrol e Search, a cada 15–35 s | One-shot (2 variações) | Voz rachada de autômato murmurando previsões | `Voice_Mutter_01`, `Voice_Mutter_02` |
| Agarrão | Início do jump scare; 2D, volume fixo | One-shot | Mãos de metal batendo nos ombros, com pancada grave | `Catch_Grab` |
| Chacoalhão | Durante o chacoalhão; 2D | Loop | Peças soltas, engrenagens e madeira batendo | `Catch_Shake_Loop` |
| Mordida | Impacto da mordida; 2D | One-shot | Dentes de latão estalando, com mastigada | `Catch_Bite` |

Os três sons do jump scare tocam em `ZatanCatchSequence`, por duas fontes 2D no filho `Zatan/CatchAudio`.

## Futuros (sem gatilho ainda, sem arquivo)

| Som | Quando tocaria | Tipo | Som final desejado |
|---|---|---|---|
| Frases do cartomante | Eventos roteirizados, jogador à vista | One-shot | Falas gravadas, com voz de autômato de parque |
| Carta virada | Revelação de carta em evento | One-shot | Carta batendo na mesa |
| Arrombar porta | Quando Zatan tratar portas trancadas | Loop + one-shot | Pancadas de madeira e latão, depois a porta cedendo |
| Vasculhar esconderijo | Quando houver esconderijos | One-shot | Dedos de metal abrindo e tateando |
| Risada | Jogador escapa ou evento roteirizado | One-shot | Gargalhada mecânica que trava e repete |
