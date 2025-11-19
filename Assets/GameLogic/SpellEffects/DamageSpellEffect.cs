using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "DamageSpellEffect", menuName = "Card Game/Spell Effects/Damage")]
    public class DamageSpellEffect : CardSpellEffect
    {
        public int minDamage = 2;
        public int maxDamage = 6;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null || context.lifeManager == null)
                return;

            int amount = Random.Range(minDamage, maxDamage + 1);
            context.lifeManager.TakeDamage(context.target, amount);
            Debug.Log($"[SpellEffect] {context.owner} conjurou dano {amount} em {context.target}.");
        }
    }
}
