using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "StaggeringSmite", menuName = "Card Game/Spell Effects/Paladin/Staggering Smite")]
    public class StaggeringSmiteSpellEffect : CardSpellEffect
    {
        public string attributeKey = "STR";
        public int difficultyClass = 15;
        public int bonusDamage = 5;
        public int stunDuration = 1;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var enemyZone = context.owner == TurnManager.TurnOwner.Player ? context.enemyZone : context.playerZone;
            if (enemyZone == null || enemyZone.slots == null)
            {
                Debug.LogWarning("[StaggeringSmite] Zona inimiga não encontrada.");
                context.FinalizeSpell();
                return;
            }

            var targetCard = FindFirstEnemy(enemyZone);
            if (targetCard == null)
            {
                Debug.Log("[StaggeringSmite] Não há cartas inimigas disponíveis para o golpe.");
                context.FinalizeSpell();
                return;
            }

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);
            switch (test.outcome)
            {
                case SpellAttributeTestOutcome.CriticalFail:
                    context.lifeManager?.TakeDamage(context.owner, 3);
                    Debug.Log("[StaggeringSmite] Falha crítica! O herói sofre o impacto de retorno.");
                    break;
                case SpellAttributeTestOutcome.Fail:
                    ApplyDamage(targetCard, Mathf.Max(1, bonusDamage / 2));
                    Debug.Log("[StaggeringSmite] Golpe parcial - não atordoa.");
                    break;
                case SpellAttributeTestOutcome.Success:
                    ApplyDamage(targetCard, bonusDamage);
                    ApplyStun(targetCard, stunDuration);
                    Debug.Log("[StaggeringSmite] Golpe atordoador aplicado com sucesso.");
                    break;
                case SpellAttributeTestOutcome.CriticalSuccess:
                    ApplyDamage(targetCard, bonusDamage + 2);
                    ApplyStun(targetCard, stunDuration + 1);
                    Debug.Log("[StaggeringSmite] Sucesso crítico! Dano extra e atordoamento prolongado.");
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

        void ApplyStun(CardUI card, int duration)
        {
            var tracker = StatusUtility.EnsureCardTracker(card);
            tracker.AddStatus(StatusEffectType.Stunned, duration, source: name);
        }
    }
}
