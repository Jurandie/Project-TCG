using UnityEngine;

namespace GameLogic
{
    public class DeckManager : MonoBehaviour
    {
        [Header("Referencias")]
        public Deck playerDeck;
        public Deck enemyDeck;
        public HandManager handManager;
        public TurnManager turnManager;

        [Header("Debug")]
        public bool debugLogs = true;

        private int playerAllowedDraws = 0;
        private int enemyAllowedDraws = 0;

        private int playerSkipDrawTurns = 0;
        private int enemySkipDrawTurns = 0;

        void Start()
        {
            if (playerDeck != null) playerDeck.deckManager = this;
            if (enemyDeck != null) enemyDeck.deckManager = this;
        }

        public void OnTurnStart(TurnManager.TurnOwner owner)
        {
            if (owner == TurnManager.TurnOwner.Player)
            {
                if (playerSkipDrawTurns > 0) playerSkipDrawTurns--;
                if (debugLogs) Debug.Log($"[DeckManager] Inicio turno Player -> skipDrawTurns = {playerSkipDrawTurns}");
            }
            else
            {
                if (enemySkipDrawTurns > 0) enemySkipDrawTurns--;
                if (debugLogs) Debug.Log($"[DeckManager] Inicio turno Enemy -> skipDrawTurns = {enemySkipDrawTurns}");
            }
        }

        public void ReceiveRoll(TurnManager.TurnOwner owner, int rollValue)
        {
            int allowed = 0;
            bool critFail = false;
            bool critSuccess = false;

            if ((owner == TurnManager.TurnOwner.Player && playerSkipDrawTurns > 0) ||
                (owner == TurnManager.TurnOwner.Enemy && enemySkipDrawTurns > 0))
            {
                if (debugLogs)
                {
                    string who = owner == TurnManager.TurnOwner.Player ? "Player" : "Enemy";
                    Debug.Log($"[DeckManager] {who} esta impedido de comprar por skipDrawTurns. Roll {rollValue} ignorado.");
                }

                if (owner == TurnManager.TurnOwner.Player) playerAllowedDraws = 0;
                else enemyAllowedDraws = 0;
                return;
            }

            if (rollValue == 1)
            {
                allowed = 0;
                critFail = true;

                if (owner == TurnManager.TurnOwner.Player)
                    playerSkipDrawTurns = 2;
                else
                    enemySkipDrawTurns = 2;

                if (debugLogs)
                    Debug.Log($"[DeckManager] {owner} FALHA CRITICA -> bloqueado por 2 turnos.");
            }
            else if (rollValue == 20)
            {
                allowed = 2;
                critSuccess = true;

                if (turnManager != null && turnManager.energyManager != null)
                {
                    turnManager.energyManager.AddEnergy(owner, 1);
                    if (debugLogs) Debug.Log($"[DeckManager] {owner} SUCESSO CRITICO -> +1 energia concedida.");
                }
            }
            else if (rollValue >= 12)
            {
                allowed = 1;
            }
            else
            {
                allowed = 0;
            }

            if (owner == TurnManager.TurnOwner.Player)
                playerAllowedDraws = allowed;
            else
                enemyAllowedDraws = allowed;

            if (debugLogs)
            {
                string who = owner == TurnManager.TurnOwner.Player ? "Player" : "Enemy";
                string more = critFail ? "(FALHA CRITICA)" : critSuccess ? "(SUCESSO CRITICO)" : "";
                Debug.Log($"[DeckManager] {who} rolou {rollValue} -> allowedDraws = {allowed} {more}");
            }

            if (allowed > 0)
                DrawNCardsForOwner(owner, allowed);
        }

        public void DrawNCardsForOwner(TurnManager.TurnOwner owner, int n)
        {
            if (n <= 0) return;

            Deck targetDeck = owner == TurnManager.TurnOwner.Player ? playerDeck : enemyDeck;
            bool isEnemy = owner == TurnManager.TurnOwner.Enemy;

            for (int i = 0; i < n; i++)
            {
                if (targetDeck == null) break;

                var card = targetDeck.DrawOne();
                if (card == null) break;
                if (handManager != null)
                    handManager.AddCardToHand(card, isEnemy);
            }

            if (owner == TurnManager.TurnOwner.Player)
                playerAllowedDraws = Mathf.Max(0, playerAllowedDraws - n);
            else
                enemyAllowedDraws = Mathf.Max(0, enemyAllowedDraws - n);

            if (debugLogs) Debug.Log($"[DeckManager] {owner} puxou {n} cartas.");
        }

        public void RequestDrawFromDeck(Deck deck)
        {
            if (deck == null) return;
            if (turnManager == null) return;

            if (turnManager.currentTurn != TurnManager.TurnOwner.Player)
            {
                Debug.Log("Nao e o turno do jogador - draw nao permitido.");
                return;
            }

            if (deck.isEnemyDeck)
            {
                Debug.Log("Nao pode clicar no deck inimigo para puxar.");
                return;
            }

            if (playerAllowedDraws <= 0)
            {
                Debug.Log("Voce nao tem permissoes de draw no momento. Clique no dado para rolar.");
                return;
            }

            if (playerSkipDrawTurns > 0)
            {
                Debug.Log("Voce esta impedido de puxar cartas por efeito de falha critica.");
                return;
            }

            playerAllowedDraws = Mathf.Max(0, playerAllowedDraws - 1);
            var card = deck.DrawOne();
            if (card != null && handManager != null)
                handManager.AddCardToHand(card, false);

            if (debugLogs) Debug.Log("[DeckManager] Jogador puxou carta via clique no deck.");
        }

        public int GetAllowedDrawsFor(TurnManager.TurnOwner owner)
        {
            return owner == TurnManager.TurnOwner.Player ? playerAllowedDraws : enemyAllowedDraws;
        }
    }
}
