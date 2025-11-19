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
    }
}
