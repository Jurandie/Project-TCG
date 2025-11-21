using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "BlessingOfLight", menuName = "Card Game/Spell Effects/Paladin/Blessing of Light")]
    public class BlessingOfLightSpellEffect : CardSpellEffect
    {
        [Header("Configuração")]
        public string attributeKey = "CHA";
        public int difficultyClass = 12;
        public int attackBonus = 2;
        public int defenseBonus = 2;
        public int durationInTurns = 2;
        public int maxTargets = 3;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = GetCasterZone(context);
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[BlessingOfLight] Nenhuma zona encontrada para aplicar a bênção.");
                context.FinalizeSpell();
                return;
            }

            var targets = CollectTargets(zone, maxTargets);
            if (targets.Count == 0)
            {
                Debug.Log("[BlessingOfLight] Não há cartas aliadas para receber a bênção.");
                context.FinalizeSpell();
                return;
            }

            var testResult = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (testResult.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                Debug.Log("[BlessingOfLight] Falha crítica! A carta magia foi desperdiçada.");
                context.FinalizeSpell();
                return;
            }

            int appliedAttack = attackBonus;
            int appliedDefense = defenseBonus;
            int duration = durationInTurns;

            if (testResult.outcome == SpellAttributeTestOutcome.Fail)
            {
                appliedAttack = Mathf.Max(1, attackBonus / 2);
                appliedDefense = Mathf.Max(1, defenseBonus / 2);
            }
            else if (testResult.outcome == SpellAttributeTestOutcome.CriticalSuccess)
            {
                appliedAttack += 1;
                appliedDefense += 1;
                duration += 1;
            }

            foreach (var card in targets)
            {
                var buff = card.gameObject.AddComponent<CardStatBuff>();
                buff.Initialize(card, context.turnManager, appliedAttack, appliedDefense, duration);
            }

            Debug.Log($"[BlessingOfLight] Aplicado +{appliedAttack}/+{appliedDefense} em {targets.Count} cartas por {duration} turnos. Teste: {testResult}");
            context.FinalizeSpell();
        }

        MonsterZone GetCasterZone(SpellCastContext context)
        {
            return context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
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
