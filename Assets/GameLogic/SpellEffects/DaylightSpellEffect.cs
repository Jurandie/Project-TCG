using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "Daylight", menuName = "Card Game/Spell Effects/Paladin/Daylight")]
    public class DaylightSpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 14;
        public int baseDamage = 3;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            if (enemyZone == null || enemyZone.slots == null)
            {
                Debug.LogWarning("[Daylight] Zona inimiga não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                StatusUtility.EnsureHeroTracker(context.owner)
                    ?.AddStatus(StatusEffectType.AuraSuppressed, 1, source: name);
                Debug.Log("[Daylight] Falha crítica! A luz cega o próprio herói.");
                context.FinalizeSpell();
                return;
            }

            float multiplier = 1f;
            if (test.outcome == SpellAttributeTestOutcome.Fail)
                multiplier = 0.5f;
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
                multiplier = 2f;

            int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
            int destroyed = 0;
            foreach (var slot in enemyZone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;

                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                ApplyDamage(card, damage);
                destroyed++;
            }

            Debug.Log($"[Daylight] Aplicou {damage} de dano luminoso em {destroyed} carta(s).");
            context.FinalizeSpell();
        }

        void ApplyDamage(CardUI card, int amount)
        {
            var runtime = card.runtimeCard;
            int remaining = runtime != null ? runtime.Defense : card.CurrentVisuals.defense;
            remaining -= amount;
            if (runtime != null)
            {
                runtime.Defense = Mathf.Max(0, remaining);
                card.Setup(runtime);
            }
            card.ShowFront();

            var tracker = StatusUtility.EnsureCardTracker(card);
            tracker.RemoveStatus(StatusEffectType.AuraSuppressed);
            tracker.RemoveStatus(StatusEffectType.Silenced);

            if (remaining <= 0)
            {
                var slot = card.CurrentSlot;
                if (slot != null)
                    slot.ClearSlot();
                Object.Destroy(card.gameObject);
            }
        }
    }
}
