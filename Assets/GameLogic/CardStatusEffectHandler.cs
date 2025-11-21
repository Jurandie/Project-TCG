using UnityEngine;

namespace GameLogic
{
    [RequireComponent(typeof(CardStatusTracker))]
    [DisallowMultipleComponent]
    public class CardStatusEffectHandler : MonoBehaviour
    {
        CardUI card;
        CardStatusTracker tracker;
        TurnManager turnManager;
        bool poisoned;
        bool silenced;
        bool stunned;
        bool shielded;
        bool auraResist;

        public bool IsSilenced => silenced;
        public bool IsStunned => stunned;

        void Awake()
        {
            card = GetComponent<CardUI>();
            tracker = GetComponent<CardStatusTracker>();
            turnManager = FindFirstObjectByType<TurnManager>();

            if (tracker != null)
                tracker.OnStatusChanged += HandleStatusChanged;
            if (turnManager != null)
                turnManager.OnTurnStarted += OnTurnStarted;
        }

        void OnDestroy()
        {
            if (tracker != null)
                tracker.OnStatusChanged -= HandleStatusChanged;
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
        }

        void HandleStatusChanged(StatusEffectType type, bool added)
        {
            switch (type)
            {
                case StatusEffectType.Poisoned:
                    poisoned = added;
                    break;
                case StatusEffectType.Silenced:
                    silenced = added;
                    if (added)
                        RemoveActiveBuffs();
                    break;
                case StatusEffectType.Stunned:
                    stunned = added;
                    break;
                case StatusEffectType.Shielded:
                    shielded = added;
                    break;
                case StatusEffectType.AuraSuppressed:
                    auraResist = added;
                    break;
            }
        }

        void OnTurnStarted(TurnManager.TurnOwner owner)
        {
            if (card == null || owner != card.OwnerSide)
                return;

            if (poisoned)
                ApplyPoisonTick();
        }

        void ApplyPoisonTick()
        {
            if (card == null)
                return;

            var runtime = card.runtimeCard;
            int maxHp = runtime != null ? Mathf.Max(1, runtime.MaxHealth) : Mathf.Max(1, card.CurrentVisuals.health);
            int damage = Mathf.Max(1, Mathf.RoundToInt(maxHp * 0.1f));

            if (runtime != null)
            {
                runtime.Defense = Mathf.Max(0, runtime.Defense - damage);
                card.Setup(runtime);
            }

            card.ShowFront();
            Debug.Log($"[Status] {card.name} sofre {damage} de dano por veneno.");

            if (runtime != null && runtime.Defense <= 0)
            {
                var slot = card.CurrentSlot;
                if (slot != null)
                    slot.ClearSlot();
                Destroy(card.gameObject);
            }
        }

        void RemoveActiveBuffs()
        {
            var buffs = GetComponents<CardStatBuff>();
            foreach (var buff in buffs)
                Destroy(buff);
        }
    }
}
