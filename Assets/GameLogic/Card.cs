using UnityEngine;

namespace GameLogic
{
    [System.Serializable]
    public class Card
    {
        public string Name;
        public int Attack;
        public int Defense;
        public int MaxHealth;
        public int Armor;
        public int EnergyCost = 1;
        public string[] Keywords;
        public string LoreDescription;
        public Sprite Artwork;
        public Sprite CardBack;
        public Sprite sacredArtworkOverride;
        public Sprite PreviewBoard;
        public CardData.CardType Kind = CardData.CardType.Monster;
        public int CurrentTier = 1;
        public CardData SourceData;
        public int Durability;
        public bool IsTranscendentFormAvailable;
        public bool DurabilityDisabled;
        public bool IsSacredSpell;
        public bool IsUndead;
        public string PreviewDescription;

        public Card() { }

        public Card(CardData data)
        {
            if (data == null) return;
            SourceData = data;
            Name = data.cardName;
            Attack = data.attack;
            Defense = data.defense;
            MaxHealth = data.maxHealth;
            Armor = data.armor;
            EnergyCost = data.energyCost;
            Keywords = data.keywords;
            LoreDescription = data.loreDescription;
            PreviewDescription = data.previewDescription;
            Artwork = data.artwork;
            PreviewBoard = data.previewBoard;
            CardBack = data.cardBackOverride;
            Kind = data.cardType;
            IsUndead = data.isUndead;
            Durability = data.durability;
            IsTranscendentFormAvailable = data.isTranscendentFormAvailable;
            DurabilityDisabled = false;
            IsSacredSpell = false;
            sacredArtworkOverride = null;
        }

        public void ApplyTier(CardVisuals visuals, int tier)
        {
            CurrentTier = tier;
            Name = visuals.name;
            Attack = visuals.attack;
            Defense = visuals.defense;
            MaxHealth = visuals.health > 0 ? visuals.health : MaxHealth;
            Armor = visuals.armor;
            EnergyCost = visuals.energyCost;
            if (visuals.keywords != null && visuals.keywords.Length > 0)
                Keywords = visuals.keywords;
            if (!string.IsNullOrEmpty(visuals.description))
                LoreDescription = visuals.description;
            Artwork = visuals.sprite;
        }
    }
}
