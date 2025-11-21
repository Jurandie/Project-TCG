using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "AuraOfVitality", menuName = "Card Game/Spell Effects/Paladin/Aura of Vitality")]
    public class AuraOfVitalitySpellEffect : CardSpellEffect
    {
        public string attributeKey = "CON";
        public int difficultyClass = 14;
        public int healPerTurn = 2;
        public int durationTurns = 3;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    StatusUtility.EnsureHeroTracker(context.owner)
                        ?.AddStatus(StatusEffectType.Poisoned, 2, source: name);
                    Debug.Log("[AuraOfVitality] Falha crítica! A energia vital se corrompeu e envenenou o herói.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    ApplyAura(context, Mathf.Max(1, healPerTurn - 1), Mathf.Max(1, durationTurns - 1));
                    break;
                case SpellAttributeTestOutcome.Success:
                    ApplyAura(context, healPerTurn, durationTurns);
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    ApplyAura(context, healPerTurn + 1, durationTurns + 1);
                    break;
            }
        }

        void ApplyAura(SpellCastContext context, int heal, int turns)
        {
            var host = context.turnManager != null
                ? context.turnManager.gameObject
                : (context.lifeManager != null ? context.lifeManager.gameObject : null);

            if (host == null)
            {
                Debug.LogWarning("[AuraOfVitality] Nenhum host disponível para anexar o efeito.");
                context.FinalizeSpell();
                return;
            }

            var regen = host.AddComponent<HeroRegenerationEffect>();
            regen.Initialize(context.lifeManager, context.turnManager, context.owner, heal, turns);
            Debug.Log($"[AuraOfVitality] {context.owner} receberá {heal} de cura por {turns} turnos.");
            context.FinalizeSpell();
        }
    }
}
