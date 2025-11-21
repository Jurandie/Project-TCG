using UnityEngine;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "UnstableReanimation", menuName = "Card Game/Spell Effects/Unstable Reanimation")]
    public class UnstableReanimationSpellEffect : CardSpellEffect
    {
        [Header("Bônus / Multiplicadores")]
        public int flatAttackBonus = 6;
        public int flatDefenseBonus = 4;
        public float attackMultiplier = 1.4f;
        public float defenseMultiplier = 1.2f;

        [Header("Tempo de Vida")]
        public int turnsAlive = 2;

        [Header("Dano do Feitico")]
        public int baseDamage = 5;
        public float criticalMultiplier = 2f;

        [Header("Versao Sagrada")]
        public Sprite sacredArtwork;
        public string sacredNameSuffix = " (Sagrado)";
        [TextArea]
        public string sacredDescriptionOverride;

        public override void Resolve(SpellCastContext context)
        {
            if (context == null)
                return;

            if (context.targetSelectionManager != null)
            {
                context.targetSelectionManager.BeginSelection(context, this);
                return;
            }

            // fallback: dano automático no herói inimigo
            ResolveAfterDice(context, SpellTargetChoice.Hero(context.target), SpellCastOutcome.Success);
        }

        public void ResolveAfterDice(SpellCastContext context, SpellTargetChoice choice, SpellCastOutcome outcome)
        {
            if (context == null)
                return;

            bool isSacred = context.isSacredSpell;

            if (outcome == SpellCastOutcome.CriticalFail)
            {
                Debug.Log("[UnstableReanimation] Falha crítica! A magia foi cancelada.");
                if (!isSacred)
                    GiveSacredCopyToOpponent(context);
                context.FinalizeSpell();
                return;
            }

            int effectAmount = baseDamage;
            if (outcome == SpellCastOutcome.CriticalSuccess)
                effectAmount = Mathf.RoundToInt(effectAmount * criticalMultiplier);
            else if (outcome == SpellCastOutcome.Fail)
                effectAmount = Mathf.Max(1, effectAmount / 2);

            if (isSacred)
            {
                ApplySacredResolution(context, choice, Mathf.Max(1, effectAmount));
                context.FinalizeSpell();
                return;
            }

            bool allowCorruption = outcome == SpellCastOutcome.Success || outcome == SpellCastOutcome.CriticalSuccess;
            Card capturedCard = null;
            if (choice.isHero)
            {
                if (context.lifeManager != null)
                {
                    context.lifeManager.TakeDamage(choice.heroOwner, effectAmount);
                    context.lifeManager.Heal(context.owner, Mathf.Max(1, effectAmount / 2));
                    Debug.Log($"[UnstableReanimation] Herói {choice.heroOwner} sofreu {effectAmount} de dano.");
                }
            }
            else if (choice.monster != null)
            {
                capturedCard = ApplyDamageToMonster(context, choice.monster, effectAmount, allowCorruption);
            }

            if (capturedCard == null)
            {
                capturedCard = allowCorruption ? PullFromDiscard(context) : null;
                if (capturedCard != null && allowCorruption)
                    SummonCorruptedCopy(context, capturedCard);
            }

            context.FinalizeSpell();
        }

        void ApplySacredResolution(SpellCastContext context, SpellTargetChoice choice, int healAmount)
        {
            if (choice.isHero)
            {
                context.lifeManager?.Heal(choice.heroOwner, healAmount);
                Debug.Log($"[UnstableReanimation] Herói {choice.heroOwner} foi curado em {healAmount}.");
                return;
            }

            if (choice.monster != null)
                HealMonster(choice.monster, healAmount);
        }

        void HealMonster(CardUI targetCard, int amount)
        {
            if (targetCard == null)
                return;

            var runtime = targetCard.runtimeCard;
            if (runtime == null)
                return;

            int maxPoints = runtime.MaxHealth > 0 ? runtime.MaxHealth : Mathf.Max(runtime.Defense, 1);
            runtime.Defense = Mathf.Clamp(runtime.Defense + amount, 0, maxPoints);
            targetCard.Setup(runtime);
            targetCard.ShowFront();
            Debug.Log($"[UnstableReanimation] Monstro restaurado em {amount} pontos (DEF atual {runtime.Defense}).");
        }

        Card ApplyDamageToMonster(SpellCastContext context, CardUI targetCard, int damage, bool allowCorruption)
        {
            var runtime = targetCard.runtimeCard;
            int remaining = runtime != null ? runtime.Defense : targetCard.CurrentVisuals.defense;
            remaining -= damage;
            if (runtime != null)
                runtime.Defense = Mathf.Max(0, remaining);

            if (remaining > 0)
            {
                targetCard.ShowFront();
                Debug.Log($"[UnstableReanimation] Monstro sofreu {damage} de dano (DEF restante {remaining}).");
                return null;
            }

            if (!allowCorruption)
            {
                var slot = targetCard.CurrentSlot;
                if (slot != null)
                    slot.ClearSlot();
                Destroy(targetCard.gameObject);
                Debug.Log("[UnstableReanimation] Monstro destruído mas resistiu ao efeito (sem corrupção).");
                return null;
            }

            Debug.Log("[UnstableReanimation] Monstro inimigo foi destruído e será corrompido.");
            var captured = CloneCardState(targetCard);
            var targetSlot = targetCard.CurrentSlot;
            if (targetSlot != null)
                targetSlot.ClearSlot();
            Destroy(targetCard.gameObject);
            return captured;
        }

        Card PullFromDiscard(SpellCastContext context)
        {
            Deck sourceDeck = context.owner == TurnManager.TurnOwner.Player
                ? context.deckManager.playerDeck
                : context.deckManager.enemyDeck;

            if (sourceDeck == null)
                return null;

            var data = sourceDeck.TakeFromDiscard();
            if (data == null)
            {
                Debug.Log("[UnstableReanimation] Nenhuma carta encontrada no cemitério para reanimar.");
                return null;
            }

            return new Card(data);
        }

        void SummonCorruptedCopy(SpellCastContext context, Card sourceCard)
        {
            if (sourceCard == null || context.handManager == null)
                return;

            MonsterZone zone = context.owner == TurnManager.TurnOwner.Player ? context.playerZone : context.enemyZone;
            if (zone == null)
                return;

            MonsterZoneSlot slot = zone.GetFirstEmptySlot();
            if (slot == null)
            {
                Debug.Log("[UnstableReanimation] Sem espaço para invocar a carta corrompida.");
                return;
            }

            GameObject prefab = context.handManager.cardPrefab;
            if (prefab == null)
                return;

            Card resurrectedCard = BuildCorruptedCard(context, sourceCard);

            GameObject cardGO = Instantiate(prefab, slot.transform.parent);
            cardGO.SetActive(true);

            var cardUI = cardGO.GetComponent<CardUI>();
            if (cardUI == null)
            {
                Destroy(cardGO);
                return;
            }

            cardUI.ResetForHandContainer();
            cardUI.Setup(resurrectedCard);
            cardUI.SetOwner(context.owner);

            if (!slot.TryPlaceCard(cardUI))
            {
                Destroy(cardGO);
                return;
            }

            var lifetime = cardGO.AddComponent<CorruptedLifetime>();
            lifetime.Initialize(context.turnManager, slot, turnsAlive, context.owner);
        }

        Card BuildCorruptedCard(SpellCastContext context, Card baseCard)
        {
            Card card = CloneCard(baseCard);
            int baseAttack = Mathf.RoundToInt(card.Attack * attackMultiplier) + flatAttackBonus;
            int baseDefense = Mathf.RoundToInt(card.Defense * defenseMultiplier) + flatDefenseBonus;

            var stats = context.attributeManager != null ? context.attributeManager.GetStats(context.owner) : null;

            int atkHalf = stats != null ? Mathf.Max(1, stats.strength / 2) : baseAttack;
            int defHalf = stats != null ? Mathf.Max(1, stats.dexterity / 2) : baseDefense;
            int hpHalf = stats != null ? Mathf.Max(1, stats.maxLife / 2) : card.MaxHealth;

            card.Attack = Random.Range(1, atkHalf + 1);
            card.Defense = Random.Range(1, defHalf + 1);
            card.MaxHealth = Random.Range(1, hpHalf + 1);
            return card;
        }

        Card CloneCardState(CardUI cardUI)
        {
            if (cardUI == null)
                return null;

            if (cardUI.runtimeCard != null)
                return CloneCard(cardUI.runtimeCard);

            if (cardUI.cardData != null)
                return new Card(cardUI.cardData);

            return null;
        }

        Card CloneCard(Card original)
        {
            if (original == null)
                return null;

            Card copy = new Card(original.SourceData ?? original.SourceData);
            copy.Attack = original.Attack;
            copy.Defense = original.Defense;
            copy.MaxHealth = original.MaxHealth;
            copy.Armor = original.Armor;
            copy.EnergyCost = original.EnergyCost;
            copy.Keywords = original.Keywords;
            copy.LoreDescription = original.LoreDescription;
            copy.Kind = original.Kind;
            copy.CurrentTier = original.CurrentTier;
            copy.Durability = original.Durability;
            copy.DurabilityDisabled = original.DurabilityDisabled;
            copy.IsTranscendentFormAvailable = original.IsTranscendentFormAvailable;
            copy.IsSacredSpell = original.IsSacredSpell;
            copy.sacredArtworkOverride = original.sacredArtworkOverride;
            return copy;
        }

        void GiveSacredCopyToOpponent(SpellCastContext context)
        {
            if (context == null || context.handManager == null)
                return;

            Card sacredCard = null;
            if (context.card != null)
            {
                if (context.card.runtimeCard != null)
                    sacredCard = CloneCard(context.card.runtimeCard);
                else if (context.card.cardData != null)
                    sacredCard = new Card(context.card.cardData);
            }

            if (sacredCard == null)
                return;

            sacredCard.IsSacredSpell = true;
            sacredCard.Kind = CardData.CardType.Spell;
            if (sacredArtwork != null)
                sacredCard.sacredArtworkOverride = sacredArtwork;
            if (!string.IsNullOrEmpty(sacredNameSuffix))
                sacredCard.Name = string.Concat(sacredCard.Name, sacredNameSuffix);
            if (!string.IsNullOrEmpty(sacredDescriptionOverride))
                sacredCard.LoreDescription = sacredDescriptionOverride;

            bool giveToEnemy = context.owner == TurnManager.TurnOwner.Player;
            context.handManager.AddCardToHand(sacredCard, giveToEnemy);
            Debug.Log("[UnstableReanimation] Falha critica criou uma versao sagrada da magia para o oponente.");
        }
    }
}



