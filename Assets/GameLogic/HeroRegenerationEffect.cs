using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Applies a heal-over-time effect to one of the heroes.
    /// </summary>
    public class HeroRegenerationEffect : MonoBehaviour
    {
        LifeManager lifeManager;
        TurnManager turnManager;
        TurnManager.TurnOwner owner;
        int healPerTurn;
        int turnsRemaining;
        bool initialized;

        public void Initialize(LifeManager life, TurnManager manager, TurnManager.TurnOwner targetOwner, int healAmount, int duration)
        {
            lifeManager = life ?? FindFirstObjectByType<LifeManager>();
            turnManager = manager ?? FindFirstObjectByType<TurnManager>();
            owner = targetOwner;
            healPerTurn = Mathf.Max(1, healAmount);
            turnsRemaining = Mathf.Max(1, duration);

            if (turnManager != null)
            {
                turnManager.OnTurnStarted += OnTurnStarted;
                initialized = true;
            }
            else
            {
                ApplyHeal(); // fallback single tick
                Destroy(this);
            }
        }

        void OnTurnStarted(TurnManager.TurnOwner startedOwner)
        {
            if (!initialized || startedOwner != owner)
                return;

            ApplyHeal();
            turnsRemaining--;
            if (turnsRemaining <= 0)
                Destroy(this);
        }

        void ApplyHeal()
        {
            lifeManager?.Heal(owner, healPerTurn);
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
        }
    }
}
