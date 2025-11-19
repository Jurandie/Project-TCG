using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GameLogic
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Dados da Carta")]
        public Card runtimeCard;
        public CardData cardData;
        public int CurrentTier { get; private set; } = 1;

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
        CardVisuals cachedVisuals;
        CardVisuals lockedVisuals;
        bool hasLockedVisuals;
        bool transcendentStatsApplied;
        int currentDurability;
        bool durabilityDisabled;
        Coroutine moveCoroutine;
        Vector2 initialSize;
        bool hasInitialSize;
        [SerializeField] TranscendentAttackManager transcendentAttackManager;
        [SerializeField] SpellTargetSelectionManager spellTargetSelectionManager;
        TurnManager.TurnOwner ownerSide = TurnManager.TurnOwner.Player;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            rootCanvas = GetComponentInParent<Canvas>();
            EnsureUiReferences();
            CaptureInitialSize();
            if (transcendentAttackManager == null)
                transcendentAttackManager = FindFirstObjectByType<TranscendentAttackManager>();
            if (spellTargetSelectionManager == null)
                spellTargetSelectionManager = FindFirstObjectByType<SpellTargetSelectionManager>();
        }

        public void Setup(Card card)
        {
            runtimeCard = card;
            cardData = card != null ? card.SourceData : null;
            CurrentTier = card != null ? Mathf.Max(1, card.CurrentTier) : 1;
            hasLockedVisuals = false;
            transcendentStatsApplied = false;
            currentDurability = card != null ? card.Durability : (cardData != null ? cardData.durability : 0);
            durabilityDisabled = card != null && card.DurabilityDisabled;
            EnsureUiReferences();
            RefreshVisuals();
        }

        public void Setup(CardData data)
        {
            cardData = data;
            runtimeCard = data != null ? new Card(data) : null;
            CurrentTier = runtimeCard != null ? runtimeCard.CurrentTier : 1;
            hasLockedVisuals = false;
            transcendentStatsApplied = false;
            currentDurability = data != null ? data.durability : 0;
            durabilityDisabled = runtimeCard != null && runtimeCard.DurabilityDisabled;
            EnsureUiReferences();
            RefreshVisuals();
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

            RenderVisuals();

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

        void EnsureUiReferences()
        {
            if (nameText == null || statsText == null || artImage == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var tmp in texts)
                {
                    string lower = tmp.name.ToLowerInvariant();
                    if (nameText == null && (lower.Contains("name") || lower.Contains("title")))
                    {
                        nameText = tmp;
                        continue;
                    }
                    if (statsText == null && (lower.Contains("stat") || lower.Contains("atk")))
                    {
                        statsText = tmp;
                        continue;
                    }
                }

                if (artImage == null)
                {
                    foreach (var img in GetComponentsInChildren<Image>(true))
                    {
                        if (img == GetComponent<Image>()) continue;
                        if (img.name.ToLowerInvariant().Contains("art") || img.name.ToLowerInvariant().Contains("image"))
                        {
                            artImage = img;
                            break;
                        }
                    }
                }
            }

            if (artImage == null)
            {
                // como fallback absoluto pega qualquer Image filha
                Image[] imgs = GetComponentsInChildren<Image>(true);
                if (imgs.Length > 0)
                    artImage = imgs[imgs.Length - 1];
            }
        }

        void CaptureInitialSize()
        {
            if (rect == null || hasInitialSize)
                return;

            initialSize = rect.sizeDelta;
            hasInitialSize = true;
        }

        void RestoreInitialSize()
        {
            if (!hasInitialSize)
                return;

            rect.sizeDelta = initialSize;
            rect.localScale = Vector3.one;
        }

        public void ResetForHandContainer()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.localRotation = Quaternion.identity;
            RestoreInitialSize();
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
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    RestoreInitialSize();
                    rect.anchoredPosition = originalPos;
                }));
            }
        }

        public void SnapToSlot(MonsterZoneSlot slot)
        {
            currentSlot = slot;
            SmoothSnapToSlot(slot);
        }

        public MonsterZoneSlot CurrentSlot => currentSlot;
        public CardVisuals CurrentVisuals => cachedVisuals;
        public bool IsSpell => (cardData != null && cardData.cardType == CardData.CardType.Spell) ||
                               (runtimeCard != null && runtimeCard.Kind == CardData.CardType.Spell);
        public bool IsEquipment => (cardData != null && cardData.cardType == CardData.CardType.Equipment) ||
                                   (runtimeCard != null && runtimeCard.Kind == CardData.CardType.Equipment);
        public TurnManager.TurnOwner OwnerSide => ownerSide;

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
                rect.localRotation = Quaternion.identity;
                StartCoroutine(BounceEffect());
            }));
        }

        public bool UpgradeTier()
        {
            if (cardData == null)
                return false;

            int maxTier = cardData.GetMaxTier();
            if (CurrentTier >= maxTier)
                return false;

            CurrentTier++;
            hasLockedVisuals = false;
            transcendentStatsApplied = false;
            RefreshVisuals();
            return true;
        }

        void RefreshVisuals()
        {
            if (cardData != null)
            {
                if (hasLockedVisuals && lockedVisuals.tier == CurrentTier)
                {
                    cachedVisuals = lockedVisuals;
                }
                else
                {
                    cachedVisuals = cardData.GetVisualsForTier(CurrentTier);
                    if (cardData.IsTierRandomized(CurrentTier))
                    {
                        lockedVisuals = cachedVisuals;
                        hasLockedVisuals = true;
                    }
                }

                runtimeCard?.ApplyTier(cachedVisuals, CurrentTier);
            }
            else if (runtimeCard != null)
            {
                cachedVisuals = new CardVisuals
                {
                    name = runtimeCard.Name,
                    sprite = runtimeCard.Artwork,
                    attack = runtimeCard.Attack,
                    defense = runtimeCard.Defense,
                    tier = CurrentTier
                };
            }

            if (!isBackVisible)
                ShowFront();
        }

        public void ApplyTranscendentAttributes(TurnManager.TurnOwner owner, CharacterAttributeManager attributeManager, bool force = false)
        {
            if (attributeManager == null || transcendentStatsApplied || IsSpell)
                return;
            if (!force && !CurrentVisuals.isTranscendent)
                return;

            if (force && !CurrentVisuals.isTranscendent)
                cachedVisuals.isTranscendent = true;

            int roll = Random.Range(1, 21);
            var stats = attributeManager.GenerateTemporaryStatBlock(roll);
            if (stats == null)
                return;

            int atkHalf = Mathf.Max(1, stats.strength / 2);
            int defHalf = Mathf.Max(1, stats.dexterity / 2);
            int hpHalf = Mathf.Max(1, stats.maxLife / 2);
            int armorHalf = Mathf.Max(0, stats.constitution / 4);

            int newAttack = Random.Range(1, atkHalf + 1);
            int newDefense = Random.Range(1, defHalf + 1);
            int newHealth = Random.Range(1, hpHalf + 1);

            if (runtimeCard != null)
            {
                runtimeCard.Attack = newAttack;
                runtimeCard.Defense = newDefense;
                runtimeCard.MaxHealth = newHealth;
                runtimeCard.Armor = armorHalf;
            }

            cachedVisuals.attack = newAttack;
            cachedVisuals.defense = newDefense;
            cachedVisuals.health = newHealth;
            cachedVisuals.armor = armorHalf;

            if (IsEquipment)
            {
                durabilityDisabled = true;
                currentDurability = Mathf.Max(currentDurability, 0);
                if (runtimeCard != null)
                {
                    runtimeCard.DurabilityDisabled = true;
                    runtimeCard.Durability = currentDurability;
                }
            }

            transcendentStatsApplied = true;
            ShowFront();
        }

        bool UseEquipment()
        {
            if (!IsEquipment || durabilityDisabled)
                return false;

            if (currentDurability <= 0)
            {
                BreakEquipment();
                return true;
            }

            currentDurability = Mathf.Max(0, currentDurability - 1);
            if (runtimeCard != null)
                runtimeCard.Durability = currentDurability;

            Debug.Log($"[Equipment] Durabilidade restante: {currentDurability}");

            if (currentDurability <= 0)
                BreakEquipment();

            return true;
        }

        void BreakEquipment()
        {
            Debug.Log("[Equipment] Equipamento quebrou.");
            if (runtimeCard != null)
            {
                runtimeCard.Durability = 0;
                runtimeCard.DurabilityDisabled = true;
            }
            var slot = currentSlot;
            if (slot != null)
                slot.ClearSlot();

            Destroy(gameObject);
        }

        void RenderVisuals()
        {
            if (nameText != null)
                nameText.text = cachedVisuals.name ?? runtimeCard?.Name ?? cardData?.cardName ?? "";

            if (statsText != null)
                statsText.text = $"ATK {cachedVisuals.attack} / DEF {cachedVisuals.defense}";

            if (artImage != null)
                artImage.sprite = cachedVisuals.sprite ?? runtimeCard?.Artwork ?? cardData?.artwork;
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isBackVisible)
                return;

            if (IsEquipment)
            {
                if (UseEquipment())
                    eventData?.Use();
                return;
            }

            if (spellTargetSelectionManager != null && spellTargetSelectionManager.TrySelectMonster(this))
            {
                eventData?.Use();
                return;
            }

            if (transcendentAttackManager != null && CurrentVisuals.isTranscendent)
            {
                transcendentAttackManager.SelectCardForAttack(this);
                eventData?.Use();
            }
        }

        public void SetOwner(TurnManager.TurnOwner owner)
        {
            ownerSide = owner;
        }
    }
}
