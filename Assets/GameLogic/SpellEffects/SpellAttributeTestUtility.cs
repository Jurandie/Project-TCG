using UnityEngine;

namespace GameLogic
{
    public enum SpellAttributeTestOutcome
    {
        CriticalFail,
        Fail,
        Success,
        CriticalSuccess
    }

    public struct SpellAttributeTestResult
    {
        public SpellAttributeTestOutcome outcome;
        public int roll;
        public int modifier;
        public int total;

        public bool IsCriticalSuccess => outcome == SpellAttributeTestOutcome.CriticalSuccess;
        public bool IsCriticalFail => outcome == SpellAttributeTestOutcome.CriticalFail;

        public override string ToString()
        {
            return $"Roll={roll} Mod={modifier} Total={total} Outcome={outcome}";
        }
    }

    public static class SpellAttributeTestUtility
    {
        public static SpellAttributeTestResult PerformTest(SpellCastContext context, string attributeKey, int difficultyClass)
        {
            int roll = Random.Range(1, 21);
            int modifier = 0;
            if (context != null && context.attributeManager != null)
                modifier = context.attributeManager.GetAbilityModifier(context.owner, attributeKey);

            var result = new SpellAttributeTestResult
            {
                roll = roll,
                modifier = modifier,
                total = roll + modifier,
                outcome = SpellAttributeTestOutcome.Fail
            };

            if (roll == 1)
            {
                result.outcome = SpellAttributeTestOutcome.CriticalFail;
                return result;
            }

            if (roll == 20)
            {
                result.outcome = SpellAttributeTestOutcome.CriticalSuccess;
                return result;
            }

            result.outcome = result.total >= difficultyClass ? SpellAttributeTestOutcome.Success : SpellAttributeTestOutcome.Fail;
            return result;
        }
    }
}
