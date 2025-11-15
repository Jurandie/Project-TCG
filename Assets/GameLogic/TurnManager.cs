using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class TurnManager : MonoBehaviour
    {
        public enum TurnOwner { Player, Enemy }

        [Header("Referências")]
        public DeckManager deckManager;
        public DiceRoller_Stable2025 diceRoller;
        public EnergyManager energyManager;
        public LifeManager lifeManager;
        public CharacterAttributeManager attributeManager;

        [Header("UI")]
        public Button endPhaseButton;
        public Text turnText;

        public TurnOwner currentTurn = TurnOwner.Player;

        private TurnOwner? pendingAttributeRollOwner;

        public event System.Action<TurnOwner> OnTurnStarted;

        void Start()
        {
            if (endPhaseButton != null)
                endPhaseButton.onClick.AddListener(EndPhase);

            StartCoroutine(DelayedStart());
        }

        IEnumerator DelayedStart()
        {
            yield return null;
            StartTurn(currentTurn);
            UpdateTurnUI();
        }

        void UpdateTurnUI()
        {
            if (turnText != null)
                turnText.text = $"Turno: {currentTurn}";

            if (endPhaseButton != null)
            {
                bool canEnd = currentTurn == TurnOwner.Player &&
                    !IsGameOver() &&
                    !IsWaitingForAttributeRoll(TurnOwner.Player);
                endPhaseButton.interactable = canEnd;
            }
        }

        bool IsGameOver()
        {
            if (lifeManager == null) return false;
            return lifeManager.IsDead(TurnOwner.Player) || lifeManager.IsDead(TurnOwner.Enemy);
        }

        public void EndPhase()
        {
            if (IsGameOver())
            {
                Debug.Log("[TurnManager] Jogo finalizado.");
                if (endPhaseButton != null) endPhaseButton.interactable = false;
                return;
            }

            if (IsWaitingForAttributeRoll(currentTurn))
            {
                Debug.Log("[TurnManager] Role os atributos antes de encerrar o turno.");
                return;
            }

            if (energyManager != null)
                energyManager.EndTurn(currentTurn);

            currentTurn = currentTurn == TurnOwner.Player ? TurnOwner.Enemy : TurnOwner.Player;

            StartTurn(currentTurn);
            UpdateTurnUI();
        }

        public void StartTurn(TurnOwner owner)
        {
            if (energyManager != null)
                energyManager.AddEnergy(owner, 1);

            if (deckManager != null)
                deckManager.OnTurnStart(owner);

            OnTurnStarted?.Invoke(owner);
            UpdateDiceColor(owner);

            if (attributeManager != null && attributeManager.NeedsInitialRoll(owner))
            {
                pendingAttributeRollOwner = owner;
                UpdateTurnUI();

                if (owner == TurnOwner.Player)
                    Debug.Log("[TurnManager] Role o dado para gerar os atributos do jogador.");
                else
                    Debug.Log("[TurnManager] Inimigo aguarda rolagem manual para definir atributos.");

                return;
            }

            pendingAttributeRollOwner = null;
            ContinueTurnFlow(owner);
        }

        void UpdateDiceColor(TurnOwner owner)
        {
            if (diceRoller == null) return;
            Color color = owner == TurnOwner.Player ? new Color(0.64f, 0f, 0.64f) : Color.white;
            diceRoller.SetColor(color);
        }

        public bool IsWaitingForAttributeRoll(TurnOwner owner)
        {
            return pendingAttributeRollOwner.HasValue && pendingAttributeRollOwner.Value == owner;
        }

        public void NotifyAttributeRollComplete(TurnOwner owner)
        {
            if (!IsWaitingForAttributeRoll(owner))
                return;

            pendingAttributeRollOwner = null;
            Debug.Log($"[TurnManager] Atributos de {owner} definidos. Prosseguindo com o turno.");
            ContinueTurnFlow(owner);
        }

        void ContinueTurnFlow(TurnOwner owner)
        {
            UpdateTurnUI();

            if (owner == TurnOwner.Enemy)
                StartCoroutine(EnemyRoutine());
            else
                Debug.Log("[TurnManager] É o turno do jogador — clique no dado para rolar.");
        }

        IEnumerator EnemyRoutine()
        {
            yield return new WaitForSeconds(0.6f);

            if (attributeManager != null && attributeManager.NeedsInitialRoll(TurnOwner.Enemy))
            {
                Debug.Log("[TurnManager] EnemyRoutine aguardando rolagem de atributos.");
                yield break;
            }

            if (diceRoller != null)
            {
                if (energyManager != null)
                {
                    if (!energyManager.CanRollDice(TurnOwner.Enemy))
                    {
                        Debug.Log("[TurnManager] Inimigo sem energia para rolar.");
                        yield return new WaitForSeconds(0.2f);
                        EndPhase();
                        yield break;
                    }

                    if (!energyManager.ConsumeEnergyForRoll(TurnOwner.Enemy))
                    {
                        Debug.Log("[TurnManager] Falha ao consumir energia do inimigo.");
                        yield return new WaitForSeconds(0.2f);
                        EndPhase();
                        yield break;
                    }
                }

                bool finished = false;
                int rollValue = 0;

                diceRoller.RollDiceForTurn((res) =>
                {
                    rollValue = res;
                    finished = true;
                });

                while (!finished) yield return null;

                if (deckManager != null)
                    deckManager.ReceiveRoll(TurnOwner.Enemy, rollValue);
            }

            yield return new WaitForSeconds(1f);
            EndPhase();
        }
    }
}
