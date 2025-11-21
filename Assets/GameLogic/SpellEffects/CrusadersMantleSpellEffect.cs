using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "CrusadersMantle", menuName = "Card Game/Spell Effects/Paladin/Crusader's Mantle")]
    public class CrusadersMantleSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CHA";
        public int difficultyClass = 14;
        public int attackBonus = 1;
        public int durationTurns = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[CrusadersMantle] Zona aliada não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                StatusUtility.EnsureHeroTracker(context.owner)
                    ?.AddStatus(StatusEffectType.Silenced, 1, source: name);
                Debug.Log("[CrusadersMantle] Falha crítica! O herói perde a voz sagrada neste turno.");
                context.FinalizeSpell();
                return;
            }

            int bonus = attackBonus;
            int duration = durationTurns;

            if (test.outcome == SpellAttributeTestOutcome.Fail)
                bonus = Mathf.Max(1, attackBonus / 2);
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
            {
                bonus += 1;
                duration += 1;
            }

            int affected = 0;
            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;
                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                var buff = card.gameObject.AddComponent<CardStatBuff>();
                buff.Initialize(card, context.turnManager, bonus, 0, duration);
                affected++;
            }

            Debug.Log($"[CrusadersMantle] {affected} carta(s) receberam +{bonus} ATK por {duration} turno(s).");
            context.FinalizeSpell();
        }
    }
}
