using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    public class SpellHeroTarget : MonoBehaviour, IPointerClickHandler
    {
        public SpellTargetSelectionManager targetManager;
        public TurnManager.TurnOwner owner = TurnManager.TurnOwner.Player;

        void Awake()
        {
            if (targetManager == null)
                targetManager = FindFirstObjectByType<SpellTargetSelectionManager>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetManager != null && targetManager.TrySelectHero(owner))
                eventData.Use();
        }
    }
}
