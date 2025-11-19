using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    [DisallowMultipleComponent]
    public class Deck : MonoBehaviour, IPointerClickHandler
    {
        [Header("Configuração do Deck")]
        public List<CardData> deckList = new List<CardData>();
        public bool shuffleOnStart = true;

        [Header("Identificação")]
        public bool isEnemyDeck = false;

        private List<CardData> drawPile = new List<CardData>();
        private List<CardData> discardPile = new List<CardData>();
        private System.Random rng = new System.Random();

        public event System.Action<CardData> OnCardDrawn;

        [Header("Visual da Pilha (UI)")]
        public Image cardBackImage;
        public GameObject cardBackPrefab;
        public Transform cardStackParent;
        public Sprite defaultCardBack;
        [Range(1, 20)] public int maxVisibleCards = 10;

        [Header("Cores do Estado")]
        public Color fullColor = Color.white;
        public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        public Color lichGlow = new Color(0.35f, 0.05f, 0.55f, 1f);

        [Header("Referências")]
        public DeckManager deckManager;

        [Header("Fumaça do Deck Player")]
        public ParticleSystem smokeEffect;

        private List<GameObject> visualStack = new List<GameObject>();
        private int totalCardsStart;
        private RectTransform rect;
        private float pulseTimer;
        private float rotationVariance = 1.4f;
        private float yOffset = -3f;
        private float cardScaleStart = 0.96f;
        private float cardScaleEnd = 1f;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        void Start()
        {
            BuildRuntimeDeckFromList();
            totalCardsStart = drawPile.Count;
            if (shuffleOnStart) Shuffle();
            RefreshVisual();
        }

        void Update()
        {
            pulseTimer += Time.deltaTime * 2f;
            float glow = (Mathf.Sin(pulseTimer) + 1f) * 0.15f;
            if (cardBackImage != null && isEnemyDeck)
                cardBackImage.color = Color.Lerp(fullColor, lichGlow, glow);
        }

        public void BuildRuntimeDeckFromList()
        {
            drawPile.Clear();
            discardPile.Clear();
            foreach (var data in deckList)
            {
                if (data == null) continue;
                int copies = Mathf.Max(1, data.copies);
                for (int i = 0; i < copies; i++)
                    drawPile.Add(data);
            }
        }

        public void Shuffle(int? seed = null)
        {
            if (seed.HasValue)
                rng = new System.Random(seed.Value);

            int n = drawPile.Count;
            for (int i = 0; i < n - 1; i++)
            {
                int j = rng.Next(i, n);
                var temp = drawPile[i];
                drawPile[i] = drawPile[j];
                drawPile[j] = temp;
            }

            RefreshVisual();
        }

        public CardData DrawOne()
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    RefreshVisual();
                    return null;
                }

                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle();
            }

            var top = drawPile[0];
            drawPile.RemoveAt(0);
            OnCardDrawn?.Invoke(top);
            RefreshVisual();
            return top;
        }

        public void Discard(CardData card)
        {
            if (card == null) return;
            discardPile.Add(card);
            RefreshVisual();
        }

        public void ResetDeck()
        {
            BuildRuntimeDeckFromList();
            Shuffle();
            discardPile.Clear();
            totalCardsStart = drawPile.Count;
            RefreshVisual();
        }

        public CardData TakeFromDiscard()
        {
            if (discardPile.Count == 0)
                return null;

            int last = discardPile.Count - 1;
            var card = discardPile[last];
            discardPile.RemoveAt(last);
            return card;
        }

        void RefreshVisual()
        {
            float fill = drawPile.Count / Mathf.Max(1f, (float)totalCardsStart);

            if (rect != null)
                rect.localScale = Vector3.one;

            if (isEnemyDeck)
            {
                if (cardBackImage != null)
                {
                    cardBackImage.color = Color.Lerp(emptyColor, fullColor, fill);
                    if (defaultCardBack != null)
                        cardBackImage.sprite = defaultCardBack;
                }

                UpdateStackVisuals(fill);
            }
            else
            {
                HandleSmokeEffect(fill);
                if (cardBackImage != null)
                    cardBackImage.enabled = false;

                foreach (var go in visualStack) Destroy(go);
                visualStack.Clear();
            }
        }

        void HandleSmokeEffect(float fill)
        {
            if (smokeEffect == null) return;

            var emission = smokeEffect.emission;
            emission.rateOverTime = Mathf.Lerp(2f, 40f, fill);

            var main = smokeEffect.main;
            float alpha = Mathf.Clamp01(fill);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, alpha));

            if (!smokeEffect.isPlaying && fill > 0f)
                smokeEffect.Play();
            if (fill <= 0f && smokeEffect.isPlaying)
                smokeEffect.Stop();
        }

        void UpdateStackVisuals(float fill)
        {
            if (!isEnemyDeck || cardStackParent == null || cardBackPrefab == null) return;

            foreach (var go in visualStack) Destroy(go);
            visualStack.Clear();

            int visibleCount = Mathf.Clamp(Mathf.FloorToInt(maxVisibleCards * fill), 0, maxVisibleCards);
            if (drawPile.Count == 0 || visibleCount <= 0)
            {
                if (cardBackImage != null) cardBackImage.enabled = false;
                return;
            }
            else if (cardBackImage != null) cardBackImage.enabled = true;

            for (int i = 0; i < visibleCount; i++)
            {
                GameObject cb = Instantiate(cardBackPrefab, cardStackParent);
                RectTransform rt = cb.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0, i * yOffset);
                    rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-rotationVariance, rotationVariance));
                    rt.localScale = Vector3.one * Mathf.Lerp(cardScaleStart, cardScaleEnd, visibleCount <= 1 ? 1f : i / (float)(visibleCount - 1));
                }

                Image img = cb.GetComponent<Image>();
                if (img != null)
                    img.color = Color.Lerp(lichGlow, fullColor, visibleCount <= 1 ? 1f : i / (float)(visibleCount - 1));

                cb.transform.SetAsFirstSibling();
                visualStack.Add(cb);
            }
        }

        public int CardsCount => drawPile.Count;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (deckManager == null)
            {
                Debug.LogWarning("[Deck] DeckManager não atribuído.");
                return;
            }

            if (isEnemyDeck)
            {
                StopAllCoroutines();
                StartCoroutine(ClickAnimation());
            }

            deckManager.RequestDrawFromDeck(this);
        }

        IEnumerator ClickAnimation()
        {
            if (rect == null) yield break;
            Vector3 originalScale = rect.localScale;
            Vector3 targetScale = originalScale * 1.05f;
            float t = 0f;
            while (t < 1f)
            {
                rect.localScale = Vector3.Lerp(originalScale, targetScale, t);
                t += Time.deltaTime * 8f;
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                rect.localScale = Vector3.Lerp(targetScale, originalScale, t);
                t += Time.deltaTime * 6f;
                yield return null;
            }

            rect.localScale = originalScale;
        }
    }
}
