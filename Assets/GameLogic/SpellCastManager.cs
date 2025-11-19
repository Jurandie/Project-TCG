using UnityEngine;

namespace GameLogic
{
    public class SpellCastManager : MonoBehaviour
    {
        [SerializeField] TurnManager turnManager;
        [SerializeField] LifeManager lifeManager;
        [SerializeField] HandManager handManager;
        [SerializeField] DeckManager deckManager;
        [SerializeField] MonsterZone playerZone;
        [SerializeField] MonsterZone enemyZone;
        [SerializeField] CharacterAttributeManager attributeManager;
        [SerializeField] SpellTargetSelectionManager targetSelectionManager;

        void Awake()
        {
            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();
            if (lifeManager == null && turnManager != null)
                lifeManager = turnManager.lifeManager;
            if (handManager == null)
                handManager = FindFirstObjectByType<HandManager>();
            if (deckManager == null)
                deckManager = FindFirstObjectByType<DeckManager>();
            if (attributeManager == null && turnManager != null)
                attributeManager = turnManager.attributeManager;
            if (targetSelectionManager == null)
            {
                targetSelectionManager = FindFirstObjectByType<SpellTargetSelectionManager>();
                if (targetSelectionManager == null)
                {
                    var go = new GameObject("SpellTargetSelectionManager");
                    targetSelectionManager = go.AddComponent<SpellTargetSelectionManager>();
                }
            }
            if (playerZone == null || enemyZone == null)
            {
                var zones = FindObjectsByType<MonsterZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var zone in zones)
                {
                    if (zone.owner == TurnManager.TurnOwner.Player && playerZone == null)
                        playerZone = zone;
                    else if (zone.owner == TurnManager.TurnOwner.Enemy && enemyZone == null)
                        enemyZone = zone;
                }
            }
        }

        public void CastSpell(TurnManager.TurnOwner owner, CardUI card)
        {
            if (card == null || card.cardData == null)
                return;

            if (!card.IsSpell)
            {
                Debug.LogWarning("[SpellCastManager] Carta recebida não é do tipo Spell.");
                return;
            }

            var effect = card.cardData.spellEffect;
            if (effect == null)
            {
                Debug.LogWarning($"[SpellCastManager] Carta {card.cardData.cardName} não possui efeito configurado.");
                Cleanup(card);
                return;
            }

            var context = new SpellCastContext
            {
                owner = owner,
                target = owner == TurnManager.TurnOwner.Player ? TurnManager.TurnOwner.Enemy : TurnManager.TurnOwner.Player,
                lifeManager = lifeManager,
                turnManager = turnManager,
                handManager = handManager,
                card = card,
                deckManager = deckManager,
                playerZone = playerZone,
                enemyZone = enemyZone,
                attributeManager = attributeManager,
                targetSelectionManager = targetSelectionManager,
                caster = this
            };

            effect.Resolve(context);

            if (card.cardData.destroyAfterCast && !context.deferCleanup)
                Cleanup(card);
        }

        void Cleanup(CardUI card)
        {
            if (card == null) return;

            var slot = card.CurrentSlot;
            if (slot != null)
                slot.ClearSlot();

            Destroy(card.gameObject);
        }

        public void FinalizeSpell(SpellCastContext context)
        {
            if (context == null || context.card == null)
                return;

            if (context.card.cardData.destroyAfterCast)
                Cleanup(context.card);
        }
    }

    public class SpellCastContext
    {
        public TurnManager.TurnOwner owner;
        public TurnManager.TurnOwner target;
        public LifeManager lifeManager;
        public TurnManager turnManager;
        public HandManager handManager;
        public DeckManager deckManager;
        public MonsterZone playerZone;
        public MonsterZone enemyZone;
        public CharacterAttributeManager attributeManager;
        public SpellTargetSelectionManager targetSelectionManager;
        public SpellCastManager caster;
        public CardUI card;
        public bool deferCleanup;

        public void DeferCleanup()
        {
            deferCleanup = true;
        }

        public void FinalizeSpell()
        {
            caster?.FinalizeSpell(this);
        }
    }
}
