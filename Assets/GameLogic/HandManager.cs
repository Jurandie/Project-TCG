using UnityEngine;

namespace GameLogic
{
    public class HandManager : MonoBehaviour
    {
        [Header("Referências de Mãos")]
        public Transform playerHandContainer;
        public Transform enemyHandContainer;

        [Header("Prefab e Arte")]
        public GameObject cardPrefab;
        public Sprite cardBackSprite;

        [Header("Referências de Deck")]
        public DeckManager deckManager;

        [Header("Configurações")]
        public int playerHandSize = 5;
        public int enemyHandSize = 5;
        public bool hideEnemyCards = true;

        void Start()
        {
            if (deckManager != null && deckManager.playerDeck != null)
            {
                var firstCard = deckManager.playerDeck.DrawOne();
                if (firstCard != null)
                {
                    AddCardToHand(firstCard, false);
                    Debug.Log("[HandManager] Jogador comprou 1 carta no início da partida.");
                }
            }
        }

        public void GenerateHandFromCardDataArray(Transform container, CardData[] dataArray, bool isEnemy)
        {
            if (container == null || dataArray == null) return;
            foreach (var data in dataArray)
                if (data != null)
                    AddCardToHand(data, isEnemy);
        }

        public void AddCardToHand(CardData data, bool isEnemy = false)
        {
            if (data == null)
            {
                Debug.LogWarning("[HandManager] CardData nulo.");
                return;
            }

            SpawnCardUI(isEnemy, (ui) => ui.Setup(data));
        }

        public void AddCardToHand(Card card, bool isEnemy = false)
        {
            if (card == null)
            {
                Debug.LogWarning("[HandManager] Card nulo.");
                return;
            }

            SpawnCardUI(isEnemy, (ui) => ui.Setup(card));
        }

        void SpawnCardUI(bool isEnemy, System.Action<CardUI> configure)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[HandManager] cardPrefab não atribuído.");
                return;
            }

            Transform container = isEnemy ? enemyHandContainer : playerHandContainer;
            if (container == null)
            {
                Debug.LogWarning("[HandManager] Container de mão não atribuído.");
                return;
            }

            GameObject go = Instantiate(cardPrefab, container);
            go.SetActive(true);

            var ui = go.GetComponent<CardUI>();
            if (ui == null)
            {
                Debug.LogError("[HandManager] Prefab não possui CardUI.");
                Destroy(go);
                return;
            }

            configure?.Invoke(ui);
            ui.cardBackSprite = cardBackSprite;

            if (isEnemy && hideEnemyCards)
                ui.ShowBack();
            else
                ui.ShowFront();
        }

        public void ClearHand(bool isEnemy = false)
        {
            Transform container = isEnemy ? enemyHandContainer : playerHandContainer;
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        public int CountHand(bool isEnemy = false)
        {
            Transform container = isEnemy ? enemyHandContainer : playerHandContainer;
            return container == null ? 0 : container.childCount;
        }
    }
}
