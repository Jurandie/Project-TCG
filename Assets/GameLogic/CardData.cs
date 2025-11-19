using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "CardData", menuName = "Card Game/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        public enum CardType
        {
            Monster,
            Spell,
            Equipment
        }

        [Header("Identidade")]
        public string cardName = "Nova Carta";
        public Sprite artwork;
        public Sprite cardBackOverride;

        [Header("Status")]
        public int attack = 1;
        public int defense = 1;

        [Header("Ficha / Atributos")]
        public int maxHealth = 10;
        public int armor = 0;
        public int energyCost = 1;
        [Tooltip("Palavras-chave / tags exibidas na carta.")]
        public string[] keywords;
        [TextArea]
        public string loreDescription;

        [Header("Tipo")]
        public CardType cardType = CardType.Monster;

        [Header("Feitiço (apenas quando tipo = Spell)")]
        public CardSpellEffect spellEffect;
        [Tooltip("Após aplicar o efeito, a carta é destruída.")]
        public bool destroyAfterCast = true;

        [Header("Equipamento")]
        [Tooltip("Durabilidade base do equipamento (quantas utilizações antes de quebrar).")]
        public int durability = 0;
        [Tooltip("Define se o equipamento pode ascender para uma forma transcendental.")]
        public bool isTranscendentFormAvailable = false;

        [Header("Evolução")]
        [Tooltip("Opções de evolução (tier 1 = base). Deixe vazio para usar apenas os valores padrões.")]
        public CardEvolutionStage[] evolutionStages;

        [Header("Cópias")]
        [Tooltip("Usado por Deck.cs para gerar o baralho runtime.")]
        public int copies = 1;

        public int GetClampedAttack() => Mathf.Max(0, attack);
        public int GetClampedDefense() => Mathf.Max(0, defense);

        public int GetMaxTier() => 1 + (evolutionStages != null ? evolutionStages.Length : 0);

        public CardEvolutionStage GetStage(int tier)
        {
            if (tier <= 1 || evolutionStages == null || evolutionStages.Length == 0)
                return null;

            int index = Mathf.Clamp(tier - 2, 0, evolutionStages.Length - 1);
            return evolutionStages[index];
        }

        public CardVisuals GetVisualsForTier(int tier)
        {
            CardVisuals result = new CardVisuals
            {
                name = cardName,
                sprite = artwork,
                attack = attack,
                defense = defense,
                health = maxHealth,
                armor = armor,
                energyCost = energyCost,
                keywords = keywords,
                description = loreDescription,
                tier = tier,
                randomized = false,
                isTranscendent = false,
                autoAttackOnTranscend = false
            };

            var stage = GetStage(tier);
            if (stage == null)
                return result;

            if (!string.IsNullOrEmpty(stage.displayName))
                result.name = stage.displayName;
            if (stage.artwork != null)
                result.sprite = stage.artwork;

            result.attack = stage.attack > 0 ? stage.attack : result.attack;
            result.defense = stage.defense > 0 ? stage.defense : result.defense;

            if (stage.overrideHealth)
                result.health = Mathf.Max(1, stage.health);
            if (stage.overrideArmor)
                result.armor = Mathf.Max(0, stage.armor);
            if (stage.overrideEnergyCost)
                result.energyCost = Mathf.Max(0, stage.energyCost);
            if (!string.IsNullOrEmpty(stage.description))
                result.description = stage.description;
            if (stage.keywords != null && stage.keywords.Length > 0)
                result.keywords = stage.keywords;

            if (stage.randomizeStats)
            {
                result.attack = Random.Range(stage.attackRange.x, stage.attackRange.y + 1);
                result.defense = Random.Range(stage.defenseRange.x, stage.defenseRange.y + 1);
                result.randomized = true;
            }

            result.isTranscendent = stage.transcendent;
            result.autoAttackOnTranscend = stage.autoAttackOnTranscend;
            return result;
        }

        public bool IsTierRandomized(int tier)
        {
            var stage = GetStage(tier);
            return stage != null && stage.randomizeStats;
        }
    }

    [System.Serializable]
    public class CardEvolutionStage
    {
        public string displayName;
        public Sprite artwork;
        public int attack;
        public int defense;
        public bool randomizeStats;
        public Vector2Int attackRange = new Vector2Int(5, 15);
        public Vector2Int defenseRange = new Vector2Int(5, 15);
        public bool overrideHealth;
        public int health = 10;
        public bool overrideArmor;
        public int armor;
        public bool overrideEnergyCost;
        public int energyCost = 1;
        [TextArea] public string description;
        public string[] keywords;
        public bool transcendent;
        public bool autoAttackOnTranscend = true;
    }

    public struct CardVisuals
    {
        public string name;
        public Sprite sprite;
        public int attack;
        public int defense;
        public int health;
        public int armor;
        public int energyCost;
        public string[] keywords;
        public string description;
        public int tier;
        public bool randomized;
        public bool isTranscendent;
        public bool autoAttackOnTranscend;
    }
}
