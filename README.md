# Project TCG

Personal trading card game project inspired by fantasy themes and tabletop RPGs, with turn-based duels, dice rolls, and evolving cards. The goal is to build the full combat experience, from deck management to the battlefield, in a way that is clear for players and easy to expand.

## Who This Project Was Designed For
- Players: a duel experience with cards, dice, and strategy.
- Game design: a ready base for testing rules, balancing, and new cards.
- Art and UX: an interface that already includes board areas and card presentation.
- Production and management: a clear view of what is already implemented and what is still missing.

## What Is Already Implemented
- Turn system with d20 rolls and energy control.
- Card draw system based on dice outcomes, including critical failure and critical success.
- Deck, hand, and battlefield for both player and enemy.
- Monster, spell, and equipment cards with durability.
- Targeted spells with different effects such as healing, damage, buffs, and debuffs.
- Negative and positive status effects such as poison, silence, stun, and shield.
- Card evolution by combining matching cards.
- Transcendent form with a special attack triggered by rolling.
- Basic enemy AI for solo matches.
- UI with card preview and attribute sheet on long press.

## Gameplay Flow Summary
1. At the start, dice rolls define the player and enemy attributes and health.
2. Each turn, rolls determine whether cards can be drawn.
3. The player draws cards, places them on the field, and uses spells.
4. Attacks are resolved based on rolls and attributes.
5. The turn passes to the enemy, which plays automatically.

## How To Open the Project
- Open it in Unity version `6000.2.7f2`.
- Open the main scene in `Assets/RPGDGG.unity` or `Assets/Scenes/SampleScene.unity`.
- Press Play to test.

## Where To Find Things
- `Assets/GameLogic`: core game rules such as turns, cards, combat, and effects.
- `Assets/Scenes`: playable scenes.
- `Assets/CardSprites`, `Assets/UISprites`: art and interface assets.
