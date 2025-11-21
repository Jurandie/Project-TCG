using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// Detects a long press over a UI element and shows the character sheet without triggering other input.
    /// </summary>
    public class HoldInspectHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler
    {
        [Tooltip("Tempo (em segundos) que o usuário precisa segurar para abrir a ficha.")]
        public float holdDuration = 0.6f;

        [Tooltip("Painel responsável por exibir as informações.")]
        public CharacterSheetDisplay sheetDisplay;

        [Tooltip("Define se este handler representa um herói ao invés de uma carta.")]
        public bool inspectHero;

        public TurnManager.TurnOwner heroOwner = TurnManager.TurnOwner.Player;

        [SerializeField] CardUI cardUI;

        bool pointerDown;
        bool sheetVisible;
        float holdTimer;
        bool blockClick;
        Vector2 lastPointerPosition;

        void Awake()
        {
            if (sheetDisplay == null)
                sheetDisplay = FindFirstObjectByType<CharacterSheetDisplay>();
            if (cardUI == null)
                cardUI = GetComponent<CardUI>();
        }

        void Update()
        {
            if (!pointerDown || sheetVisible)
                return;

            holdTimer += Time.unscaledDeltaTime;
            if (holdTimer >= holdDuration)
            {
                pointerDown = false;
                if (!CanInspect())
                {
                    sheetVisible = false;
                    blockClick = false;
                    return;
                }

                sheetVisible = true;
                blockClick = true;
                ShowSheet();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDown = true;
            sheetVisible = false;
            holdTimer = 0f;
            lastPointerPosition = eventData != null ? eventData.position : Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (sheetVisible)
            {
                eventData?.Use();
                HideSheet();
            }

            pointerDown = false;
            sheetVisible = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelHold();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            CancelHold();
        }

        public void CancelHold()
        {
            if (sheetVisible)
                HideSheet();

            pointerDown = false;
            sheetVisible = false;
        }

        bool CanInspect()
        {
            if (inspectHero)
                return heroOwner == TurnManager.TurnOwner.Player;

            if (cardUI != null)
                return cardUI.OwnerSide == TurnManager.TurnOwner.Player;

            return false;
        }

        void ShowSheet()
        {
            if (sheetDisplay == null)
                return;

            if (inspectHero)
                sheetDisplay.ShowHero(heroOwner, lastPointerPosition);
            else if (cardUI != null)
                sheetDisplay.ShowCard(cardUI, lastPointerPosition);
        }

        void HideSheet()
        {
            sheetDisplay?.Hide();
        }

        /// <summary>
        /// Returns true once when the handler consumed the click (i.e. a long press happened).
        /// </summary>
        public bool ConsumeClickBlock()
        {
            if (!blockClick)
                return false;

            blockClick = false;
            return true;
        }
    }
}
