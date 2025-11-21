using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    [DisallowMultipleComponent]
    public class CardStatusTracker : MonoBehaviour
    {
        [System.Serializable]
        public class StatusEntry
        {
            public StatusEffectType type;
            public int remainingTurns;
            public int intensity;
            public string source;
        }

        [SerializeField] List<StatusEntry> statuses = new List<StatusEntry>();

        public IReadOnlyList<StatusEntry> Statuses => statuses;

        public System.Action<StatusEffectType, bool> OnStatusChanged;

        CardUI card;
        TurnManager turnManager;

        void Awake()
        {
            card = GetComponent<CardUI>();
            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();
            if (turnManager != null)
                turnManager.OnTurnStarted += OnTurnStarted;
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
        }

        void OnTurnStarted(TurnManager.TurnOwner owner)
        {
            if (card == null || owner != card.OwnerSide)
                return;

            bool anyRemoved = false;
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                var entry = statuses[i];
                if (entry.remainingTurns > 0)
                {
                    entry.remainingTurns--;
                    if (entry.remainingTurns <= 0)
                    {
                        statuses.RemoveAt(i);
                        OnStatusChanged?.Invoke(entry.type, false);
                        anyRemoved = true;
                    }
                }
            }

            if (anyRemoved && statuses.Count == 0)
                statuses.Clear();
        }

        public void AddStatus(StatusEffectType type, int duration, int intensity = 0, string source = null, bool refreshDuration = true)
        {
            if (type == StatusEffectType.None)
                return;

            var entry = statuses.Find(s => s.type == type);
            if (entry != null)
            {
                if (refreshDuration)
                    entry.remainingTurns = duration;
                else
                    entry.remainingTurns = Mathf.Max(entry.remainingTurns, duration);

                entry.intensity = intensity;
                entry.source = source;
            }
            else
            {
                statuses.Add(new StatusEntry
                {
                    type = type,
                    remainingTurns = duration,
                    intensity = intensity,
                    source = source
                });
                OnStatusChanged?.Invoke(type, true);
            }
        }

        public void RemoveStatus(StatusEffectType type)
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i].type == type)
                {
                    statuses.RemoveAt(i);
                    OnStatusChanged?.Invoke(type, false);
                    break;
                }
            }
        }

        public bool HasStatus(StatusEffectType type)
        {
            return statuses.Exists(s => s.type == type);
        }

        public int GetIntensity(StatusEffectType type)
        {
            var entry = statuses.Find(s => s.type == type);
            return entry != null ? entry.intensity : 0;
        }

        public void ClearAll()
        {
            if (statuses.Count == 0)
                return;

            foreach (var status in statuses)
                OnStatusChanged?.Invoke(status.type, false);

            statuses.Clear();
        }
    }
}
