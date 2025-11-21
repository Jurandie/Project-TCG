using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class CardEvolutionManager : MonoBehaviour
    {
        [Header("Referências")]
        public TranscendentAttackManager attackManager;

        class CardGroup
        {
            public readonly List<CardUI> cards = new List<CardUI>();
        }

        readonly Dictionary<TurnManager.TurnOwner, Dictionary<string, Dictionary<int, CardGroup>>> tracker =
            new Dictionary<TurnManager.TurnOwner, Dictionary<string, Dictionary<int, CardGroup>>>();

        public void OnCardPlaced(TurnManager.TurnOwner owner, CardUI card)
        {
            if (card == null || card.cardData == null)
                return;

            if (card.IsSpell || card.IsEquipment)
                return;

            var bucket = GetGroup(owner, card.cardData.cardName, card.CurrentTier);
            if (!bucket.cards.Contains(card))
                bucket.cards.Add(card);

            if (bucket.cards.Count >= 2)
                MergeCards(owner, card.cardData.cardName, card.CurrentTier, bucket.cards[0]);
        }

        public void OnCardRemoved(TurnManager.TurnOwner owner, CardUI card)
        {
            if (card == null || card.cardData == null)
                return;

            var bucket = GetGroup(owner, card.cardData.cardName, card.CurrentTier, false);
            bucket?.cards.Remove(card);
            attackManager?.ClearSelection(card);
        }

        void MergeCards(TurnManager.TurnOwner owner, string cardName, int tier, CardUI referenceCard)
        {
            var bucket = GetGroup(owner, cardName, tier);
            CardUI partner = null;
            foreach (var c in bucket.cards)
            {
                if (c != referenceCard)
                {
                    partner = c;
                    break;
                }
            }

            if (partner == null)
                return;

            bucket.cards.Remove(referenceCard);
            bucket.cards.Remove(partner);

            var partnerSlot = partner.CurrentSlot;
            if (partnerSlot != null)
                partnerSlot.ClearSlot();

            Destroy(partner.gameObject);

            if (!referenceCard.UpgradeTier())
            {
                bucket.cards.Add(referenceCard);
                return;
            }

            var newBucket = GetGroup(owner, cardName, referenceCard.CurrentTier);
            if (!newBucket.cards.Contains(referenceCard))
                newBucket.cards.Add(referenceCard);

            TryHandleTranscendence(owner, referenceCard);
        }

        CardGroup GetGroup(TurnManager.TurnOwner owner, string cardName, int tier, bool createIfMissing = true)
        {
            if (!tracker.TryGetValue(owner, out var ownerDict))
            {
                if (!createIfMissing)
                    return null;
                ownerDict = new Dictionary<string, Dictionary<int, CardGroup>>();
                tracker[owner] = ownerDict;
            }

            if (!ownerDict.TryGetValue(cardName, out var tierDict))
            {
                if (!createIfMissing)
                    return null;
                tierDict = new Dictionary<int, CardGroup>();
                ownerDict[cardName] = tierDict;
            }

            if (!tierDict.TryGetValue(tier, out var group))
            {
                if (!createIfMissing)
                    return null;
                group = new CardGroup();
                tierDict[tier] = group;
            }

            return group;
        }

        void TryHandleTranscendence(TurnManager.TurnOwner owner, CardUI card)
        {
            if (card == null || card.cardData == null)
                return;

            int maxTier = card.cardData.GetMaxTier();
            if (card.CurrentTier < maxTier)
                return;

            bool isEquipment = card.IsEquipment;
            bool equipmentFlag = false;
            if (isEquipment)
            {
                if (card.runtimeCard != null)
                    equipmentFlag = card.runtimeCard.IsTranscendentFormAvailable;
                else if (card.cardData != null)
                    equipmentFlag = card.cardData.isTranscendentFormAvailable;
            }

            var visuals = card.CurrentVisuals;
            bool allowTranscend = visuals.isTranscendent || (isEquipment && equipmentFlag);
            if (!allowTranscend)
                return;

            var zone = card.CurrentSlot != null ? card.CurrentSlot.ParentZone : null;
            if (zone != null)
            {
                if (zone.attributeManager == null)
                    zone.attributeManager = FindFirstObjectByType<TurnManager>()?.attributeManager;

                card.ApplyTranscendentAttributes(zone.owner, zone.attributeManager, isEquipment && equipmentFlag && !visuals.isTranscendent);
            }

            Debug.Log("[CardEvolution] Carta alcançou forma transcendida. Clique nela para atacar.");
            attackManager?.ClearSelection(card);
        }

        public bool ForceDowngrade(CardUI card, int tiers)
        {
            if (card == null || card.cardData == null || tiers <= 0)
                return false;

            var slot = card.CurrentSlot;
            TurnManager.TurnOwner owner = slot != null ? slot.ParentZone.owner : card.OwnerSide;

            var bucket = GetGroup(owner, card.cardData.cardName, card.CurrentTier, false);
            bucket?.cards.Remove(card);

            bool changed = card.DowngradeTier(tiers);
            if (!changed)
            {
                if (bucket != null && !bucket.cards.Contains(card))
                    bucket.cards.Add(card);
                return false;
            }

            var newBucket = GetGroup(owner, card.cardData.cardName, card.CurrentTier);
            if (!newBucket.cards.Contains(card))
                newBucket.cards.Add(card);

            return true;
        }
    }
}
