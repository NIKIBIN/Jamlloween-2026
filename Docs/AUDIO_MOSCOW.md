# Áudio: prioridades da jam (MoSCoW)

Os 90 sons do [ASSET_LEDGER](ASSET_LEDGER.md) organizados pelo que realmente importa para entregar a jam. A pergunta para cada som foi: **sem ele, o jogo deixa de funcionar ou de dar medo?**

- **Must**: sem isso o jogador não entende o perigo, não recebe resposta das ações ou o susto não funciona. Fazer primeiro.
- **Should**: deixa o jogo bem melhor, mas ele funciona sem. Fazer depois dos Must.
- **Could**: polimento. Só se sobrar tempo.
- **Won't (nesta jam)**: fica para depois, porque o código ainda não tem como tocar esses sons.

Status vem do ledger: **Placeholder** (já toca, é temporário) ou **Faltando** (não existe som).

## Estratégia para caber na jam

1. **Um clique compartilhado.** Um conjunto de 3 variações de "encaixe" (metal e plástico) cobre os encaixes do rádio, do carrossel, do painel de Zatan e da caixa de fusíveis. São cerca de 12 sons da lista resolvidos por um só asset.
2. **Uma superfície de passo.** Começar com um único passo genérico do jogador. Passos por superfície (grama, serragem, floresta, madeira) ficam para o Could.
3. **Variar por código, não por arquivo.** Ligar e desligar a lanterna podem ser o mesmo clique com pitch diferente, e o passo correndo pode ser o passo de andar mais alto e mais agudo.
4. **Fundir sons que tocam juntos.** "Captura" e "Agarrão" disparam no mesmo instante: um som só basta.

## Must (16)

| # | Som | Por que é fundamental | Cobre do ledger | Status |
|---|---|---|---|---|
| 1 | **Ambiente noturno (novo)** | Hoje o jogo fica em silêncio total quando Zatan está longe. Sem ambiente não há clima de terror. Não está no ledger. | Novo: vento, estrutura do parque rangendo, música distante | Faltando |
| 2 | Passos de Zatan (4 variações) | É como o jogador sabe onde ele está sem vê-lo, já que a névoa limita a visão a 5 m. | Zatan: Passo | Placeholder |
| 3 | Loop de perseguição | Diz ao jogador "corra agora". | Zatan: Perseguição | Placeholder |
| 4 | Alerta | Marca o instante em que Zatan avista o jogador e a pausa de 0,5 s antes de correr. | Zatan: Alerta | Placeholder |
| 5 | Agarrão | Abre o jump scare, o momento-chave do jogo. | Zatan: Agarrão e Captura (fundidos) | Placeholder |
| 6 | Mordida | Fecha o jump scare. | Zatan: Mordida | Placeholder |
| 7 | Passos do jogador (4 variações, genérico) | Sem eles o jogador não sente o próprio movimento nem entende que faz barulho para Zatan. | Jogador: Passo na midway (usado em todas as superfícies) e Passo correndo (com pitch) | Faltando |
| 8 | Clique da lanterna | A lanterna é a principal ferramenta do jogador. | Jogador: Lanterna ligando e desligando | Faltando |
| 9 | Clique de encaixe compartilhado (3 variações) | É a resposta básica de quase toda ação nos minigames. | Rádio, Carrossel e Painel de Zatan: Clique de encaixe; Fusíveis: Encaixar fusível novo e Tirar fusível queimado | Placeholder (Faltando nos fusíveis) |
| 10 | Estática do rádio | É a própria mecânica do minigame: o jogador sintoniza pelo som. | Rádio: Estática | Placeholder |
| 11 | Energia voltando | A caixa de fusíveis não tem nenhum som. Este é o mínimo para o conserto ter recompensa. | Fusíveis: Energia voltando | Faltando |
| 12 | Cabeça virada (jump scare do painel) | O minigame do painel termina num jump scare, que hoje é mudo. | Painel de Zatan: Cabeça virada | Faltando |
| 13 | Puxão de corda | A ação central de ligar o gerador; sem som, a puxada não tem peso. | Gerador: Puxão de corda | Faltando (sem gatilho) |
| 14 | Gerador falhando | Diz ao jogador que precisa tentar de novo, e o barulho atrai tensão. | Gerador: Gerador falhando | Faltando (sem gatilho) |
| 15 | Gerador ligado | A recompensa do conserto, e o loop vira um ponto de referência no mapa. | Gerador: Gerador ligado | Faltando (sem gatilho) |
| 16 | Lixeira tombando | É o aviso de que o esconderijo falhou e de que Zatan está em cima do jogador. | Esconderijos: Lixeira tombando | Placeholder |

Os itens 13 a 15 dependem do minigame do gerador, que ainda não existe no código.

## Should (19)

