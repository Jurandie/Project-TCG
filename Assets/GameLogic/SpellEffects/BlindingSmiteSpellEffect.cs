using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "BlindingSmite", menuName = "Card Game/Spell Effects/Paladin/Blinding Smite")]
    public class BlindingSmiteSpellEffect : CardSpellEffect
    {
        public string attributeKey = "STR";
        public int difficultyClass = 15;
        public int damage = 5;
        public int blindDuration = 1;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            if (enemyZone == null || enemyZone.slots == null)
            {
                Debug.LogWarning("[BlindingSmite] Zona inimiga não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var targetCard = FindFirstEnemy(enemyZone);
            if (targetCard == null)
            {
                Debug.Log("[BlindingSmite] Não há cartas inimigas para atingir.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    context.lifeManager?.TakeDamage(context.owner, 3);
                    Debug.Log("[BlindingSmite] Falha crítica! O herói se fere no processo.");
                    context.FinalizeSpell();
                    return;
                case SpellAttributeTestOutcome.Fail:
                    ApplyDamage(targetCard, Mathf.Max(1, damage / 2));
                    Debug.Log("[BlindingSmite] Golpe parcial sem cegar o alvo.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    ApplyDamage(targetCard, damage);
                    ApplyBlind(targetCard, blindDuration);
                    Debug.Log("[BlindingSmite] Golpe bem-sucedido! O alvo foi cegado.");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    ApplyDamage(targetCard, damage + 3);
                    ApplyBlind(targetCard, blindDuration + 1);
                    Debug.Log("[BlindingSmite] Sucesso crítico! Dano extra e cegueira prolongada.");
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

        void ApplyDamage(CardUI card, int amount)
        {
            if (card == null)
                return;

            var runtime = card.runtimeCard;
            int remaining = runtime != null ? runtime.Defense : card.CurrentVisuals.defense;
            remaining -= amount;
            if (runtime != null)
            {
                runtime.Defense = Mathf.Max(0, remaining);
                card.Setup(runtime);
            }
            card.ShowFront();

            if (remaining <= 0)
            {
                var slot = card.CurrentSlot;
                if (slot != null)
                    slot.ClearSlot();
                Object.Destroy(card.gameObject);
            }
        }

        void ApplyBlind(CardUI card, int duration)
        {
            var tracker = StatusUtility.EnsureCardTracker(card);
            tracker.AddStatus(StatusEffectType.Stunned, duration, source: name);
        }
    }
}
