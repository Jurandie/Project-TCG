using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class LifeManager : MonoBehaviour
    {
        [Header("Valores Iniciais")]
        public int playerStartingHP = 20;
        public int enemyStartingHP = 20;

        [Header("Referencias de UI")]
        public Text playerHPText;
        public Text enemyHPText;
        public LifeBadgeUI playerLifeUI;
        public LifeBadgeUI enemyLifeUI;

        [Header("Configuracoes de Debug")]
        public bool debugLogs = true;

        private int playerCurrentHP;
        private int enemyCurrentHP;

        private bool playerDead = false;
        private bool enemyDead = false;

        void Start()
        {
            ResetLife();
            UpdateUI();
        }

        public void ApplyExternalStartingHP(int playerHP, int enemyHP)
        {
            ApplyExternalStartingHP(TurnManager.TurnOwner.Player, playerHP);
            ApplyExternalStartingHP(TurnManager.TurnOwner.Enemy, enemyHP);
        }

        public void ApplyExternalStartingHP(TurnManager.TurnOwner owner, int hp)
        {
            hp = Mathf.Max(1, hp);

            if (owner == TurnManager.TurnOwner.Player)
            {
                playerStartingHP = hp;
                playerCurrentHP = hp;
                playerDead = false;
            }
            else
            {
                enemyStartingHP = hp;
                enemyCurrentHP = hp;
                enemyDead = false;
            }

            UpdateUI();

            if (debugLogs)
                Debug.Log($"[LifeManager] HP definido por atributos externos ({owner}) = {hp}");
        }

        public void ResetLife()
        {
            playerCurrentHP = playerStartingHP;
            enemyCurrentHP = enemyStartingHP;
            playerDead = false;
            enemyDead = false;

            if (debugLogs)
                Debug.Log("[LifeManager] HP inicializado: Player=" + playerCurrentHP + " | Enemy=" + enemyCurrentHP);
        }

        public void TakeDamage(TurnManager.TurnOwner target, int amount)
        {
            if (amount <= 0) return;

            if (target == TurnManager.TurnOwner.Player)
            {
                playerCurrentHP = Mathf.Max(0, playerCurrentHP - amount);
                playerLifeUI?.UpdateLife(playerCurrentHP);
                playerLifeUI?.PlayDamageAnimation();
                if (debugLogs) Debug.Log($"[LifeManager] Jogador tomou {amount} de dano -> HP atual: {playerCurrentHP}");
                if (playerCurrentHP <= 0) { playerDead = true; OnDeath(target); }
            }
            else
            {
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - amount);
                enemyLifeUI?.UpdateLife(enemyCurrentHP);
                enemyLifeUI?.PlayDamageAnimation();
                if (debugLogs) Debug.Log($"[LifeManager] Inimigo tomou {amount} de dano -> HP atual: {enemyCurrentHP}");
                if (enemyCurrentHP <= 0) { enemyDead = true; OnDeath(target); }
            }

            UpdateUI();
        }

        public void Heal(TurnManager.TurnOwner target, int amount)
        {
            if (amount <= 0) return;

            if (target == TurnManager.TurnOwner.Player)
            {
                playerCurrentHP = Mathf.Min(playerStartingHP, playerCurrentHP + amount);
                playerLifeUI?.UpdateLife(playerCurrentHP);
                playerLifeUI?.PlayHealAnimation();
                if (debugLogs) Debug.Log($"[LifeManager] Jogador curou {amount} -> HP atual: {playerCurrentHP}");
            }
            else
            {
                enemyCurrentHP = Mathf.Min(enemyStartingHP, enemyCurrentHP + amount);
                enemyLifeUI?.UpdateLife(enemyCurrentHP);
                enemyLifeUI?.PlayHealAnimation();
                if (debugLogs) Debug.Log($"[LifeManager] Inimigo curou {amount} -> HP atual: {enemyCurrentHP}");
            }

            UpdateUI();
        }

        public bool IsDead(TurnManager.TurnOwner target)
        {
            return target == TurnManager.TurnOwner.Player ? playerDead : enemyDead;
        }

        private void OnDeath(TurnManager.TurnOwner who)
        {
            if (debugLogs)
                Debug.Log($"[LifeManager] {who} morreu!");

            if (who == TurnManager.TurnOwner.Player)
            {
                Debug.Log("O jogador perdeu o duelo!");
            }
            else
            {
                Debug.Log("O inimigo foi derrotado!");
            }
        }

        private void UpdateUI()
        {
            if (playerHPText != null)
                playerHPText.text = playerCurrentHP.ToString();
            if (enemyHPText != null)
                enemyHPText.text = enemyCurrentHP.ToString();

            playerLifeUI?.SetIntensity((float)playerCurrentHP / Mathf.Max(1, playerStartingHP));
            enemyLifeUI?.SetIntensity((float)enemyCurrentHP / Mathf.Max(1, enemyStartingHP));
        }

        public int GetCurrentHP(TurnManager.TurnOwner target)
        {
            return target == TurnManager.TurnOwner.Player ? playerCurrentHP : enemyCurrentHP;
        }
    }
}
