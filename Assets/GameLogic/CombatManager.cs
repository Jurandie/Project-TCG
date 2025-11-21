using System.Collections;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Handles D&D-style attack rolls for player and enemy.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [Header("Referências")]
        public TurnManager turnManager;
        public DiceRoller_Stable2025 diceRoller;
        public LifeManager lifeManager;
        public CharacterAttributeManager attributeManager;
        public HeroAttackAnimator playerAnimator;
        public HeroAttackAnimator enemyAnimator;
        HeroStatusEffectHandler playerHeroStatus;
        HeroStatusEffectHandler enemyHeroStatus;

        [Header("Configuração de Dano")]
        public int baseDamage = 4;
        public bool addStrengthModifier = true;
        public int criticalMultiplier = 2;
        public float counterAttackDelay = 0.5f;

        [Header("Debug")]
        public bool logCombat = true;

        bool isResolving;
        bool playerAttackUsed;
        bool enemyAttackUsed;

        void Awake()
        {
            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();
            if (diceRoller == null && turnManager != null)
                diceRoller = turnManager.diceRoller;
            if (lifeManager == null && turnManager != null)
                lifeManager = turnManager.lifeManager;
            if (attributeManager == null && turnManager != null)
                attributeManager = turnManager.attributeManager;
            if (playerAnimator == null)
                playerAnimator = FindAnimator(TurnManager.TurnOwner.Player);
            if (enemyAnimator == null)
                enemyAnimator = FindAnimator(TurnManager.TurnOwner.Enemy);

            CacheHeroStatusHandlers();

            if (turnManager != null)
                turnManager.OnTurnStarted += OnTurnStarted;
        }

        public void RequestPlayerAttack()
        {
            RequestAttack(TurnManager.TurnOwner.Player);
        }

        public bool RequestAttack(TurnManager.TurnOwner attacker)
        {
            if (isResolving)
                return false;
            if (!CanAttack(attacker))
                return false;

            if (attacker == TurnManager.TurnOwner.Player && playerAttackUsed)
            {
                Debug.Log("[Combat] Jogador já atacou neste turno.");
                return false;
            }
            if (attacker == TurnManager.TurnOwner.Enemy && enemyAttackUsed)
            {
                Debug.Log("[Combat] Inimigo já atacou neste turno.");
                return false;
            }

            StartCoroutine(AttackRoutine(attacker));
            if (attacker == TurnManager.TurnOwner.Player)
                playerAttackUsed = true;
            else
                enemyAttackUsed = true;
            return true;
        }

        public IEnumerator PerformAttackAndWait(TurnManager.TurnOwner attacker)
        {
            if (isResolving)
                yield break;
            if (!CanAttack(attacker))
                yield break;

            yield return AttackRoutine(attacker);
        }

        bool CanAttack(TurnManager.TurnOwner attacker)
        {
            if (lifeManager != null && lifeManager.IsDead(attacker))
            {
                if (logCombat) Debug.Log($"[Combat] {attacker} não pode atacar (sem vida).");
                return false;
            }

            if (turnManager != null)
            {
                if (turnManager.currentTurn != attacker)
                {
                    if (logCombat) Debug.Log($"[Combat] Não é o turno de {attacker}.");
                    return false;
                }

                if (turnManager.IsWaitingForAttributeRoll(attacker))
                {
                    if (logCombat) Debug.Log($"[Combat] {attacker} precisa definir atributos antes de atacar.");
                    return false;
                }
            }

            var heroStatus = GetHeroStatusHandler(attacker);
            if (heroStatus != null && heroStatus.IsSilenced)
            {
                if (logCombat) Debug.Log($"[Combat] {attacker} está silenciado e não pode atacar.");
                return false;
            }

            return true;
        }

        IEnumerator AttackRoutine(TurnManager.TurnOwner attacker)
        {
            isResolving = true;

            TurnManager.TurnOwner defender =
                attacker == TurnManager.TurnOwner.Player ? TurnManager.TurnOwner.Enemy : TurnManager.TurnOwner.Player;

            int roll = 0;
            yield return RollD20(value => roll = value);

            if (lifeManager != null && lifeManager.IsDead(attacker))
            {
                isResolving = false;
                yield break;
            }

            if (logCombat) Debug.Log($"[Combat] {attacker} rolou {roll} contra {defender}.");

            if (roll == 20)
            {
                int damage = ComputeDamage(attacker) * criticalMultiplier;
                ApplyDamage(defender, damage, "Sucesso critico", attacker);
            }
            else if (roll == 1)
            {
                if (logCombat) Debug.Log("[Combat] Falha crítica! Contra-ataque garantido.");
                yield return CounterAttack(defender, attacker);
            }
            else
            {
                int attackScore = roll + GetModifier(attacker, "STR");
                int defenseScore = 10 + GetModifier(defender, "DEX");

                if (attackScore >= defenseScore)
                {
            ApplyDamage(defender, ComputeDamage(attacker), "Ataque bem-sucedido", attacker);
                }
                else if (logCombat)
                {
                    Debug.Log($"[Combat] Ataque de {attacker} falhou ({attackScore} vs {defenseScore}).");
                }
            }

            isResolving = false;
        }

        IEnumerator RollD20(System.Action<int> onResult)
        {
            int result = Random.Range(1, 21);

            if (diceRoller != null)
            {
                bool completed = false;
                diceRoller.RollDiceForTurn(res =>
                {
                    result = res;
                    completed = true;
                }, true);

                while (!completed)
                    yield return null;
            }
            else
            {
                yield return null;
            }

            onResult?.Invoke(result);
        }

        IEnumerator CounterAttack(TurnManager.TurnOwner counterAttacker, TurnManager.TurnOwner target)
        {
            if (lifeManager != null && lifeManager.IsDead(counterAttacker))
                yield break;

            yield return new WaitForSeconds(counterAttackDelay);
            ApplyDamage(target, ComputeDamage(counterAttacker), $"Contra-ataque de {counterAttacker}", counterAttacker);
        }

        int ComputeDamage(TurnManager.TurnOwner owner)
        {
            int damage = baseDamage;
            if (addStrengthModifier)
                damage += GetModifier(owner, "STR");
            return Mathf.Max(1, damage);
        }

        int GetModifier(TurnManager.TurnOwner owner, string key)
        {
            if (attributeManager == null)
                return 0;

            return attributeManager.GetAbilityModifier(owner, key);
        }

        void ApplyDamage(TurnManager.TurnOwner target, int amount, string context, TurnManager.TurnOwner attacker)
        {
            PlayAttackAnimation(attacker);
            if (lifeManager != null)
                lifeManager.TakeDamage(target, amount);

            if (logCombat)
                Debug.Log($"[Combat] {target} recebeu {amount} de dano. {context}");
        }

        void PlayAttackAnimation(TurnManager.TurnOwner attacker)
        {
            HeroAttackAnimator animator = attacker == TurnManager.TurnOwner.Player ? playerAnimator : enemyAnimator;
            animator?.Play();
        }

        void OnTurnStarted(TurnManager.TurnOwner owner)
        {
            if (owner == TurnManager.TurnOwner.Player)
                playerAttackUsed = false;
            else
                enemyAttackUsed = false;
        }

        void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnTurnStarted -= OnTurnStarted;
        }

        HeroAttackAnimator FindAnimator(TurnManager.TurnOwner owner)
        {
            var animators = FindObjectsByType<HeroAttackAnimator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var anim in animators)
            {
                if (anim == null) continue;
                var target = anim.GetComponent<AttackClickTarget>();
                if (target != null && target.owner == owner)
                    return anim;
            }

            return null;
        }

        void CacheHeroStatusHandlers()
        {
            var handlers = FindObjectsByType<HeroStatusEffectHandler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var handler in handlers)
            {
                var tracker = handler.GetComponent<HeroStatusTracker>();
                if (tracker == null)
                    continue;

                if (tracker.owner == TurnManager.TurnOwner.Player)
                    playerHeroStatus = handler;
                else if (tracker.owner == TurnManager.TurnOwner.Enemy)
                    enemyHeroStatus = handler;
            }
        }

        HeroStatusEffectHandler GetHeroStatusHandler(TurnManager.TurnOwner owner)
        {
            if (owner == TurnManager.TurnOwner.Player)
            {
                if (playerHeroStatus == null)
                    CacheHeroStatusHandlers();
                return playerHeroStatus;
            }

            if (enemyHeroStatus == null)
                CacheHeroStatusHandlers();
            return enemyHeroStatus;
        }
    }
}
