using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "CircleOfPower", menuName = "Card Game/Spell Effects/Paladin/Circle of Power")]
    public class CircleOfPowerSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CHA";
        public int difficultyClass = 17;
        public int durationTurns = 3;
        public int resistanceBonus = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[CircleOfPower] Zona aliada não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var tracker = StatusUtility.EnsureHeroTracker(context.owner);
            if (tracker == null)
            {
                Debug.LogWarning("[CircleOfPower] Tracker do herói não encontrado.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    tracker.AddStatus(StatusEffectType.Silenced, 1, source: name);
                    Debug.Log("[CircleOfPower] Falha crítica! O herói entrou em choque e ficou silenciado.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    ApplyAura(tracker, zone, Mathf.Max(1, durationTurns - 1), Mathf.Max(1, resistanceBonus - 1));
                    Debug.Log("[CircleOfPower] Aura instável - concede resistência reduzida.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    ApplyAura(tracker, zone, durationTurns, resistanceBonus);
                    Debug.Log("[CircleOfPower] Resistência mágica aplicada aos aliados da zona.");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    ApplyAura(tracker, zone, durationTurns + 1, resistanceBonus + 1);
                    StatusUtility.EnsureHeroHandler(context.owner)?.lifeManager?.Heal(context.owner, 4);
                    Debug.Log("[CircleOfPower] Sucesso crítico! Aura reforçada e cura adicional.");
                    break;
            }

            context.FinalizeSpell();
        }

        void ApplyAura(HeroStatusTracker tracker, MonsterZone zone, int turns, int intensity)
        {
            tracker.RemoveStatus(StatusEffectType.Poisoned);
            tracker.RemoveStatus(StatusEffectType.Stunned);
            tracker.RemoveStatus(StatusEffectType.Marked);

            tracker.AddStatus(StatusEffectType.AuraSuppressed, turns, intensity: intensity, source: name, refresh: true);

            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;
                var card = slot.CurrentCard;
                if (card == null)
                    continue;

                var cardTracker = StatusUtility.EnsureCardTracker(card);
                cardTracker.RemoveStatus(StatusEffectType.Poisoned);
                cardTracker.RemoveStatus(StatusEffectType.Stunned);
                cardTracker.AddStatus(StatusEffectType.AuraSuppressed, turns, intensity: intensity, source: name);
            }
        }
    }
}
