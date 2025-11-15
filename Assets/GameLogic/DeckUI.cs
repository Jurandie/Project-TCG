using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class DeckUI : MonoBehaviour
    {
        [Header("Referências")]
        public Deck deck;
        public Text deckCountText;

        void Update()
        {
            if (deck != null && deckCountText != null)
                deckCountText.text = deck.CardsCount.ToString();
        }
    }
}
