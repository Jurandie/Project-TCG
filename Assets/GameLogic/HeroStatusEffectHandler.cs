using UnityEngine;

namespace GameLogic
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeroStatusTracker))]
    public class HeroStatusEffectHandler : MonoBehaviour
    {
        public LifeManager lifeManager;
        public TurnManager turnManager;

        HeroStatusTracker tracker;
        bool poisoned;
        bool silenced;

        public bool IsSilenced => silenced;

        void Awake()
        {
            tracker = GetComponent<HeroStatusTracker>();
            if (lifeManager == null)
                lifeManager = FindFirstObjectByType<LifeManager>();
            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();

            if (tracker != null)
                tracker.OnStatusChanged += HandleStatus;
            if (turnManager != null)
                turnManager.OnTurnStarted += OnTurnStarted;
            if (lifeManager != null)
                lifeManager.OnModifyDamage += HandleModifyDamage;
        }

        void OnDestroy()
        {
            if (tracker != null)
                tracker.OnStatusChanged -= HandleStatus;
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
            if (lifeManager != null)
                lifeManager.OnModifyDamage -= HandleModifyDamage;
        }

        void HandleStatus(StatusEffectType type, bool added)
        {
            switch (type)
            {
                case StatusEffectType.Poisoned:
                    poisoned = added;
                    break;
                case StatusEffectType.Silenced:
                    silenced = added;
                    break;
            }
        }

        void OnTurnStarted(TurnManager.TurnOwner owner)
        {
            if (tracker == null || owner != tracker.owner)
                return;

            if (poisoned)
                ApplyPoisonTick();
        }

        void ApplyPoisonTick()
        {
            if (lifeManager == null || tracker == null)
                return;

            int currentHp = lifeManager.GetCurrentHP(tracker.owner);
            int baseHp = tracker.owner == TurnManager.TurnOwner.Player ? lifeManager.playerStartingHP : lifeManager.enemyStartingHP;
            int damage = Mathf.Max(1, Mathf.RoundToInt(baseHp * 0.1f));
            lifeManager.TakeDamage(tracker.owner, damage);
            Debug.Log($"[Status] {tracker.owner} sofre {damage} de dano por veneno.");
        }

        int HandleModifyDamage(TurnManager.TurnOwner target, int incoming)
        {
            if (tracker == null || target != tracker.owner || incoming <= 0)
                return incoming;

            if (tracker.HasStatus(StatusEffectType.Shielded))
            {
                int currentHp = lifeManager != null ? lifeManager.GetCurrentHP(target) : incoming;
                if (currentHp > 1 && incoming >= currentHp)
                {
                    tracker.RemoveStatus(StatusEffectType.Shielded);
                    Debug.Log("[Status] Escudo divino impede a morte do herói.");
                    return currentHp - 1;
                }
            }

            return incoming;
        }
    }
}
