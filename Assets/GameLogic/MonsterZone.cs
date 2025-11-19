using UnityEngine;

namespace GameLogic
{
    public class MonsterZone : MonoBehaviour
    {
        public TurnManager.TurnOwner owner = TurnManager.TurnOwner.Player;
        public CardEvolutionManager evolutionManager;
        public CharacterAttributeManager attributeManager;
        public SpellCastManager spellCastManager;
        public MonsterZoneSlot[] slots;

        void Awake()
        {
            slots = GetComponentsInChildren<MonsterZoneSlot>();
            foreach (var slot in slots)
                slot.SetParentZone(this);
        }

        internal void NotifyCardPlaced(CardUI card)
        {
            if (card != null)
                card.SetOwner(owner);
            evolutionManager?.OnCardPlaced(owner, card);
            attributeManager = attributeManager ?? (FindFirstObjectByType<TurnManager>()?.attributeManager);
            card?.ApplyTranscendentAttributes(owner, attributeManager);
        }

        internal void NotifyCardRemoved(CardUI card)
        {
            evolutionManager?.OnCardRemoved(owner, card);
        }

        internal bool HandleSpellCard(CardUI card)
        {
            if (spellCastManager == null || card == null)
                return false;

            spellCastManager.CastSpell(owner, card);
            return true;
        }

        internal MonsterZoneSlot GetFirstEmptySlot()
        {
            if (slots == null)
                return null;

            foreach (var slot in slots)
            {
                if (slot != null && slot.IsEmpty())
                    return slot;
            }

            return null;
        }
    }
}
