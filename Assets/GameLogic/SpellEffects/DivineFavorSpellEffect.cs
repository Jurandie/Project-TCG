using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "DivineFavor", menuName = "Card Game/Spell Effects/Paladin/Divine Favor")]
    public class DivineFavorSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CHA";
        public int difficultyClass = 11;
        public int attackBonus = 2;
        public int durationInTurns = 1;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[DivineFavor] Nenhuma zona encontrada para aplicar o favor divino.");
                context.FinalizeSpell();
                return;
            }

            CardUI targetCard = FindFirstEligibleCard(zone);
            if (targetCard == null)
            {
                Debug.Log("[DivineFavor] Não há cartas aliadas para receber o favor divino.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                Debug.Log("[DivineFavor] Falha crítica! O favor divino foi perdido.");
                context.FinalizeSpell();
                return;
            }

            int bonus = attackBonus;
            int duration = durationInTurns;

            if (test.outcome == SpellAttributeTestOutcome.Fail)
                bonus = Mathf.Max(1, attackBonus / 2);
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
            {
                bonus += 2;
                duration += 1;
            }

            var buff = targetCard.gameObject.AddComponent<CardStatBuff>();
            buff.Initialize(targetCard, context.turnManager, bonus, 0, duration);
            Debug.Log($"[DivineFavor] Aplicado +{bonus} ATK em {targetCard.name} por {duration} turnos. Teste: {test}");
            context.FinalizeSpell();
        }

        CardUI FindFirstEligibleCard(MonsterZone zone)
        {
            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;

                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                return card;
            }

            return null;
        }
    }
}
