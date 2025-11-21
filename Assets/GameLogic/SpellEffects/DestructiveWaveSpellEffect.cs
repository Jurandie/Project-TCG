using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "DestructiveWave", menuName = "Card Game/Spell Effects/Paladin/Destructive Wave")]
    public class DestructiveWaveSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CON";
        public int difficultyClass = 17;
        public int radiantDamage = 5;
        public int thunderDamage = 5;
        public int stunDuration = 1;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            if (enemyZone == null || enemyZone.slots == null)
            {
                Debug.LogWarning("[DestructiveWave] Zona inimiga não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            float multiplier = 1f;
            bool applyStun = true;

            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    context.lifeManager?.TakeDamage(context.owner, 5);
                    Debug.Log("[DestructiveWave] Falha crítica! A onda explode sobre o próprio herói.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    multiplier = 0.5f;
                    applyStun = false;
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    multiplier = 1.5f;
                    stunDuration += 1;
                    break;
            }

            int totalDamage = Mathf.Max(1, Mathf.RoundToInt((radiantDamage + thunderDamage) * multiplier));
            foreach (var slot in enemyZone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;
                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                int remaining = ApplyDamage(card, totalDamage);
                if (remaining > 0 && applyStun)
                {
                    var tracker = StatusUtility.EnsureCardTracker(card);
                    tracker.AddStatus(StatusEffectType.Stunned, stunDuration, source: name);
                }
            }

            Debug.Log($"[DestructiveWave] Infligiu {totalDamage} de dano em área. Stun aplicável: {applyStun}");
            context.FinalizeSpell();
        }

        int ApplyDamage(CardUI card, int amount)
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

            if (remaining <= 0)
            {
                var slot = card.CurrentSlot;
                if (slot != null)
                    slot.ClearSlot();
                Object.Destroy(card.gameObject);
                remaining = 0;
            }

            return remaining;
        }
    }
}
