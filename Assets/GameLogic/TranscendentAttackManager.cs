using UnityEngine;

namespace GameLogic
{
    public class TranscendentAttackManager : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] TurnManager turnManager;
        [SerializeField] LifeManager lifeManager;
        [SerializeField] CharacterAttributeManager attributeManager;

        CardUI pendingCard;
        TurnManager.TurnOwner pendingOwner;

        void Awake()
        {
            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();
            if (lifeManager == null && turnManager != null)
                lifeManager = turnManager.lifeManager;
            if (attributeManager == null && turnManager != null)
                attributeManager = turnManager.attributeManager;
        }

        public void SelectCardForAttack(CardUI card)
        {
            if (card == null)
                return;

            if (!card.CurrentVisuals.isTranscendent)
            {
                Debug.Log("[TranscendentAttack] Carta ainda não ascendeu.");
                return;
            }

            if (card.IsEquipment)
            {
                Debug.Log("[TranscendentAttack] Equipamentos não realizam ataques direcionados.");
                return;
            }

            var slot = card.CurrentSlot;
            if (slot == null || slot.ParentZone == null)
            {
                Debug.Log("[TranscendentAttack] Carta precisa estar em um slot para atacar.");
                return;
            }

            var owner = slot.ParentZone.owner;
            if (turnManager != null && turnManager.currentTurn != owner)
            {
                Debug.Log("[TranscendentAttack] Não é o turno do dono da carta.");
                return;
            }

            pendingCard = card;
            pendingOwner = owner;
            Debug.Log("[TranscendentAttack] Carta selecionada. Clique no dado para realizar o teste.");
        }

        public bool HasPendingAttack(TurnManager.TurnOwner owner)
        {
            return pendingCard != null && pendingOwner == owner;
        }

        public void ResolvePendingAttack(int rollResult)
        {
            if (pendingCard == null)
                return;

            var owner = pendingOwner;
            var target = owner == TurnManager.TurnOwner.Player ? TurnManager.TurnOwner.Enemy : TurnManager.TurnOwner.Player;

            int attackMod = attributeManager != null ? attributeManager.GetAbilityModifier(owner, "STR") : 0;
            int defenseScore = 10 + (attributeManager != null ? attributeManager.GetAbilityModifier(target, "DEX") : 0);

            bool success;
            if (rollResult == 20) success = true;
            else if (rollResult == 1) success = false;
            else success = (rollResult + attackMod) >= defenseScore;

            if (success)
            {
                int damage = Mathf.Max(1, pendingCard.CurrentVisuals.attack);
                lifeManager?.TakeDamage(target, damage);
                Debug.Log($"[TranscendentAttack] Ataque bem-sucedido ({rollResult}+{attackMod} >= {defenseScore}). Dano {damage} em {target}.");
            }
            else
            {
                Debug.Log($"[TranscendentAttack] Ataque falhou ({rollResult}+{attackMod} < {defenseScore}).");
            }

            pendingCard = null;
        }

        public void ClearSelection(CardUI card)
        {
            if (pendingCard == card)
                pendingCard = null;
        }
    }
}
