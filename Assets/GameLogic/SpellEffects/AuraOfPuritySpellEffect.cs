using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "AuraOfPurity", menuName = "Card Game/Spell Effects/Paladin/Aura of Purity")]
    public class AuraOfPuritySpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 15;
        public int duration = 3;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var tracker = StatusUtility.EnsureHeroTracker(context.owner);
            if (tracker == null)
            {
                Debug.LogWarning("[AuraOfPurity] Não foi possível localizar o herói para aplicar a aura.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    tracker.AddStatus(StatusEffectType.Poisoned, 2, source: name);
                    Debug.Log("[AuraOfPurity] Falha crítica! A aura recai em veneno sobre o herói.");
                    break;
                case SpellAttributeTestOutcome.Fail:
                    ApplyPurity(tracker, Mathf.Max(1, duration - 1));
                    Debug.Log("[AuraOfPurity] A aura purifica parcialmente, concedendo resistência reduzida.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    ApplyPurity(tracker, duration);
                    Debug.Log("[AuraOfPurity] Resistência a debuffs e limpeza de estados aplicada.");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    ApplyPurity(tracker, duration + 1);
                    StatusUtility.EnsureHeroHandler(context.owner)?.lifeManager?.Heal(context.owner, 3);
                    Debug.Log("[AuraOfPurity] Sucesso crítico! Aura fortalecida e cura adicional.");
                    break;
            }

            context.FinalizeSpell();
        }

        void ApplyPurity(HeroStatusTracker tracker, int turns)
        {
            foreach (StatusEffectType type in System.Enum.GetValues(typeof(StatusEffectType)))
            {
                if (type == StatusEffectType.Poisoned || type == StatusEffectType.Stunned || type == StatusEffectType.Marked || type == StatusEffectType.Silenced)
                    tracker.RemoveStatus(type);
            }

            tracker.AddStatus(StatusEffectType.AuraSuppressed, turns, source: "PurityResistance", refresh: true);
        }
    }
}
