using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "CardData", menuName = "Card Game/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("Identidade")]
        public string cardName = "Nova Carta";
        public Sprite artwork;
        public Sprite cardBackOverride;

        [Header("Status")]
        public int attack = 1;
        public int defense = 1;

        [Header("Cópias")]
        [Tooltip("Usado por Deck.cs para gerar o baralho runtime.")]
        public int copies = 1;

        public int GetClampedAttack() => Mathf.Max(0, attack);
        public int GetClampedDefense() => Mathf.Max(0, defense);
    }
}
