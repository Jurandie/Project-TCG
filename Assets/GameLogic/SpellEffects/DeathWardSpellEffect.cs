using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "DeathWard", menuName = "Card Game/Spell Effects/Paladin/Death Ward")]
    public class DeathWardSpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 16;
        public int duration = 3;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var tracker = StatusUtility.EnsureHeroTracker(context.owner);
            if (tracker == null)
            {
                Debug.LogWarning("[DeathWard] Não foi possível localizar o herói.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    tracker.AddStatus(StatusEffectType.Poisoned, 2, source: name);
                    Debug.Log("[DeathWard] Falha crítica! O herói sofre uma reação necromântica.");
                    break;
                case SpellAttributeTestOutcome.Fail:
                    tracker.AddStatus(StatusEffectType.Shielded, 1, source: name);
                    Debug.Log("[DeathWard] Proteção enfraquecida - dura apenas 1 turno.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    tracker.AddStatus(StatusEffectType.Shielded, duration, source: name);
                    Debug.Log("[DeathWard] O próximo golpe fatal será negado.");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    tracker.AddStatus(StatusEffectType.Shielded, duration);
                    tracker.AddStatus(StatusEffectType.BlessedWeapon, duration, source: name);
                    StatusUtility.EnsureHeroHandler(context.owner)?.lifeManager?.Heal(context.owner, 5);
                    Debug.Log("[DeathWard] Sucesso crítico! Escudo prolongado e benção ofensiva concedida.");
                    break;
            }

            context.FinalizeSpell();
        }
    }
}
