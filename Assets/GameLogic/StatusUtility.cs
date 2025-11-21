using UnityEngine;

namespace GameLogic
{
    public static class StatusUtility
    {
        public static HeroStatusTracker EnsureHeroTracker(TurnManager.TurnOwner owner)
        {
            var trackers = Object.FindObjectsByType<HeroStatusTracker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tracker in trackers)
            {
                if (tracker != null && tracker.owner == owner)
                    return tracker;
            }

            var go = new GameObject($"HeroStatus_{owner}");
            var newTracker = go.AddComponent<HeroStatusTracker>();
            newTracker.owner = owner;
            var handler = go.AddComponent<HeroStatusEffectHandler>();
            handler.lifeManager = Object.FindFirstObjectByType<LifeManager>();
            handler.turnManager = Object.FindFirstObjectByType<TurnManager>();
            return newTracker;
        }

        public static HeroStatusEffectHandler EnsureHeroHandler(TurnManager.TurnOwner owner)
        {
            var tracker = EnsureHeroTracker(owner);
            var handler = tracker.GetComponent<HeroStatusEffectHandler>();
            if (handler == null)
            {
                handler = tracker.gameObject.AddComponent<HeroStatusEffectHandler>();
                handler.lifeManager = Object.FindFirstObjectByType<LifeManager>();
                handler.turnManager = Object.FindFirstObjectByType<TurnManager>();
            }
            else
            {
                if (handler.lifeManager == null)
                    handler.lifeManager = Object.FindFirstObjectByType<LifeManager>();
                if (handler.turnManager == null)
                    handler.turnManager = Object.FindFirstObjectByType<TurnManager>();
            }

            return handler;
        }

        public static CardStatusTracker EnsureCardTracker(CardUI card)
        {
            if (card == null)
                return null;

            var tracker = card.GetComponent<CardStatusTracker>();
            if (tracker == null)
                tracker = card.gameObject.AddComponent<CardStatusTracker>();
            return tracker;
        }

        public static CardEvolutionManager GetEvolutionManager()
        {
            return Object.FindFirstObjectByType<CardEvolutionManager>();
        }

        public static bool BanishCard(CardUI card, int turns)
        {
            if (card == null || card.CurrentSlot == null)
                return false;

            var manager = Object.FindFirstObjectByType<TurnManager>();
            if (manager == null)
                return false;

            var effectObject = new GameObject($"BanishEffect_{card.name}");
            effectObject.transform.SetParent(manager.transform);
            var effect = effectObject.AddComponent<CardBanishEffect>();
            effect.Initialize(card, card.CurrentSlot, turns, manager, card.OwnerSide);
            return true;
        }
    }
}
