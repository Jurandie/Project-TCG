using UnityEngine;

namespace GameLogic
{
    public class CardFactory : MonoBehaviour
    {
        [Header("Prefab da Carta")]
        public GameObject cardPrefab;

        public CardUI CreateCard(CardData data, Transform parent)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[CardFactory] cardPrefab não atribuído.");
                return null;
            }

            if (data == null)
            {
                Debug.LogWarning("[CardFactory] CardData é nulo.");
                return null;
            }

            GameObject go = Instantiate(cardPrefab, parent);
            go.SetActive(true);

            var ui = go.GetComponent<CardUI>();
            if (ui == null)
            {
                Debug.LogError("[CardFactory] Prefab não contém CardUI.");
                Destroy(go);
                return null;
            }

            ui.SetupFromCardData(data);
            return ui;
        }
    }
}
