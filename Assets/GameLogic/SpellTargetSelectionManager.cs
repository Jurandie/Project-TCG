using UnityEngine;

namespace GameLogic
{
    public enum SpellCastOutcome
    {
        CriticalFail,
        Fail,
        Success,
        CriticalSuccess
    }

    public struct SpellTargetChoice
    {
        public bool isHero;
        public TurnManager.TurnOwner heroOwner;
        public CardUI monster;

        public static SpellTargetChoice Hero(TurnManager.TurnOwner owner)
        {
            return new SpellTargetChoice { isHero = true, heroOwner = owner };
        }

        public static SpellTargetChoice Monster(CardUI card)
        {
            return new SpellTargetChoice { isHero = false, monster = card };
        }
    }

    public class SpellTargetSelectionManager : MonoBehaviour
    {
        [SerializeField] DiceRoller_Stable2025 diceRoller;
        [SerializeField] CharacterAttributeManager attributeManager;

        SpellCastContext pendingContext;
        UnstableReanimationSpellEffect pendingEffect;
        SpellTargetChoice pendingChoice;
        TurnManager.TurnOwner expectedTargetOwner;
        bool awaitingRoll;

        void Awake()
        {
            if (diceRoller == null)
                diceRoller = FindFirstObjectByType<DiceRoller_Stable2025>();
            if (attributeManager == null)
            {
                var tm = FindFirstObjectByType<TurnManager>();
                if (tm != null)
                    attributeManager = tm.attributeManager;
            }
        }

        public bool IsSelecting => pendingEffect != null;
        public TurnManager.TurnOwner ExpectedTargetOwner => expectedTargetOwner;
        bool TargetingAllies => pendingContext != null && pendingContext.targetAllies;

        public void BeginSelection(SpellCastContext context, UnstableReanimationSpellEffect effect)
        {
            if (context == null || effect == null)
                return;

            pendingContext = context;
            pendingEffect = effect;
            bool targetAllies = TargetingAllies;
            expectedTargetOwner = targetAllies ? context.owner :
                (context.owner == TurnManager.TurnOwner.Player ? TurnManager.TurnOwner.Enemy : TurnManager.TurnOwner.Player);
            context.DeferCleanup();
            Debug.Log(targetAllies
                ? "[SpellTarget] Selecione um herói aliado ou monstro transcendente para receber a cura."
                : "[SpellTarget] Selecione um herói ou monstro inimigo para a magia.");
        }

        public bool TrySelectHero(TurnManager.TurnOwner target)
        {
            if (!IsSelecting || awaitingRoll)
                return false;

            if (target != expectedTargetOwner)
            {
                Debug.Log("[SpellTarget] Este herói não é um alvo válido.");
                return false;
            }

            pendingChoice = SpellTargetChoice.Hero(target);
            TriggerRoll();
            return true;
        }

        public bool TrySelectMonster(CardUI card)
        {
            if (!IsSelecting || awaitingRoll || card == null)
                return false;
            if (card.IsEquipment)
            {
                Debug.Log("[SpellTarget] Equipamentos não podem ser alvos da magia.");
                return false;
            }

            bool targetingAllies = TargetingAllies;
            var slot = card.CurrentSlot;
            var zone = slot != null ? slot.ParentZone : null;
            MonsterZone expectedZone = targetingAllies ? GetCasterZone(true) : GetCasterZone(false);

            bool zoneMatches = true;
            if (expectedZone != null)
                zoneMatches = zone == expectedZone;
            else if (zone != null)
                zoneMatches = zone.owner == expectedTargetOwner;

            if (slot == null || zone == null || !zoneMatches)
            {
                Debug.Log(targetingAllies
                    ? "[SpellTarget] Esta carta não pertence ao seu lado."
                    : "[SpellTarget] Esta carta não pertence ao oponente.");
                return false;
            }

            if (targetingAllies && !card.IsTranscendent)
            {
                Debug.Log("[SpellTarget] Apenas monstros transcendentes aliados podem receber essa magia.");
                return false;
            }

            pendingChoice = SpellTargetChoice.Monster(card);
            TriggerRoll();
            return true;
        }

        MonsterZone GetCasterZone(bool wantCasterZone)
        {
            if (pendingContext == null)
                return null;

            bool casterIsPlayer = pendingContext.owner == TurnManager.TurnOwner.Player;
            if (wantCasterZone)
                return casterIsPlayer ? pendingContext.playerZone : pendingContext.enemyZone;
            return casterIsPlayer ? pendingContext.enemyZone : pendingContext.playerZone;
        }

        void TriggerRoll()
        {
            if (diceRoller == null)
            {
                Debug.LogWarning("[SpellTarget] DiceRoller não atribuído.");
                CancelSelection();
                return;
            }

            awaitingRoll = true;
            diceRoller.RollDiceForTurn(OnRollComplete, true);
        }

        void OnRollComplete(int result)
        {
            awaitingRoll = false;
            if (pendingEffect == null || pendingContext == null)
                return;

            var outcome = EvaluateOutcome(result);
            pendingEffect.ResolveAfterDice(pendingContext, pendingChoice, outcome);
            pendingEffect = null;
            pendingContext = null;
        }

        SpellCastOutcome EvaluateOutcome(int roll)
        {
            if (roll <= 1) return SpellCastOutcome.CriticalFail;
            if (roll >= 20) return SpellCastOutcome.CriticalSuccess;

            int attackMod = attributeManager != null ? attributeManager.GetAbilityModifier(pendingContext.owner, "INT") : 0;

            TurnManager.TurnOwner defenderOwner = pendingChoice.isHero ? pendingChoice.heroOwner :
                (pendingChoice.monster?.CurrentSlot?.ParentZone?.owner ?? expectedTargetOwner);

            int defenseMod = attributeManager != null ? attributeManager.GetAbilityModifier(defenderOwner, "DEX") : 0;
            int targetNumber = 10 + defenseMod;
            return (roll + attackMod >= targetNumber) ? SpellCastOutcome.Success : SpellCastOutcome.Fail;
        }

        public void CancelSelection()
        {
            pendingEffect = null;
            pendingContext = null;
            awaitingRoll = false;
        }
    }
}
