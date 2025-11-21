using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "ZoneOfTruth", menuName = "Card Game/Spell Effects/Paladin/Zone of Truth")]
    public class ZoneOfTruthSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CHA";
        public int difficultyClass = 15;
        public int auraDuration = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            if (enemyZone == null || enemyZone.slots == null)
            {
                Debug.LogWarning("[ZoneOfTruth] Nenhuma zona adversária encontrada.");
                context.FinalizeSpell();
                return;
            }

            var targetCard = FindFirstEnemyCard(enemyZone);
            if (targetCard == null)
            {
                Debug.Log("[ZoneOfTruth] O adversário não possui cartas para serem afetadas.");
                context.FinalizeSpell();
                return;
            }

            var evolutionManager = StatusUtility.GetEvolutionManager();
            if (evolutionManager == null)
            {
                Debug.LogWarning("[ZoneOfTruth] CardEvolutionManager não encontrado.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    ApplyPenaltyToCaster(context, evolutionManager);
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    Debug.Log("[ZoneOfTruth] A carta resistiu ao interrogatório divino.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Success:
                    ApplyAura(targetCard, evolutionManager, 1);
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    ApplyAura(targetCard, evolutionManager, 2);
                    break;
            }

            context.FinalizeSpell();
        }

        void ApplyAura(CardUI targetCard, CardEvolutionManager evolutionManager, int tiers)
        {
            if (!evolutionManager.ForceDowngrade(targetCard, tiers))
                Debug.Log("[ZoneOfTruth] A carta já estava no nível base, mas a aura ainda impede evoluções.");
            var tracker = StatusUtility.EnsureCardTracker(targetCard);
            tracker.AddStatus(StatusEffectType.AuraSuppressed, auraDuration, source: name);
            Debug.Log($"[ZoneOfTruth] {targetCard.name} regrediu {tiers} nível(is) e está sob a Zona da Verdade por {auraDuration} turnos.");
        }

        void ApplyPenaltyToCaster(SpellCastContext context, CardEvolutionManager evolutionManager)
        {
            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
                return;

            var card = FindFirstEnemyCard(zone); // inimigo = próprio conjurador
            if (card == null)
                return;

            evolutionManager.ForceDowngrade(card, 1);
            var tracker = StatusUtility.EnsureCardTracker(card);
            tracker.AddStatus(StatusEffectType.Silenced, 1, source: name);
            Debug.Log("[ZoneOfTruth] Falha crítica! A aura se volta contra o conjurador.");
        }

        CardUI FindFirstEnemyCard(MonsterZone zone)
        {
            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;

                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                return card;
            }

            return null;
        }
    }
}
