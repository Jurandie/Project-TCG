using UnityEngine;

namespace GameLogic
{
    public class CardBanishEffect : MonoBehaviour
    {
        CardUI card;
        MonsterZoneSlot originalSlot;
        TurnManager turnManager;
        TurnManager.TurnOwner owner;
        int remainingTurns;
        bool pendingReturn;
        bool initialized;

        public void Initialize(CardUI targetCard, MonsterZoneSlot slot, int turns, TurnManager manager, TurnManager.TurnOwner slotOwner)
        {
            card = targetCard;
            originalSlot = slot;
            remainingTurns = Mathf.Max(1, turns);
            turnManager = manager ?? FindFirstObjectByType<TurnManager>();
            owner = slotOwner;

            if (card == null || originalSlot == null)
            {
                Destroy(gameObject);
                return;
            }

            originalSlot.ClearSlot();
            HideCard();

            if (turnManager != null)
                turnManager.OnTurnStarted += OnTurnStarted;

            initialized = true;
        }

        void HideCard()
        {
            var canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            card.gameObject.SetActive(false);
        }

        void RestoreCard()
        {
            card.gameObject.SetActive(true);
            var canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
        }

        void OnTurnStarted(TurnManager.TurnOwner owner)
        {
            if (!initialized || card == null)
                return;

            if (owner != card.OwnerSide)
                return;

            remainingTurns--;
            if (remainingTurns <= 0 && !pendingReturn)
            {
                pendingReturn = true;
                ReturnToField();
            }
        }

        void ReturnToField()
        {
            if (originalSlot == null || card == null)
            {
                Destroy(gameObject);
                return;
            }

            RestoreCard();
            if (!originalSlot.TryPlaceCard(card))
            {
                card.transform.SetParent(originalSlot.transform.parent, false);
                card.transform.SetAsLastSibling();
            }

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
            if (pendingReturn)
                return;
            if (card != null && !card.gameObject.activeSelf)
                RestoreCard();
        }
    }
}
