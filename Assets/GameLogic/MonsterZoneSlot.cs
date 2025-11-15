using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    public class MonsterZoneSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Estado do Slot")]
        public bool IsOccupied { get; private set; } = false;

        private CardUI currentCard;
        private Image slotImage;

        [Header("Cores de Destaque")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color occupiedColor = new Color(1f, 0f, 0f, 0.3f);

        void Awake()
        {
            slotImage = GetComponent<Image>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (IsOccupied) return;

            var cardUI = eventData.pointerDrag?.GetComponent<CardUI>();
            if (cardUI != null)
            {
                IsOccupied = true;
                currentCard = cardUI;
                cardUI.SnapToSlot(this);
                UpdateVisual();
            }
        }

        public void ClearSlot()
        {
            IsOccupied = false;
            currentCard = null;
            UpdateVisual();
        }

        public bool IsEmpty() => !IsOccupied;

        void UpdateVisual()
        {
            if (slotImage == null) return;
            slotImage.color = IsOccupied ? occupiedColor : normalColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (slotImage == null || IsOccupied) return;
            slotImage.color = highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UpdateVisual();
        }
    }
}
