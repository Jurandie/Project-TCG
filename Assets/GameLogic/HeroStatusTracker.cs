using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class HeroStatusTracker : MonoBehaviour
    {
        [System.Serializable]
        public class StatusEntry
        {
            public StatusEffectType type;
            public int remainingTurns;
            public int intensity;
            public string source;
        }

        public TurnManager.TurnOwner owner = TurnManager.TurnOwner.Player;

        [SerializeField] List<StatusEntry> statuses = new List<StatusEntry>();

        public System.Action<StatusEffectType, bool> OnStatusChanged;

        TurnManager turnManager;

        void Awake()
        {
            turnManager = FindFirstObjectByType<TurnManager>();
            if (turnManager != null)
                turnManager.OnTurnStarted += HandleTurn;
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= HandleTurn;
        }

        void HandleTurn(TurnManager.TurnOwner startedOwner)
        {
            if (startedOwner != owner)
                return;

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
                    }
                }
            }
        }

        public void AddStatus(StatusEffectType type, int duration, int intensity = 0, string source = null, bool refresh = true)
        {
            var entry = statuses.Find(s => s.type == type);
            if (entry != null)
            {
                if (refresh)
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

        public bool HasStatus(StatusEffectType type)
        {
            return statuses.Exists(s => s.type == type);
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

        public void ClearAll()
        {
            foreach (var status in statuses)
                OnStatusChanged?.Invoke(status.type, false);
            statuses.Clear();
        }
    }
}
