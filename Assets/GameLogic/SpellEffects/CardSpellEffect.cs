using UnityEngine;

namespace GameLogic
{
    public abstract class CardSpellEffect : ScriptableObject
    {
        public abstract void Resolve(SpellCastContext context);
    }
}
