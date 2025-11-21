using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "Heroism", menuName = "Card Game/Spell Effects/Paladin/Heroism")]
    public class HeroismSpellEffect : CardSpellEffect
    {
        public string attributeKey = "CON";
        public int difficultyClass = 12;
        public int healPerTurn = 2;
        public int durationInTurns = 3;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            var test = SpellAttributeTestUtility.PerformTest(context, attributeKey, difficultyClass);

            if (test.outcome == SpellAttributeTestOutcome.CriticalFail)
            {
                Debug.Log("[Heroism] Falha crítica! O herói fica exausto e perde 1 de vida.");
                context.lifeManager?.TakeDamage(context.owner, 1);
                context.FinalizeSpell();
                return;
            }

            int turns = durationInTurns;
            int heal = healPerTurn;

            if (test.outcome == SpellAttributeTestOutcome.Fail)
            {
                turns = Mathf.Max(1, durationInTurns - 1);
                heal = Mathf.Max(1, healPerTurn - 1);
            }
            else if (test.outcome == SpellAttributeTestOutcome.CriticalSuccess)
            {
                turns += 1;
                heal += 1;
            }

            var host = context.turnManager != null ? context.turnManager.gameObject : (context.lifeManager != null ? context.lifeManager.gameObject : null);
            if (host == null)
            {
                Debug.LogWarning("[Heroism] Nenhum host disponível para aplicar o efeito contínuo.");
                context.FinalizeSpell();
                return;
            }

            var regen = host.AddComponent<HeroRegenerationEffect>();
            regen.Initialize(context.lifeManager, context.turnManager, context.owner, heal, turns);
            Debug.Log($"[Heroism] {context.owner} receberá {heal} de cura por {turns} turnos. Teste: {test}");
            context.FinalizeSpell();
        }
    }
}
