using System.Collections;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Simple enemy controller that automates dice rolls, draws and basic plays.
    /// </summary>
    public class EnemyAIController : MonoBehaviour
    {
        [Header("Referências")]
        public TurnManager turnManager;
        public DiceRoller_Stable2025 diceRoller;
        public DeckManager deckManager;
        public HandManager handManager;
        public MonsterZone enemyMonsterZone;
        public CombatManager combatManager;

        [Header("Timings")]
        public float initialDelay = 0.5f;
        public float postRollDelay = 0.75f;
        public float postPlayDelay = 0.5f;

        Coroutine routine;

        void Awake()
        {
            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();
            if (diceRoller == null && turnManager != null)
                diceRoller = turnManager.diceRoller;
            if (deckManager == null && turnManager != null)
                deckManager = turnManager.deckManager;
            if (handManager == null && deckManager != null)
                handManager = deckManager.handManager;
            if (combatManager == null)
                combatManager = FindFirstObjectByType<CombatManager>();
        }

        void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        public void BeginTurn()
        {
            if (!isActiveAndEnabled)
                return;

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(RunTurn());
        }

        IEnumerator RunTurn()
        {
            yield return new WaitForSeconds(initialDelay);

            if (turnManager == null)
            {
                routine = null;
                yield break;
            }

            if (turnManager.IsWaitingForAttributeRoll(TurnManager.TurnOwner.Enemy))
            {
                routine = null;
                yield break;
            }

            int rollValue = 0;
            bool rolled = false;

            if (diceRoller != null)
            {
                diceRoller.RollDiceForTurn((res) =>
                {
                    rollValue = res;
                    rolled = true;
                });

                while (!rolled)
                    yield return null;

                if (deckManager != null)
                    deckManager.ReceiveRoll(TurnManager.TurnOwner.Enemy, rollValue);
            }

            yield return new WaitForSeconds(postRollDelay);
            TryPlayCardFromHand();
            yield return new WaitForSeconds(postPlayDelay);

            if (combatManager != null)
                yield return combatManager.PerformAttackAndWait(TurnManager.TurnOwner.Enemy);
            else
                yield return new WaitForSeconds(0.25f);

            turnManager.EndPhase();
            routine = null;
        }

        void TryPlayCardFromHand()
        {
            if (handManager == null || enemyMonsterZone == null)
                return;

            Transform enemyHand = handManager.enemyHandContainer;
            if (enemyHand == null || enemyHand.childCount == 0)
                return;

            MonsterZoneSlot targetSlot = null;
            foreach (var slot in enemyMonsterZone.slots)
            {
                if (slot != null && slot.IsEmpty())
                {
                    targetSlot = slot;
                    break;
                }
            }

            if (targetSlot == null)
                return;

            CardUI card = null;
            for (int i = 0; i < enemyHand.childCount; i++)
            {
                card = enemyHand.GetChild(i).GetComponent<CardUI>();
                if (card != null)
                    break;
            }

            if (card == null)
                return;

            if (!targetSlot.TryPlaceCard(card))
                return;

            Debug.Log("[EnemyAI] Carta posicionada automaticamente em um slot livre.");
        }
    }
}
