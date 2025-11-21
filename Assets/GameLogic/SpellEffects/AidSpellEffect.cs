using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "PaladinAid", menuName = "Card Game/Spell Effects/Paladin/Aid")]
    public class AidSpellEffect : CardSpellEffect
    {
        [Header("Teste de atributo")]
        public string attributeKey = "CHA";
        public int difficultyClass = 13;

        [Header("Bônus")]
        public int maxTargets = 3;
        public int bonusHp = 4;
        public int durationTurns = 3;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[Aid] Zona não encontrada para aplicar o efeito.");
                context.FinalizeSpell();
                return;
            }

            var targets = CollectTargets(zone, maxTargets);
            if (targets.Count == 0)
            {
                Debug.Log("[Aid] Nenhuma carta aliada para receber Socorro Valente.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                Debug.Log("[Aid] Falha crítica! O herói sofre o recuo divino.");
                context.lifeManager?.TakeDamage(context.owner, 2);
                context.FinalizeSpell();
                return;
            }

            int appliedBonus = bonusHp;
            int duration = durationTurns;
            bool heal = true;

            if (test.outcome == SpellAttributeTestOutcome.Fail)
            {
                appliedBonus = Mathf.Max(1, bonusHp / 2);
                duration = Mathf.Max(1, durationTurns - 1);
            }
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
            {
                appliedBonus += 2;
                duration += 1;
            }

            foreach (var card in targets)
            {
                var buff = card.gameObject.AddComponent<CardStatBuff>();
                buff.Initialize(card, context.turnManager, 0, 0, duration, appliedBonus, heal);
            }

            Debug.Log($"[Aid] Aplicado +{appliedBonus} HP máx em {targets.Count} cartas por {duration} turnos. Teste: {test}");
            context.FinalizeSpell();
        }

        List<CardUI> CollectTargets(MonsterZone zone, int limit)
        {
            var list = new List<CardUI>();

            foreach (var slot in zone.slots)
            {
                if (slot == null || slot.IsEmpty())
                    continue;

                var card = slot.CurrentCard;
                if (card == null || card.IsSpell)
                    continue;

                list.Add(card);
                if (list.Count >= limit)
                    break;
            }

            return list;
        }
    }
}
