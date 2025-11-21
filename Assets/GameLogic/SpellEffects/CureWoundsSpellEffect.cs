using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "CureWounds", menuName = "Card Game/Spell Effects/Paladin/Cure Wounds")]
    public class CureWoundsSpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 12;
        public int baseHeal = 5;
        public int criticalMultiplier = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null || context.lifeManager == null)
                return;

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            int healAmount = baseHeal + Mathf.Max(0, test.modifier);

            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    Debug.Log("[CureWounds] Falha crítica! A energia divina ricocheteia e nenhum aliado é curado.");
                    context.lifeManager.TakeDamage(context.owner, 2);
                    context.FinalizeSpell();
                    return;

                case SpellAttributeTestOutcome.Fail:
                    healAmount = Mathf.Max(1, healAmount / 2);
                    break;

                case SpellAttributeTestOutcome.CriticalSuccess:
                    healAmount *= criticalMultiplier;
                    break;
            }

            context.lifeManager.Heal(context.owner, healAmount);
            Debug.Log($"[CureWounds] {context.owner} curou {healAmount} de HP. Teste: {test}");
            context.FinalizeSpell();
        }
    }
}
