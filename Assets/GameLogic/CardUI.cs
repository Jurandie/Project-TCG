using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GameLogic
{
    public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Dados da Carta")]
        public Card runtimeCard;
        public CardData cardData;

        [Header("Referências UI")]
        public TMP_Text nameText;
        public TMP_Text statsText;
        public Image artImage;

        [Header("Verso da Carta")]
        public Sprite cardBackSprite;
        public bool isBackVisible = false;

        [Header("Configurações de Exibição")]
        [Range(0.5f, 1f)] public float fitScale = 0.9f;
        [Range(0.05f, 0.3f)] public float snapDuration = 0.1f;
        [Range(1f, 1.2f)] public float bounceScale = 1.08f;
        [Range(0.05f, 0.2f)] public float bounceDuration = 0.08f;

        Canvas rootCanvas;
        RectTransform rect;
        CanvasGroup group;
        Transform originalParent;
        Vector2 originalPos;
        Vector2 dragOffset;
        MonsterZoneSlot currentSlot;
        Coroutine moveCoroutine;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void Setup(Card card)
        {
            runtimeCard = card;
            cardData = null;
            ShowFront();
        }

        public void Setup(CardData data)
        {
            cardData = data;
            runtimeCard = null;
            ShowFront();
        }

        public void SetupFromCardData(CardData data) => Setup(data);

        public void ShowFront()
        {
            isBackVisible = false;
            if (runtimeCard == null && cardData == null)
            {
                ClearUI();
                return;
            }

            if (runtimeCard != null)
            {
                if (nameText != null) nameText.text = runtimeCard.Name ?? "";
                if (statsText != null) statsText.text = $"ATK {runtimeCard.Attack} / DEF {runtimeCard.Defense}";
                if (artImage != null) artImage.sprite = runtimeCard.Artwork;
            }
            else
            {
                if (nameText != null) nameText.text = cardData.cardName ?? "";
                if (statsText != null) statsText.text = $"ATK {cardData.attack} / DEF {cardData.defense}";
                if (artImage != null) artImage.sprite = cardData.artwork;
            }

            if (artImage != null) artImage.color = Color.white;
        }

        public void ShowBack()
        {
            isBackVisible = true;
            if (nameText != null) nameText.text = "";
            if (statsText != null) statsText.text = "";
            if (artImage != null)
            {
                artImage.sprite = cardBackSprite;
                artImage.color = Color.white;
            }
        }

        void ClearUI()
        {
            if (nameText != null) nameText.text = "";
            if (statsText != null) statsText.text = "";
            if (artImage != null) artImage.sprite = null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isBackVisible) return;
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);

            originalParent = transform.parent;
            originalPos = rect.anchoredPosition;
            group.blocksRaycasts = false;

            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
                transform.SetAsLastSibling();
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                rootCanvas != null ? rootCanvas.worldCamera : null,
                out var localPoint);

            dragOffset = rect.anchoredPosition - localPoint;

            if (currentSlot != null)
            {
                currentSlot.ClearSlot();
                currentSlot = null;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isBackVisible) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                rootCanvas != null ? rootCanvas.worldCamera : null,
                out var pos))
            {
                rect.anchoredPosition = pos + dragOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isBackVisible) return;
            group.blocksRaycasts = true;

            if (currentSlot != null)
            {
                SmoothSnapToSlot(currentSlot);
            }
            else
            {
                moveCoroutine = StartCoroutine(SmoothMove(rect.anchoredPosition, originalPos, snapDuration, () =>
                {
                    transform.SetParent(originalParent);
                    rect.anchoredPosition = originalPos;
                }));
            }
        }

        public void SnapToSlot(MonsterZoneSlot slot)
        {
            currentSlot = slot;
            SmoothSnapToSlot(slot);
        }

        void SmoothSnapToSlot(MonsterZoneSlot slot)
        {
            currentSlot = slot;
            transform.SetParent(slot.transform, false);
            RectTransform slotRect = slot.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 targetSize = slotRect.rect.size * fitScale;

            moveCoroutine = StartCoroutine(SmoothMove(rect.anchoredPosition, Vector2.zero, snapDuration, () =>
            {
                rect.sizeDelta = targetSize;
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
                StartCoroutine(BounceEffect());
            }));
        }

        IEnumerator SmoothMove(Vector2 from, Vector2 to, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = 1f - Mathf.Pow(1f - t, 3f);
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                yield return null;
            }

            rect.anchoredPosition = to;
            onComplete?.Invoke();
        }

        IEnumerator BounceEffect()
        {
            Vector3 startScale = Vector3.one;
            Vector3 peakScale = Vector3.one * bounceScale;
            float elapsed = 0f;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - elapsed / bounceDuration, 3f);
                rect.localScale = Vector3.Lerp(startScale, peakScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - elapsed / bounceDuration, 3f);
                rect.localScale = Vector3.Lerp(peakScale, startScale, t);
                yield return null;
            }

            rect.localScale = Vector3.one;
        }
    }
}
