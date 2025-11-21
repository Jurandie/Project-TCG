using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "BanishingSmite", menuName = "Card Game/Spell Effects/Paladin/Banishing Smite")]
    public class BanishingSmiteSpellEffect : CardSpellEffect
    {
        public string attributeKey = "STR";
        public int difficultyClass = 17;
        public int baseDamage = 6;
        public int thresholdHP = 5;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            if (enemyZone == null || enemyZone.slots == null)
            {
                Debug.LogWarning("[BanishingSmite] Zona inimiga não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var targetCard = FindFirstEnemy(enemyZone);
            if (targetCard == null)
            {
                Debug.Log("[BanishingSmite] Não há cartas inimigas para atacar.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    context.lifeManager?.TakeDamage(context.owner, 4);
                    Debug.Log("[BanishingSmite] Falha crítica! O golpe ricocheteia no herói.");
                    break;
                case SpellAttributeTestOutcome.Fail:
                    ApplyDamage(targetCard, Mathf.Max(1, baseDamage / 2));
                    break;
                case SpellAttributeTestOutcome.Success:
                    if (ApplyDamage(targetCard, baseDamage) <= thresholdHP)
                        BanishOrClaim(context, targetCard);
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    if (ApplyDamage(targetCard, baseDamage + 3) <= thresholdHP)
                        BanishOrClaim(context, targetCard, claimOnSuccess: true);
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

        int ApplyDamage(CardUI targetCard, int amount)
        {
            var runtime = targetCard.runtimeCard;
            int remaining = runtime != null ? runtime.Defense : targetCard.CurrentVisuals.defense;
            remaining -= amount;
            if (runtime != null)
            {
                runtime.Defense = Mathf.Max(0, remaining);
                targetCard.Setup(runtime);
            }
            targetCard.ShowFront();

            if (remaining <= 0)
            {
                var slot = targetCard.CurrentSlot;
                if (slot != null)
                    slot.ClearSlot();
                Object.Destroy(targetCard.gameObject);
                remaining = 0;
            }

            return remaining;
        }

        void BanishOrClaim(SpellCastContext context, CardUI card, bool claimOnSuccess = false)
        {
            if (claimOnSuccess)
            {
                var zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
                if (zone != null)
                {
                    var slot = zone.GetFirstEmptySlot();
                    if (slot != null)
                    {
                        card.SetOwner(context.owner);
                        slot.TryPlaceCard(card);
                        Debug.Log("[BanishingSmite] Carta inimiga foi reivindicada!");
                        return;
                    }
                }
            }

            StatusUtility.BanishCard(card, 2);
            Debug.Log("[BanishingSmite] Carta banida do campo.");
        }
    }
}
