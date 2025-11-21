using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "HolyWeapon", menuName = "Card Game/Spell Effects/Paladin/Holy Weapon")]
    public class HolyWeaponSpellEffect : CardSpellEffect
    {
        public string attributeKey = "STR";
        public int difficultyClass = 14;
        public int attackBonus = 4;
        public int defenseBonus = 2;
        public int durationTurns = 2;
        [Tooltip("Bônus percentual contra mortos-vivos.")]
        public int undeadBonusPercent = 25;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null || zone.slots == null)
            {
                Debug.LogWarning("[HolyWeapon] Nenhuma zona encontrada para aplicar a arma sagrada.");
                context.FinalizeSpell();
                return;
            }

            var targetCard = FindFirstEligibleCard(zone);
            if (targetCard == null)
            {
                Debug.Log("[HolyWeapon] Não há cartas aliadas para receber a benção.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                var tracker = StatusUtility.EnsureCardTracker(targetCard);
                tracker.AddStatus(StatusEffectType.Silenced, 1, source: name);
                Debug.Log("[HolyWeapon] Falha crítica! A arma se dessintoniza e silencia a carta.");
                context.FinalizeSpell();
                return;
            }

            int atk = attackBonus;
            int def = defenseBonus;
            int duration = durationTurns;

            if (test.outcome == SpellAttributeTestOutcome.Fail)
            {
                atk = Mathf.Max(1, attackBonus / 2);
                def = Mathf.Max(0, defenseBonus / 2);
            }
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
            {
                atk += 2;
                duration += 1;
            }

            var buff = targetCard.gameObject.AddComponent<CardStatBuff>();
            buff.Initialize(targetCard, context.turnManager, atk, def, duration);
            var trackerBuff = StatusUtility.EnsureCardTracker(targetCard);
            trackerBuff.AddStatus(StatusEffectType.BlessedWeapon, duration, intensity: undeadBonusPercent, source: name);

            Debug.Log($"[HolyWeapon] {targetCard.name} recebeu +{atk}/+{def} por {duration} turnos. Dano extra vs Undead: {undeadBonusPercent}%.");
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
