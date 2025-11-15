using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Sorteia atributos em estilo D&D para jogador e inimigo e aplica ajustes
    /// de acordo com resultados criticos de um d20.
    /// </summary>
    public class CharacterAttributeManager : MonoBehaviour
    {
        public enum CharacterStatus
        {
            Normal,
            CriticalSuccessBonus,
            CriticalFailurePenalty
        }

        [System.Serializable]
        public class CharacterStatBlock
        {
            [Header("Atributos principais")]
            public int strength;
            public int dexterity;
            public int constitution;
            public int intelligence;
            public int wisdom;
            public int charisma;

            [Header("Resumo")]
            public int criticalRoll;
            public int totalAllocatedPoints;
            public CharacterStatus status;
            [TextArea]
            public string statusDescription;
            public int maxLife;

            public int GetAbilityScore(string key)
            {
                switch (key)
                {
                    case "STR": return strength;
                    case "DEX": return dexterity;
                    case "CON": return constitution;
                    case "INT": return intelligence;
                    case "WIS": return wisdom;
                    case "CHA": return charisma;
                    default: return 0;
                }
            }
        }

        [Header("Configuracao dos atributos")]
        [Tooltip("Total base de pontos disponiveis para cada personagem antes dos modificadores.")]
        public int basePointPool = 72;

        [Tooltip("Valor minimo permitido para cada atributo.")]
        public int minAttributeValue = 8;

        [Tooltip("Valor maximo permitido para cada atributo.")]
        public int maxAttributeValue = 18;

        [Tooltip("Vida base usada antes de aplicar o modificador de Constituicao.")]
        public int baseLife = 20;

        [Tooltip("Tamanho do dado de vida usado para converter o modificador de Constituicao em HP.")]
        public int hitDiceSize = 8;

        [Header("Integracao")]
        [Tooltip("Atualiza automaticamente o LifeManager com os HP calculados.")]
        public bool autoApplyLifeToManager = true;
        public LifeManager lifeManager;

        [Header("Debug")]
        public bool logResults = true;

        [SerializeField]
        private CharacterStatBlock playerStats = new CharacterStatBlock();

        [SerializeField]
        private CharacterStatBlock enemyStats = new CharacterStatBlock();

        private bool playerStatsReady;
        private bool enemyStatsReady;

        private static readonly string[] AttributeOrder = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

        public CharacterStatBlock GetStats(TurnManager.TurnOwner owner)
        {
            return owner == TurnManager.TurnOwner.Player ? playerStats : enemyStats;
        }

        public bool NeedsInitialRoll(TurnManager.TurnOwner owner)
        {
            return owner == TurnManager.TurnOwner.Player ? !playerStatsReady : !enemyStatsReady;
        }

        public bool HasStatsFor(TurnManager.TurnOwner owner)
        {
            return owner == TurnManager.TurnOwner.Player ? playerStatsReady : enemyStatsReady;
        }

        public bool TryApplyInitialRoll(TurnManager.TurnOwner owner, int diceResult)
        {
            if (!NeedsInitialRoll(owner))
                return false;

            var block = BuildStatBlock(owner, diceResult);

            if (owner == TurnManager.TurnOwner.Player)
            {
                playerStats = block;
                playerStatsReady = true;
            }
            else
            {
                enemyStats = block;
                enemyStatsReady = true;
            }

            if (autoApplyLifeToManager && lifeManager != null)
                lifeManager.ApplyExternalStartingHP(owner, block.maxLife);

            if (logResults)
                LogBlock(owner == TurnManager.TurnOwner.Player ? "PLAYER" : "ENEMY", block);

            return true;
        }

        [ContextMenu("Debug/Resetar atributos")]
        public void DebugResetAttributes()
        {
            playerStats = new CharacterStatBlock();
            enemyStats = new CharacterStatBlock();
            playerStatsReady = false;
            enemyStatsReady = false;
        }

        private CharacterStatBlock BuildStatBlock(TurnManager.TurnOwner owner, int diceResult)
        {
            var block = new CharacterStatBlock();
            block.criticalRoll = Mathf.Clamp(diceResult, 1, 20);
            ApplyStatusAndPointAdjustment(block);

            int attrCount = AttributeOrder.Length;
            int minTotal = minAttributeValue * attrCount;
            int maxTotal = maxAttributeValue * attrCount;

            if (basePointPool < minTotal && logResults)
                Debug.LogWarning($"[CharacterAttributeManager] basePointPool ({basePointPool}) menor que o minimo possivel ({minTotal}). Ajustando automaticamente.");

            block.totalAllocatedPoints = Mathf.Clamp(block.totalAllocatedPoints, minTotal, maxTotal);

            int[] values = new int[attrCount];
            for (int i = 0; i < attrCount; i++)
                values[i] = minAttributeValue;

            int remaining = block.totalAllocatedPoints - minTotal;
            int guard = 0;
            while (remaining > 0 && guard < 10000)
            {
                guard++;
                int index = Random.Range(0, attrCount);
                if (values[index] >= maxAttributeValue)
                    continue;

                values[index]++;
                remaining--;
            }

            block.strength = values[0];
            block.dexterity = values[1];
            block.constitution = values[2];
            block.intelligence = values[3];
            block.wisdom = values[4];
            block.charisma = values[5];
            block.maxLife = CalculateLifeFromCon(block.constitution);
            return block;
        }

        private void ApplyStatusAndPointAdjustment(CharacterStatBlock block)
        {
            int quarter = Mathf.Max(1, Mathf.RoundToInt(basePointPool * 0.25f));
            block.totalAllocatedPoints = basePointPool;

            if (block.criticalRoll == 20)
            {
                block.status = CharacterStatus.CriticalSuccessBonus;
                block.totalAllocatedPoints += quarter;
                block.statusDescription = $"Sucesso critico (d20): +{quarter} pontos";
            }
            else if (block.criticalRoll == 1)
            {
                block.status = CharacterStatus.CriticalFailurePenalty;
                block.totalAllocatedPoints = Mathf.Max(basePointPool - quarter, 0);
                block.statusDescription = $"Falha critica (d1): -{quarter} pontos";
            }
            else
            {
                block.status = CharacterStatus.Normal;
                block.statusDescription = "Status normal";
            }
        }

        private int CalculateLifeFromCon(int constitution)
        {
            int conModifier = Mathf.FloorToInt((constitution - 10) / 2f);
            int hp = baseLife + conModifier * hitDiceSize;
            return Mathf.Max(1, hp);
        }

        public int GetAbilityModifier(TurnManager.TurnOwner owner, string attributeKey)
        {
            var block = GetStats(owner);
            if (block == null) return 0;

            int score = block.GetAbilityScore(attributeKey);
            int modifier = Mathf.FloorToInt((score - 10) / 2f);
            return modifier;
        }

        private void LogBlock(string ownerLabel, CharacterStatBlock block)
        {
            Debug.Log($"[CharacterAttributeManager] {ownerLabel} roll={block.criticalRoll} status={block.status} pontos={block.totalAllocatedPoints} | " +
                $"STR {block.strength} | DEX {block.dexterity} | CON {block.constitution} | INT {block.intelligence} | WIS {block.wisdom} | CHA {block.charisma} | HP {block.maxLife}");
        }
    }
}
