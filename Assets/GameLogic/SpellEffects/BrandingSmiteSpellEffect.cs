using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "BrandingSmite", menuName = "Card Game/Spell Effects/Paladin/Branding Smite")]
    public class BrandingSmiteSpellEffect : CardSpellEffect
    {
        [Header("Teste de atributo")]
        public string attributeKey = "STR";
        public int difficultyClass = 14;

        [Header("Bônus")]
        public int bonusAttack = 4;
        public int buffDuration = 1;
        public int markDuration = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[BrandingSmite] Zona não encontrada para aplicar o golpe.");
                context.FinalizeSpell();
                return;
            }

            var targetCard = FindFirstEligibleCard(zone);
            if (targetCard == null)
            {
                Debug.Log("[BrandingSmite] Nenhuma carta aliada para receber o golpe marcado.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                Debug.Log("[BrandingSmite] Falha crítica! A arma escorrega e o herói leva 2 de dano.");
                context.lifeManager?.TakeDamage(context.owner, 2);
                context.FinalizeSpell();
                return;
            }

            int appliedBonus = bonusAttack;
            int duration = buffDuration;
            int markTurns = markDuration;

            if (test.outcome == SpellAttributeTestOutcome.Fail)
            {
                appliedBonus = Mathf.Max(1, bonusAttack / 2);
            }
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
            {
                appliedBonus += 2;
                markTurns += 1;
            }

            var buff = targetCard.gameObject.AddComponent<CardStatBuff>();
            buff.Initialize(targetCard, context.turnManager, appliedBonus, 0, duration);

            var tracker = targetCard.GetComponent<CardStatusTracker>();
            if (tracker == null)
                tracker = targetCard.gameObject.AddComponent<CardStatusTracker>();
            tracker.AddStatus(StatusEffectType.Marked, markTurns, intensity: appliedBonus, source: name);
            tracker.AddStatus(StatusEffectType.CannotStealth, markTurns, source: name);

            Debug.Log($"[BrandingSmite] {targetCard.name} recebeu +{appliedBonus} ATK por {duration} turnos e marca por {markTurns}. Teste: {test}");
            context.FinalizeSpell();
        }

        CardUI FindFirstEligibleCard(MonsterZone zone)
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
