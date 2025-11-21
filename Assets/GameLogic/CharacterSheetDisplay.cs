using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// Simple floating panel that displays hero or card stats.
    /// </summary>
    public class CharacterSheetDisplay : MonoBehaviour
    {
        [Header("UI")]
        public RectTransform panelRoot;
        public TMP_Text titleText;
        public TMP_Text bodyText;
        public Image artworkImage;
        public Canvas rootCanvas;
        public Vector2 panelOffset = new Vector2(24f, -24f);

        [Header("Data Sources")]
        public CharacterAttributeManager attributeManager;

        void Awake()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
            if (attributeManager == null)
                attributeManager = FindFirstObjectByType<CharacterAttributeManager>();

            Hide();
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
                Hide();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
                Hide();
        }
#endif

        public void ShowHero(TurnManager.TurnOwner owner, Vector2 screenPosition)
        {
            if (attributeManager == null)
                return;

            var stats = attributeManager.GetStats(owner);
            if (stats == null)
                return;

            string heroName = owner == TurnManager.TurnOwner.Player ? "Jogador" : "Inimigo";
            if (titleText != null)
                titleText.text = $"{heroName} - Ficha";

            if (bodyText != null)
            {
                bodyText.text =
                    $"HP: {stats.maxLife}\n" +
                    $"STR: {stats.strength}\n" +
                    $"DEX: {stats.dexterity}\n" +
                    $"CON: {stats.constitution}\n" +
                    $"INT: {stats.intelligence}\n" +
                    $"WIS: {stats.wisdom}\n" +
                    $"CHA: {stats.charisma}\n" +
                    $"{stats.statusDescription}";
            }

            if (artworkImage != null)
                artworkImage.sprite = null;

            PlacePanel(screenPosition);
        }

        public void ShowCard(CardUI card, Vector2 screenPosition)
        {
            if (card == null)
                return;

            var visuals = card.CurrentVisuals;
            var runtime = card.runtimeCard;
            var data = runtime ?? (card.cardData != null ? new Card(card.cardData) : null);

            if (titleText != null)
                titleText.text = $"{visuals.name} - Carta";

            if (bodyText != null)
            {
                if (card.IsSpell)
                {
                    string desc = data?.LoreDescription ?? card.cardData?.loreDescription ?? "Magia";
                    bodyText.text = $"Tipo: Magia\nCusto: {data?.EnergyCost ?? card.cardData?.energyCost ?? 0}\n\n{desc}";
                }
                else
                {
                    int atk = visuals.attack;
                    int def = visuals.defense;
                    int hp = runtime?.MaxHealth ?? visuals.health;
                    int cost = data?.EnergyCost ?? card.cardData?.energyCost ?? 0;
                    bodyText.text =
                        $"Tipo: {(card.IsEquipment ? "Equipamento" : "Monstro")}\n" +
                        $"ATK: {atk}\nDEF: {def}\nHP: {hp}\nEnergia: {cost}";
                }
            }

            if (artworkImage != null)
                artworkImage.sprite = card.artImage != null ? card.artImage.sprite : visuals.sprite;

            PlacePanel(screenPosition);
        }

        void PlacePanel(Vector2 screenPosition)
        {
            if (panelRoot == null || rootCanvas == null)
                return;

            Vector2 anchoredPos;
            var canvasRect = rootCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, rootCanvas.worldCamera, out anchoredPos))
            {
                anchoredPos += panelOffset;
                panelRoot.anchoredPosition = anchoredPos;
            }

            panelRoot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.gameObject.SetActive(false);
        }
    }
}
