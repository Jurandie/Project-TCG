using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "DispelEvilAndGood", menuName = "Card Game/Spell Effects/Paladin/Dispel Evil and Good")]
    public class DispelEvilAndGoodSpellEffect : CardSpellEffect
    {
        public string attributeKey = "WIS";
        public int difficultyClass = 16;
        public int allyAttackBonus = 2;
        public int allyDuration = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            var allyZone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;

            if (enemyZone == null || allyZone == null)
            {
                Debug.LogWarning("[DispelEvilAndGood] Zonas não encontradas.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    StatusUtility.EnsureHeroTracker(context.owner)
                        ?.AddStatus(StatusEffectType.Silenced, 1, source: name);
                    Debug.Log("[DispelEvilAndGood] Falha crítica! A magia se desfaz e silencia o herói.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    CleanseHero(context.owner);
                    Debug.Log("[DispelEvilAndGood] Apenas o herói foi purificado.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    CleanseHero(context.owner);
                    PurgeEnemy(enemyZone);
                    BuffAllies(allyZone, allyAttackBonus, allyDuration);
                    Debug.Log("[DispelEvilAndGood] Inimigos dissipados e aliados fortalecidos.");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    CleanseHero(context.owner);
                    PurgeEnemy(enemyZone, destroyUndeadOnly: false);
                    BuffAllies(allyZone, allyAttackBonus + 1, allyDuration + 1);
                    Debug.Log("[DispelEvilAndGood] Sucesso crítico! Dissipação total e buffs prolongados.");
                    break;
            }

            context.FinalizeSpell();
        }

        void CleanseHero(TurnManager.TurnOwner owner)
        {
            var tracker = StatusUtility.EnsureHeroTracker(owner);
            tracker?.RemoveStatus(StatusEffectType.Poisoned);
            tracker?.RemoveStatus(StatusEffectType.Stunned);
            tracker?.RemoveStatus(StatusEffectType.Marked);
        }

        void PurgeEnemy(MonsterZone zone, bool destroyUndeadOnly = true)
        {
            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;

                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                bool isUndead = (card.runtimeCard != null && card.runtimeCard.IsUndead) ||
                                (card.cardData != null && card.cardData.isUndead);

                if (destroyUndeadOnly && !isUndead)
                {
                    var tracker = StatusUtility.EnsureCardTracker(card);
                    tracker.RemoveStatus(StatusEffectType.Poisoned);
                    tracker.RemoveStatus(StatusEffectType.Silenced);
                    continue;
                }

                slot.ClearSlot();
                Object.Destroy(card.gameObject);
            }
        }

        void BuffAllies(MonsterZone zone, int bonus, int duration)
        {
            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;
                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                var buff = card.gameObject.AddComponent<CardStatBuff>();
                buff.Initialize(card, FindFirstObjectByType<TurnManager>(), bonus, 0, duration);
            }
        }
    }
}
