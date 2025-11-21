using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// Routes clicks from a UI/3D element to the CombatManager.
    /// </summary>
    public class AttackClickTarget : MonoBehaviour, IPointerClickHandler
    {
        public CombatManager combatManager;
        public TurnManager.TurnOwner owner = TurnManager.TurnOwner.Player;
        [SerializeField] SpellTargetSelectionManager spellTargetManager;
        HoldInspectHandler holdInspector;
        HeroStatusTracker heroStatusTracker;
        HeroStatusEffectHandler heroStatusEffectHandler;

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleClick(eventData);
        }

        void OnMouseDown()
        {
            HandleClick(null);
        }

        void HandleClick(PointerEventData eventData)
        {
            if (holdInspector == null)
                holdInspector = GetComponent<HoldInspectHandler>();
            if (holdInspector != null && holdInspector.ConsumeClickBlock())
            {
                eventData?.Use();
                return;
            }

            EnsureHeroStatusComponents();
            if (heroStatusEffectHandler != null && heroStatusEffectHandler.IsSilenced)
            {
                Debug.Log("[AttackClickTarget] Este herói está silenciado e não pode agir.");
                eventData?.Use();
                return;
            }

            if (spellTargetManager == null)
                spellTargetManager = FindFirstObjectByType<SpellTargetSelectionManager>();

            if (spellTargetManager != null && spellTargetManager.TrySelectHero(owner))
            {
                eventData?.Use();
                return;
            }

            TriggerAttack();
        }

        void TriggerAttack()
        {
            if (combatManager == null)
            {
                Debug.LogWarning("[AttackClickTarget] CombatManager não atribuído.");
                return;
            }

            combatManager.RequestAttack(owner);
        }

        void EnsureHeroStatusComponents()
        {
            if (heroStatusTracker == null)
            {
                heroStatusTracker = GetComponent<HeroStatusTracker>();
                if (heroStatusTracker == null)
                    heroStatusTracker = gameObject.AddComponent<HeroStatusTracker>();
                heroStatusTracker.owner = owner;
            }

            if (heroStatusEffectHandler == null)
            {
                heroStatusEffectHandler = GetComponent<HeroStatusEffectHandler>();
                if (heroStatusEffectHandler == null)
                    heroStatusEffectHandler = gameObject.AddComponent<HeroStatusEffectHandler>();
            }

            if (heroStatusEffectHandler != null)
            {
                if (heroStatusEffectHandler.lifeManager == null)
                    heroStatusEffectHandler.lifeManager = FindFirstObjectByType<LifeManager>();
                if (heroStatusEffectHandler.turnManager == null)
                    heroStatusEffectHandler.turnManager = FindFirstObjectByType<TurnManager>();
            }
        }
    }
}
