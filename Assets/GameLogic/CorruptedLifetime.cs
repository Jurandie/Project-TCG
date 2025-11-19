using UnityEngine;

namespace GameLogic
{
    public class CorruptedLifetime : MonoBehaviour
    {
        [SerializeField] int turnsRemaining = 2;

        TurnManager turnManager;
        MonsterZoneSlot slot;
        TurnManager.TurnOwner owner;
        bool initialized;

        public void Initialize(TurnManager manager, MonsterZoneSlot targetSlot, int lifetime, TurnManager.TurnOwner slotOwner)
        {
            turnManager = manager;
            slot = targetSlot;
            turnsRemaining = Mathf.Max(1, lifetime);
            owner = slotOwner;
            if (turnManager != null)
                turnManager.OnTurnStarted += OnTurnStarted;
            initialized = true;
        }

        void OnTurnStarted(TurnManager.TurnOwner startedOwner)
        {
            if (!initialized || startedOwner != owner)
                return;

            turnsRemaining--;
            if (turnsRemaining <= 0)
                Explode();
        }

        void Explode()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;

            if (slot != null)
                slot.ClearSlot();

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
        }
    }
}
