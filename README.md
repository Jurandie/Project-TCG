# Project TCG

Projeto pessoal de um jogo de cartas (TCG) inspirado em fantasia e RPG de mesa, com duelos por turnos, rolagem de dados e cartas com evolucao. O foco e construir a experiencia completa do combate, do baralho ao campo de batalha, de forma clara para jogadores e facil de expandir.

## Para quem este projeto foi pensado
- Jogadores: experiencia de duelo com cartas, dados e estrategia.
- Game design: base pronta para testar regras, balanceamento e novas cartas.
- Arte/UX: interface ja montada com areas do tabuleiro e cartas.
- Producao/gestao: visao clara do que ja esta implementado e do que falta.

## O que ja esta implementado
- Sistema de turnos com rolagem de d20 e controle de energia.
- Compra de cartas baseada na rolagem (com falha e sucesso critico).
- Baralho, mao e campo de batalha para jogador e inimigo.
- Cartas de monstro, magia e equipamento (com durabilidade).
- Magias com alvo e efeitos variados (cura, dano, buffs, debuffs).
- Status negativos e positivos (ex.: veneno, silencio, atordoamento, escudo).
- Evolucao de cartas ao combinar cartas iguais.
- Forma transcendente com ataque especial via rolagem.
- IA basica para o inimigo em partidas solo.
- UI com preview de carta e ficha de atributos por toque longo.

## Fluxo de jogo (resumo)
1. No inicio, o dado define atributos e vida do jogador e do inimigo.
2. A cada turno, rolagens liberam compras de cartas.
3. O jogador compra cartas, posiciona no campo e usa magias.
4. Ataques acontecem com base em rolagem e atributos.
5. O turno passa para o inimigo, que joga automaticamente.

## Como abrir o projeto
- Abra no Unity (versao 6000.2.7f2).
- Abra a cena principal em `Assets/RPGDGG.unity` (ou `Assets/Scenes/SampleScene.unity`).
- Clique em Play para testar.

## Onde encontrar as coisas
- `Assets/GameLogic`: regras do jogo (turnos, cartas, combate, efeitos).
- `Assets/Scenes`: cenas jogaveis.
- `Assets/CardSprites`, `Assets/UISprites`: arte e interface.
