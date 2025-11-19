using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// Binds a clickable object (UI image or 3D collider) to TurnManager.EndPhase.
    /// </summary>
    public class DiceBaseTurnButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Turno")]
        [SerializeField] TurnManager turnManager;
        [SerializeField] bool onlyPlayerCanClick = true;

        [Header("Feedback opcional")]
        [SerializeField] AudioSource clickAudio;

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("[DiceBaseTurnButton] Pointer click recebido.");
            TryEndTurn();
        }

        void OnMouseDown()
        {
            // Permite uso com objetos 3D ou sprites com collider.
            Debug.Log("[DiceBaseTurnButton] OnMouseDown detectado.");
            TryEndTurn();
        }

        void TryEndTurn()
        {
            if (turnManager == null)
            {
                Debug.LogWarning("[DiceBaseTurnButton] TurnManager nÃ£o atribuÃ­do.");
                return;
            }

            if (onlyPlayerCanClick &&
                turnManager.currentTurn != TurnManager.TurnOwner.Player)
            {
                Debug.Log("[DiceBaseTurnButton] Clique ignorado - nÃ£o Ã© turno do jogador.");
                return;
            }

            if (clickAudio != null)
                clickAudio.Play();

            Debug.Log("[DiceBaseTurnButton] Executando EndPhase via DiceBase.");
            turnManager.EndPhase();
        }

        public void AssignTurnManager(TurnManager manager)
        {
            turnManager = manager;
        }
    }
}

