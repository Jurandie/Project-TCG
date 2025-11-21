using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "LesserRestoration", menuName = "Card Game/Spell Effects/Paladin/Lesser Restoration")]
    public class LesserRestorationSpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 13;
        public StatusEffectType[] removableStatuses = new[]
        {
            StatusEffectType.Poisoned,
            StatusEffectType.Silenced,
            StatusEffectType.Marked,
            StatusEffectType.Stunned
        };
        public int penaltyDuration = 2;
        public int healOnCriticalSuccess = 5;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var heroTracker = StatusUtility.EnsureHeroTracker(context.owner);
            var heroHandler = StatusUtility.EnsureHeroHandler(context.owner);

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    ApplyNegativeStatus(heroTracker, StatusEffectType.Poisoned, penaltyDuration + 1);
                    heroHandler?.lifeManager?.TakeDamage(context.owner, 2);
                    Debug.Log("[LesserRestoration] Falha crítica! O herói sofre envenenamento.");
                    context.FinalizeSpell();
                    return;

                case SpellAttributeTestOutcome.Fail:
                    ApplyNegativeStatus(heroTracker, StatusEffectType.Poisoned, penaltyDuration);
                    Debug.Log("[LesserRestoration] Falha no ritual. Um veneno residual afeta o herói.");
                    context.FinalizeSpell();
                    return;

                case SpellAttributeTestOutcome.Success:
                    CleanseHero(heroTracker);
                    CleanseFirstAlly(context);
                    Debug.Log("[LesserRestoration] Status negativos removidos do herói.");
                    break;

                case SpellAttributeTestOutcome.CriticalSuccess:
                    CleanseHero(heroTracker);
                    CleanseFirstAlly(context);
                    heroHandler?.lifeManager?.Heal(context.owner, healOnCriticalSuccess);
                    Debug.Log("[LesserRestoration] Sucesso crítico! Além de remover os status, o herói foi curado.");
                    break;
            }

            context.FinalizeSpell();
        }

        void CleanseHero(HeroStatusTracker tracker)
        {
            if (tracker == null)
                return;

            foreach (var status in removableStatuses)
                tracker.RemoveStatus(status);
        }

        void CleanseFirstAlly(SpellCastContext context)
        {
            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
                return;

            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;

                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                var tracker = StatusUtility.EnsureCardTracker(card);
                foreach (var status in removableStatuses)
                    tracker.RemoveStatus(status);
                break;
            }
        }

        void ApplyNegativeStatus(HeroStatusTracker tracker, StatusEffectType type, int duration)
        {
            tracker?.AddStatus(type, duration, intensity: 1, source: name);
        }
    }
}
