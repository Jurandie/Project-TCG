using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "RaiseDead", menuName = "Card Game/Spell Effects/Paladin/Raise Dead")]
    public class RaiseDeadSpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 18;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null || context.deckManager == null)
                return;

            Deck sourceDeck = context.owner == TurnManager.TurnOwner.Player
                ? context.deckManager.playerDeck
                : context.deckManager.enemyDeck;

            if (sourceDeck == null)
            {
                Debug.LogWarning("[RaiseDead] Deck não encontrado.");
                context.FinalizeSpell();
                return;
            }

            var data = sourceDeck.TakeFromDiscard();
            if (data == null)
            {
                Debug.Log("[RaiseDead] Nenhuma carta disponível no cemitério.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            float hpFactor = 0.75f;
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    Debug.Log("[RaiseDead] Falha crítica! A alma se recusa a retornar.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    hpFactor = 0.5f;
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    hpFactor = 1f;
                    break;
            }

            if (!SummonRevivedCard(context, data, hpFactor))
            {
                sourceDeck.Discard(data);
                Debug.Log("[RaiseDead] Sem espaço - carta devolvida ao cemitério.");
            }

            context.FinalizeSpell();
        }

        bool SummonRevivedCard(SpellCastContext context, CardData data, float hpFactor)
        {
            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null)
                return false;

            var slot = zone.GetFirstEmptySlot();
            bool toHand = slot == null;

            if (context.handManager == null || context.handManager.cardPrefab == null)
                return false;

            GameObject cardGO = Object.Instantiate(context.handManager.cardPrefab,
                toHand ? (context.owner == TurnManager.TurnOwner.Player ? context.handManager.playerHandContainer : context.handManager.enemyHandContainer)
                       : slot.transform.parent);
            cardGO.SetActive(true);

            var cardUI = cardGO.GetComponent<CardUI>();
            if (cardUI == null)
            {
                Object.Destroy(cardGO);
                return false;
            }

            var card = new Card(data);
            int revivedHP = Mathf.Max(1, Mathf.RoundToInt(card.MaxHealth * hpFactor));
            card.MaxHealth = revivedHP;
            card.Defense = revivedHP;

            cardUI.ResetForHandContainer();
            cardUI.Setup(card);
            cardUI.SetOwner(context.owner);

            if (!toHand)
            {
                if (!slot.TryPlaceCard(cardUI))
                {
                    Object.Destroy(cardGO);
                    return false;
                }
            }
            else
            {
                if (context.handManager.hideEnemyCards && context.owner == TurnManager.TurnOwner.Enemy)
                    cardUI.ShowBack();
            }

            var tracker = StatusUtility.EnsureCardTracker(cardUI);
            tracker.RemoveStatus(StatusEffectType.Poisoned);
            tracker.RemoveStatus(StatusEffectType.Silenced);

            Debug.Log($"[RaiseDead] {data.cardName} retornou com {revivedHP} HP ({(hpFactor * 100f):0}% do total).");
            return true;
        }
    }
}
