using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "Revivify", menuName = "Card Game/Spell Effects/Paladin/Revivify")]
    public class RevivifySpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 15;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null || context.deckManager == null)
                return;

            Deck sourceDeck = context.owner == TurnManager.TurnOwner.Player
                ? context.deckManager.playerDeck
                : context.deckManager.enemyDeck;

            if (sourceDeck == null)
            {
                Debug.LogWarning("[Revivify] Deck não encontrado.");
                context.FinalizeSpell();
                return;
            }

            var data = sourceDeck.TakeFromDiscard();
            if (data == null)
            {
                Debug.Log("[Revivify] Não há cartas no cemitério para reviver.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            float healthModifier = 0.5f;
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    Debug.Log("[Revivify] Falha crítica! A alma se perde definitivamente.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    healthModifier = 0.25f;
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    healthModifier = 0.75f;
                    break;
            }

            if (!SummonRevivedCard(context, data, healthModifier))
            {
                Debug.Log("[Revivify] Falha ao encontrar espaço para a carta revivida, devolvendo ao descarte.");
                sourceDeck.Discard(data);
            }

            context.FinalizeSpell();
        }

        bool SummonRevivedCard(SpellCastContext context, CardData data, float healthModifier)
        {
            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null)
                return false;

            var slot = zone.GetFirstEmptySlot();
            if (slot == null)
            {
                Debug.Log("[Revivify] Sem espaço na zona, carta adicionada à mão.");
                context.handManager?.AddCardToHand(data, context.owner == TurnManager.TurnOwner.Enemy);
                return true;
            }

            if (context.handManager == null || context.handManager.cardPrefab == null)
                return false;

            GameObject cardGO = Object.Instantiate(context.handManager.cardPrefab, slot.transform.parent);
            cardGO.SetActive(true);

            var cardUI = cardGO.GetComponent<CardUI>();
            if (cardUI == null)
            {
                Object.Destroy(cardGO);
                return false;
            }

            var card = new Card(data);
            int revivedHP = Mathf.Max(1, Mathf.RoundToInt(card.MaxHealth * healthModifier));
            card.MaxHealth = Mathf.Max(1, revivedHP);
            card.Defense = revivedHP;

            cardUI.ResetForHandContainer();
            cardUI.Setup(card);
            cardUI.SetOwner(context.owner);

            if (!slot.TryPlaceCard(cardUI))
            {
                Object.Destroy(cardGO);
                return false;
            }

            Debug.Log($"[Revivify] {data.cardName} retornou com {revivedHP} de vida.");
            return true;
        }
    }
}
