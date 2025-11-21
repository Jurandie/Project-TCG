using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "Banishment", menuName = "Card Game/Spell Effects/Paladin/Banishment")]
    public class BanishmentSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CHA";
        public int difficultyClass = 16;
        public int baseDuration = 2;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            if (enemyZone == null || enemyZone.slots == null)
            {
                Debug.LogWarning("[Banishment] Zona inimiga não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var targetCard = FindFirstEnemy(enemyZone);
            if (targetCard == null)
            {
                Debug.Log("[Banishment] Não há cartas inimigas para banir.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    StatusUtility.BanishCard(targetCard, 1);
                    StatusUtility.EnsureCardTracker(targetCard)?.AddStatus(StatusEffectType.Silenced, 1, source: name);
                    Debug.Log("[Banishment] Falha crítica! A carta retorna imediatamente e silencia o conjurador.");
                    break;
                case SpellAttributeTestOutcome.Fail:
                    Debug.Log("[Banishment] O alvo resistiu ao banimento.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    StatusUtility.BanishCard(targetCard, baseDuration);
                    Debug.Log($"[Banishment] Carta banida por {baseDuration} turno(s).");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    StatusUtility.BanishCard(targetCard, baseDuration + 1);
                    Debug.Log($"[Banishment] Sucesso crítico! Carta banida por {baseDuration + 1} turnos.");
                    break;
            }

            context.FinalizeSpell();
        }

        CardUI FindFirstEnemy(MonsterZone zone)
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
