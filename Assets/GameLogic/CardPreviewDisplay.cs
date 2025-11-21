using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class CardPreviewDisplay : MonoBehaviour
    {
        static CardPreviewDisplay instance;

        [Header("UI")]
        [SerializeField] RectTransform panelRoot;
        [SerializeField] Canvas rootCanvas;
        [SerializeField] Image artworkImage;
        [SerializeField] Image boardImage;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] Vector2 anchoredPosition = new Vector2(0f, 0f);
        [Range(1f, 2f)] public float artworkScale = 1.2f;

        CardUI currentCard;
        Vector2 baseArtworkSize;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (panelRoot == null && rootCanvas != null)
                panelRoot = rootCanvas.transform as RectTransform;

            if (artworkImage != null)
                baseArtworkSize = artworkImage.rectTransform.sizeDelta;

            Hide();
        }

        public static CardPreviewDisplay EnsureInstance()
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<CardPreviewDisplay>();
            if (instance == null)
            {
                Debug.LogWarning("[CardPreview] Nenhum CardPreviewDisplay ativo na cena.");
                return null;
            }

            instance.Hide();
            return instance;
        }

        public void Show(CardUI card)
        {
            if (card == null || panelRoot == null)
                return;

            currentCard = card;
            var visuals = card.CurrentVisuals;
            var runtime = card.runtimeCard;
            var data = card.cardData;

            if (nameText != null)
                nameText.text = visuals.name ?? runtime?.Name ?? data?.cardName ?? "Carta";

            if (artworkImage != null)
            {
                artworkImage.sprite = visuals.sprite ?? runtime?.Artwork ?? data?.artwork;
                if (baseArtworkSize == Vector2.zero)
                    baseArtworkSize = artworkImage.rectTransform.sizeDelta;
                artworkImage.rectTransform.sizeDelta = baseArtworkSize * artworkScale;
            }

            if (statsText != null)
            {
                if (card.IsSpell)
                    statsText.text = $"Magia • Custo {runtime?.EnergyCost ?? data?.energyCost ?? 0}";
                else
                {
                    int atk = visuals.attack;
                    int def = visuals.defense;
                    int hp = runtime != null ? runtime.MaxHealth : visuals.health;
                    int cost = data?.energyCost ?? visuals.energyCost;
                    statsText.text = $"ATK {atk} / DEF {def} / HP {hp} • Custo {cost}";
                }
            }

            if (descriptionText != null)
            {
                string description = runtime?.PreviewDescription ??
                                     data?.previewDescription ??
                                     visuals.description ??
                                     runtime?.LoreDescription ??
                                     data?.loreDescription ?? "Sem descrição.";
                descriptionText.text = description;
            }

            if (boardImage != null)
            {
                var boardSprite = runtime?.PreviewBoard ?? data?.previewBoard;
                boardImage.sprite = boardSprite;
                boardImage.enabled = boardSprite != null;
            }

            panelRoot.anchoredPosition = anchoredPosition;
            panelRoot.gameObject.SetActive(true);
        }

        public void Hide(CardUI card = null)
        {
            if (card != null && card != currentCard)
                return;

            currentCard = null;

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(false);

            if (artworkImage != null && baseArtworkSize != Vector2.zero)
                artworkImage.rectTransform.sizeDelta = baseArtworkSize;

            if (boardImage != null)
                boardImage.enabled = false;
        }
    }
}