| Som | Por que vale a pena | Status |
|---|---|---|
| Zatan: Mecanismo (loop) | Presença de Zatan quando ele patrulha perto, mesmo parado. | Placeholder |
| Zatan: Tique próximo | Aviso de que ele está a menos de 10 m. | Placeholder |
| Zatan: Chacoalhão | Preenche o segundo do meio do jump scare. | Placeholder |
| Zatan: Corda acabando | Avisa que o jogador escapou. | Placeholder |
| Zatan: Servo | Avisa que ele ouviu algo e vai investigar. | Placeholder |
| Zatan: Passo pesado | Tensão alta mais audível; dá para começar com o passo normal mais grave. | Placeholder |
| Jogador: Respiração de pânico | Reforça a perseguição do lado do jogador. | Faltando |
| Jogador: Porta abrindo e fechando | Portas são frequentes e hoje são mudas. | Faltando |
| Jogador: Pegar item | Confirma que o item foi pego. | Faltando |
| Rádio: Voz do patrão | É a recompensa narrativa do rádio. | Placeholder |
| Progressão: Falas do rádio (8) | Hoje só legenda; gravar as 8 falas é caro para uma jam, então a legenda segura. | Faltando |
| Fusíveis: Alavanca descendo | A ação final do minigame. | Faltando |
| Carrossel: Música | O carrossel girando é a recompensa e vira um ponto de referência no mapa. | Placeholder |
| Gerador: Colocar gasolina | Confirma o abastecimento antes de puxar a corda. | Faltando (sem gatilho) |
| Esconderijos: Entrar na lixeira | Confirma que o jogador se escondeu. | Placeholder |
| Esconderijos: Sair da lixeira | Confirma que o jogador saiu. | Placeholder |
| Painel de Zatan: Respiração do compressor | É o coração do minigame e acelera com a manivela. | Placeholder |
| Painel de Zatan: Clunk da chave | A ação que liga o compressor. | Placeholder |
| Painel de Zatan: Suspiro final | Prepara o silêncio antes do jump scare. | Placeholder |

## Could (37)

**Zatan**
- Murmúrio (2 variações) · Placeholder
- Cartas ao sumir · Placeholder
- Caixa de música ao reaparecer · Placeholder
- Passo correndo · Faltando
- Engrenagem travando · Faltando
- Cabeça girando · Faltando

**Jogador**
- Passos por superfície: grama, serragem, floresta e madeira · Faltando
- Passo correndo com som próprio · Faltando
- Respiração cansada · Faltando
- Susto · Faltando
- Batimento · Faltando
- Grito abafado · Faltando
- Pegar a lanterna · Faltando
- Bloqueado · Faltando
- Prancheta subindo e descendo · Faltando
- Tarefa riscada · Faltando
- Entrar e sair do minigame · Faltando

**Rádio**
- Virar o rádio · Faltando
- Girar o dial · Faltando
- Tampa saindo · Faltando

**Caixa de fusíveis**
- Parafuso girando e parafuso solto · Faltando
- Tampa saindo · Faltando
- Alavanca voltando · Faltando

**Carrossel**
- Rangido ao tirar o objeto · Placeholder
- Zumbido do motor · Placeholder
- Cabeça girando · Faltando
- Tensor girando · Faltando
- Carrossel girando · Faltando

**Painel de Zatan**
- Solenoide · Placeholder
- Zumbido · Placeholder
- Rangido da manivela · Placeholder
- Vazamento · Placeholder
- Estopa esfregando · Placeholder
- Tampa saindo · Faltando

**Gerador**
- Líquido no galão, andando com a gasolina · Faltando (sem gatilho)

**Esconderijos**
- Tampa tremendo com Zatan perto · Faltando (sem gatilho)

**Progressão**
- Sons das etapas · Faltando

## Won't nesta jam (6)

O código ainda não tem como tocar estes sons, e as mecânicas deles não estão planejadas para a jam, então gravá-los agora não muda o jogo. Os sons do gerador também estão sem gatilho, mas ficaram em Must, Should e Could porque o minigame dele está planejado.

- Zatan: Rangido de junta
- Zatan: Frases do cartomante
- Zatan: Carta virada
- Zatan: Arrombar porta
- Zatan: Vasculhar esconderijo
- Zatan: Risada

## Resumo

| Prioridade | Itens | Já têm placeholder | Faltando |
|---|---|---|---|
| Must | 16 assets (cobrem cerca de 24 sons do ledger) | 8 | 8 |
| Should | 19 | 13 | 6 |
| Could | 37 | 10 | 27 |
| Won't | 6 | 0 | 6 |

Se o tempo for curto, os 8 Must que estão faltando são os primeiros a criar: **ambiente noturno, passos do jogador, clique da lanterna, energia voltando nos fusíveis, o estalo da cabeça virada no painel de Zatan e os três sons do gerador (puxão de corda, falhando e ligado).** Os do gerador só tocam quando o minigame dele existir. Os outros 8 Must já tocam como placeholder, e só precisam do som final.

Motor forçando e correia estourando, do carrossel, saíram da lista.
