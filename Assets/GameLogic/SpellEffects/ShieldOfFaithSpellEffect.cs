using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "ShieldOfFaith", menuName = "Card Game/Spell Effects/Paladin/Shield of Faith")]
    public class ShieldOfFaithSpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 13;
        public int defenseBonus = 4;
        public int durationInTurns = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[ShieldOfFaith] Nenhuma zona encontrada para aplicar o escudo.");
                context.FinalizeSpell();
                return;
            }

            var cards = CollectCards(zone);
            if (cards.Count == 0)
            {
                Debug.Log("[ShieldOfFaith] Não há cartas aliadas para receber o escudo da fé.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                Debug.Log("[ShieldOfFaith] Falha crítica! A aura se desfaz e nenhuma carta é protegida.");
                context.FinalizeSpell();
                return;
            }

            int bonus = defenseBonus;
            int duration = durationInTurns;

            if (test.outcome == SpellAttributeTestOutcome.Fail)
                bonus = Mathf.Max(1, defenseBonus / 2);
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
                duration += 1;

            foreach (var card in cards)
            {
                var buff = card.gameObject.AddComponent<CardStatBuff>();
                buff.Initialize(card, context.turnManager, 0, bonus, duration);
            }

            Debug.Log($"[ShieldOfFaith] Aplicado +{bonus} DEF a {cards.Count} cartas por {duration} turnos. Teste: {test}");
            context.FinalizeSpell();
        }

        List<CardUI> CollectCards(MonsterZone zone)
        {
            var list = new List<CardUI>();
            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;

                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                list.Add(card);
            }

            return list;
        }
    }
}
