using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class DiceRoller_Stable2025 : MonoBehaviour
    {
        [Header("Configuração do Dado")]
        public int diceSides = 20;
        public float rollDuration = 2.0f;
        public float chaosSpinSpeed = 1080f;

        [Header("Mapeamento visual das faces (face 1 = índice 0)")]
        public List<Vector3> faceRotations = new List<Vector3>();

        [Header("Referências")]
        public TurnManager turnManager;
        public DeckManager deckManager;
        public EnergyManager energyManager;
        [SerializeField] TranscendentAttackManager transcendentAttackManager;

        [Header("Visual")]
        public Transform visualTransformOverride;
        public Renderer diceRenderer;
        public Color defaultColor = Color.white;

        [Header("Debug")]
        public bool enableDebugLogs = true;

        private bool isRolling = false;
        private Quaternion targetRotation;
        private Vector3 fixedPosition;
        private Transform visualTransform;

        void Start()
        {
            fixedPosition = transform.position;
            visualTransform = visualTransformOverride != null ? visualTransformOverride :
                          (diceRenderer != null ? diceRenderer.transform : transform);
            if (enableDebugLogs) Debug.Log("[DiceRoller] Ready.");
            if (transcendentAttackManager == null)
                transcendentAttackManager = FindFirstObjectByType<TranscendentAttackManager>();
        }

        void Update()
        {
            transform.position = fixedPosition;
        }

        private void OnMouseDown()
        {
            if (turnManager != null && turnManager.currentTurn != TurnManager.TurnOwner.Player)
            {
                if (enableDebugLogs) Debug.Log("[DiceRoller] Clique ignorado (não é turno do jogador).");
                return;
            }

            if (enableDebugLogs)
                Debug.Log("[DiceRoller] Clique 3D detectado - iniciando tentativa de rolagem.");

            TryStartPlayerRoll();
        }

        public void OnClickFromUI()
        {
            if (turnManager != null && turnManager.currentTurn != TurnManager.TurnOwner.Player)
            {
                if (enableDebugLogs) Debug.Log("[DiceRoller] UI Click ignorado (não é turno do jogador).");
                return;
            }

            TryStartPlayerRoll();
        }

        private void TryStartPlayerRoll()
        {
            if (isRolling)
            {
                if (enableDebugLogs) Debug.Log("[DiceRoller] Já rolando.");
                return;
            }

            bool attributeRoll = NeedsAttributeRoll(TurnManager.TurnOwner.Player);

            if (!attributeRoll && transcendentAttackManager != null &&
                transcendentAttackManager.HasPendingAttack(TurnManager.TurnOwner.Player))
            {
                RollDiceForTurn((res) => transcendentAttackManager.ResolvePendingAttack(res), true);
                return;
            }

            EnergyManager em = attributeRoll ? null : (energyManager != null ? energyManager : (turnManager != null ? turnManager.energyManager : null));
            if (em != null)
            {
                if (!em.CanRollDice(TurnManager.TurnOwner.Player))
                {
                    if (enableDebugLogs) Debug.Log("[DiceRoller] Jogador sem energia para rolar.");
                    return;
                }

                if (!em.ConsumeEnergyForRoll(TurnManager.TurnOwner.Player))
                {
                    if (enableDebugLogs) Debug.Log("[DiceRoller] Falha ao consumir energia do jogador.");
                    return;
                }
            }

            if (attributeRoll)
            {
                RollDiceForTurn((res) => HandleAttributeRollResult(TurnManager.TurnOwner.Player, res), true);
            }
            else
            {
                RollDiceForTurn((res) =>
                {
                    if (deckManager != null)
                        deckManager.ReceiveRoll(TurnManager.TurnOwner.Player, res);
                });
            }
        }

        public void RollDiceForTurn(Action<int> onCompleteCallback, bool skipEnergy = false)
        {
            if (isRolling)
            {
                if (enableDebugLogs) Debug.LogWarning("[DiceRoller] Já está rolando.");
                return;
            }

            if (!skipEnergy && turnManager != null && turnManager.currentTurn == TurnManager.TurnOwner.Enemy)
            {
                EnergyManager em = energyManager != null ? energyManager : (turnManager != null ? turnManager.energyManager : null);
                if (em != null)
                {
                    if (!em.CanRollDice(TurnManager.TurnOwner.Enemy))
                    {
                        if (enableDebugLogs) Debug.Log("[DiceRoller] Inimigo sem energia para rolar.");
                        return;
                    }

                    if (!em.ConsumeEnergyForRoll(TurnManager.TurnOwner.Enemy))
                    {
                        if (enableDebugLogs) Debug.Log("[DiceRoller] Falha ao consumir energia do inimigo.");
                        return;
                    }
                }
            }

            StartCoroutine(RollAnimationSequence(onCompleteCallback));
        }

        private IEnumerator RollAnimationSequence(Action<int> onCompleteCallback)
        {
            isRolling = true;

            int result = UnityEngine.Random.Range(1, diceSides + 1);
            if (enableDebugLogs) Debug.Log($"[DiceRoller] Resultado planejado: {result}");

            if (faceRotations != null && faceRotations.Count >= diceSides)
                targetRotation = Quaternion.Euler(faceRotations[result - 1]);
            else
                targetRotation = UnityEngine.Random.rotation;

            float elapsed = 0f;
            float chaosTime = rollDuration * 0.6f;
            Quaternion chaosStart = UnityEngine.Random.rotation;
            Quaternion chaosEnd = UnityEngine.Random.rotation;

            while (elapsed < chaosTime)
            {
                float t = elapsed / chaosTime;
                float speedFactor = Mathf.Lerp(1.4f, 0.6f, t);
                visualTransform.rotation = Quaternion.Slerp(chaosStart, chaosEnd, t) *
                                           Quaternion.Euler(Time.deltaTime * chaosSpinSpeed * speedFactor,
                                                            Time.deltaTime * chaosSpinSpeed * speedFactor,
                                                            Time.deltaTime * chaosSpinSpeed * speedFactor);
                elapsed += Time.deltaTime;
                yield return null;
            }

            float settleTime = Mathf.Max(0.01f, rollDuration - chaosTime);
            elapsed = 0f;
            Quaternion midRotation = visualTransform.rotation;

            while (elapsed < settleTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / settleTime);
                visualTransform.rotation = Quaternion.Slerp(midRotation, targetRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            visualTransform.rotation = targetRotation;
            yield return StartCoroutine(FinalWobble());

            if (enableDebugLogs) Debug.Log($"[DiceRoller] Dado parou visualmente no valor {result}");

            isRolling = false;
            onCompleteCallback?.Invoke(result);
        }

        private IEnumerator FinalWobble()
        {
            float wobbleDuration = 0.25f;
            float elapsed = 0f;
            Quaternion baseRot = visualTransform.rotation;
            float wobbleAngle = 2f;

            while (elapsed < wobbleDuration)
            {
                float t = elapsed / wobbleDuration;
                float wobble = Mathf.Sin(t * Mathf.PI) * (1f - t) * wobbleAngle;
                visualTransform.rotation = baseRot * Quaternion.Euler(wobble, wobble, -wobble);
                elapsed += Time.deltaTime;
                yield return null;
            }

            visualTransform.rotation = baseRot;
        }

        public void ResetForNewTurn()
        {
        }

        public void SetColor(Color c)
        {
            if (diceRenderer != null && diceRenderer.material != null)
                diceRenderer.material.color = c;
        }

        bool NeedsAttributeRoll(TurnManager.TurnOwner owner)
        {
            if (turnManager == null || turnManager.attributeManager == null) return false;
            return turnManager.IsWaitingForAttributeRoll(owner);
        }

        void HandleAttributeRollResult(TurnManager.TurnOwner owner, int result)
        {
            var attr = turnManager != null ? turnManager.attributeManager : null;
            if (attr == null)
            {
                if (enableDebugLogs) Debug.LogWarning("[DiceRoller] AttributeManager não está definido.");
                return;
            }

            bool applied = attr.TryApplyInitialRoll(owner, result);
            if (!applied)
            {
                if (enableDebugLogs) Debug.LogWarning("[DiceRoller] Rolagem de atributo ignorada (já definida?).");
                return;
            }

            turnManager?.NotifyAttributeRollComplete(owner);
        }
    }
}
