using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Applies temporary stat changes (attack, defense, max HP) to a card.
    /// </summary>
    [DisallowMultipleComponent]
    public class CardStatBuff : MonoBehaviour
    {
        CardUI card;
        TurnManager turnManager;

        int attackBonus;
        int defenseBonus;
        int maxHealthBonus;
        int turnsRemaining;
        bool healOnApply;
        bool applied;
        bool pendingDestroy;

        public void Initialize(CardUI target, TurnManager manager, int atkBonus, int defBonus, int duration, int maxHpBonus = 0, bool healBonus = true)
        {
            card = target ?? GetComponent<CardUI>();
            turnManager = manager ?? FindFirstObjectByType<TurnManager>();
            attackBonus = atkBonus;
            defenseBonus = defBonus;
            maxHealthBonus = maxHpBonus;
            turnsRemaining = Mathf.Max(1, duration);
            healOnApply = healBonus;

            ApplyBonus();

            if (turnManager != null)
                turnManager.OnTurnStarted += OnTurnStarted;
        }

        void ApplyBonus()
        {
            if (card == null || card.runtimeCard == null || applied)
                return;

            var runtime = card.runtimeCard;
            runtime.Attack += attackBonus;
            runtime.Defense += defenseBonus;
            runtime.MaxHealth += maxHealthBonus;

            if (maxHealthBonus > 0 && healOnApply)
                runtime.Defense = Mathf.Min(runtime.MaxHealth, runtime.Defense + maxHealthBonus);
            else
                runtime.Defense = Mathf.Min(runtime.MaxHealth, runtime.Defense);

            card.Setup(runtime);
            card.ShowFront();
            applied = true;
        }

        void RemoveBonus()
        {
            if (card == null || card.runtimeCard == null || !applied)
                return;

            var runtime = card.runtimeCard;
            runtime.Attack -= attackBonus;
            runtime.Defense -= defenseBonus;
            runtime.MaxHealth -= maxHealthBonus;

            runtime.MaxHealth = Mathf.Max(1, runtime.MaxHealth);
            runtime.Defense = Mathf.Clamp(runtime.Defense, 0, runtime.MaxHealth);

            card.Setup(runtime);
            card.ShowFront();
            applied = false;
        }

        void OnTurnStarted(TurnManager.TurnOwner owner)
        {
            if (card == null)
                return;

            if (owner != card.OwnerSide)
                return;

            turnsRemaining--;
            if (turnsRemaining <= 0)
                DestroySelf();
        }

        void DestroySelf()
        {
            if (pendingDestroy)
                return;

            pendingDestroy = true;
            RemoveBonus();
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
            Destroy(this);
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;

            if (!pendingDestroy)
                RemoveBonus();
        }
    }
}
