using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// EnergyManager compativel com varias versoes de chamadas do projeto.
    /// Atualizado para evitar o uso obsoleto de FindObjectOfType.
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        [Header("Configuracao basica")]
        [Tooltip("Energia inicial de cada lado no inicio do jogo")]
        public int startingEnergy = 5;

        [Tooltip("Energia maxima (limite visual/UI)")]
        public int maxEnergy = 10;

        [Tooltip("Custo base (multiplicador 2^n aplicado sobre este)")]
        public int baseCost = 1;

        [Header("Estado (visivel no Inspector)")]
        public int playerEnergy;
        public int enemyEnergy;

        private int playerConsecutiveRolls = 0;
        private int enemyConsecutiveRolls = 0;

        [Header("UI")]
        public EnergyUIManager energyUiManager;
        public event System.Action<int, int> OnEnergyChanged;

        void Awake()
        {
            if (energyUiManager == null)
            {
#if UNITY_2023_1_OR_NEWER
                energyUiManager = FindFirstObjectByType<EnergyUIManager>();
#else
                var found = FindObjectOfType(typeof(EnergyUIManager));
                energyUiManager = found as EnergyUIManager;
#endif
            }
        }

        void Start()
        {
            ResetAll();
            RefreshUI();
        }

        public void ResetAll()
        {
            playerEnergy = Mathf.Clamp(startingEnergy, 0, maxEnergy);
            enemyEnergy = Mathf.Clamp(startingEnergy, 0, maxEnergy);
            playerConsecutiveRolls = 0;
            enemyConsecutiveRolls = 0;
            RefreshUI();
        }

        public bool CanRollDice(TurnManager.TurnOwner owner)
        {
            return HasEnoughEnergy(owner, GetNextRollCost(owner));
        }

        public bool ConsumeEnergyForRoll(TurnManager.TurnOwner owner)
        {
            int cost = GetNextRollCost(owner);
            if (!HasEnoughEnergy(owner, cost)) return false;

            if (owner == TurnManager.TurnOwner.Player)
            {
                playerEnergy = Mathf.Max(0, playerEnergy - cost);
                playerConsecutiveRolls++;
            }
            else
            {
                enemyEnergy = Mathf.Max(0, enemyEnergy - cost);
                enemyConsecutiveRolls++;
            }

            RefreshUI();
            if (Debug.isDebugBuild) Debug.Log($"[EnergyManager] {owner} consumiu {cost} energia. (restam: {GetCurrentEnergy(owner)})");
            return true;
        }

        public void AddEnergy(TurnManager.TurnOwner owner, int amount)
        {
            if (amount <= 0) return;
            if (owner == TurnManager.TurnOwner.Player)
                playerEnergy = Mathf.Clamp(playerEnergy + amount, 0, maxEnergy);
            else
                enemyEnergy = Mathf.Clamp(enemyEnergy + amount, 0, maxEnergy);

            RefreshUI();
            if (Debug.isDebugBuild) Debug.Log($"[EnergyManager] {owner} recebeu +{amount} energia. (total: {GetCurrentEnergy(owner)})");
        }

        public void EndTurn(TurnManager.TurnOwner owner)
        {
            if (owner == TurnManager.TurnOwner.Player)
                playerConsecutiveRolls = 0;
            else
                enemyConsecutiveRolls = 0;

            if (Debug.isDebugBuild) Debug.Log($"[EnergyManager] EndTurn({owner}) - consecutive rolls reset.");
            RefreshUI();
        }

        public bool HasEnoughEnergy(TurnManager.TurnOwner owner, int required)
        {
            return GetCurrentEnergy(owner) >= required;
        }

        public bool SpendEnergy(TurnManager.TurnOwner owner)
        {
            return ConsumeEnergyForRoll(owner);
        }

        public int GetNextRollCost(TurnManager.TurnOwner owner)
        {
            int count = (owner == TurnManager.TurnOwner.Player) ? playerConsecutiveRolls : enemyConsecutiveRolls;
            long raw = (long)baseCost * (1L << count);
            int cost = (int)Mathf.Clamp(raw, 1, Mathf.Max(1, maxEnergy));
            return cost;
        }

        public int GetCurrentEnergy(TurnManager.TurnOwner owner)
        {
            return (owner == TurnManager.TurnOwner.Player) ? playerEnergy : enemyEnergy;
        }

        public void SetCurrentEnergy(TurnManager.TurnOwner owner, int value)
        {
            if (owner == TurnManager.TurnOwner.Player)
                playerEnergy = Mathf.Clamp(value, 0, maxEnergy);
            else
                enemyEnergy = Mathf.Clamp(value, 0, maxEnergy);

            RefreshUI();
        }

        private void RefreshUI()
        {
            OnEnergyChanged?.Invoke(playerEnergy, enemyEnergy);
            if (energyUiManager != null)
                energyUiManager.UpdateEnergyDisplay(playerEnergy, enemyEnergy);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Gasta 1 energia Player")]
        private void DebugSpendPlayer()
        {
            ConsumeEnergyForRoll(TurnManager.TurnOwner.Player);
        }

        [ContextMenu("Debug: Gasta 1 energia Enemy")]
        private void DebugSpendEnemy()
        {
            ConsumeEnergyForRoll(TurnManager.TurnOwner.Enemy);
        }

        [ContextMenu("Debug: Reset All")]
        private void DebugResetAll()
        {
            ResetAll();
        }
#endif
    }
}
