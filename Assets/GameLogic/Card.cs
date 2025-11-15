using UnityEngine;

namespace GameLogic
{
    [System.Serializable]
    public class Card
    {
        public string Name;
        public int Attack;
        public int Defense;
        public Sprite Artwork;
        public Sprite CardBack;

        public Card() { }

        public Card(CardData data)
        {
            if (data == null) return;
            Name = data.cardName;
            Attack = data.attack;
            Defense = data.defense;
            Artwork = data.artwork;
            CardBack = data.cardBackOverride;
        }
    }
}
