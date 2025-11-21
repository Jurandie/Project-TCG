using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "AuraOfLife", menuName = "Card Game/Spell Effects/Paladin/Aura of Life")]
    public class AuraOfLifeSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CON";
        public int difficultyClass = 15;
        public int duration = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var tracker = StatusUtility.EnsureHeroTracker(context.owner);
            var handler = StatusUtility.EnsureHeroHandler(context.owner);
            if (tracker == null || handler == null)
            {
                Debug.LogWarning("[AuraOfLife] Não foi possível localizar o herói para aplicar a aura.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    tracker.AddStatus(StatusEffectType.Poisoned, duration + 1, source: name);
                    Debug.Log("[AuraOfLife] Falha crítica! O herói é contaminado durante o ritual.");
                    break;
                case SpellAttributeTestOutcome.Fail:
                    tracker.AddStatus(StatusEffectType.Shielded, Mathf.Max(1, duration - 1), source: name);
                    Debug.Log("[AuraOfLife] Aura instável: proteção concedida por menos tempo.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    tracker.AddStatus(StatusEffectType.Shielded, duration, source: name);
                    Debug.Log("[AuraOfLife] Escudo divino ativo - o herói não cairá abaixo de 1 HP.");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    tracker.AddStatus(StatusEffectType.Shielded, duration + 1, source: name);
                    context.lifeManager?.Heal(context.owner, 4);
                    Debug.Log("[AuraOfLife] Sucesso crítico! Escudo prolongado e cura adicional.");
                    break;
            }

            context.FinalizeSpell();
        }
    }
}
