using System.Collections.Generic;
using UnityEngine;

public static class CardBattleRules
{
    // 선택한 행동에 맞춰 기본 공격 또는 카드 고유 효과를 적용한다.
    public static void ApplyAction(BattleCardRuntime actor, BattleCardRuntime target, IReadOnlyList<BattleCardRuntime> enemyField, BattleActionType actionType)
    {
        if (actor == null || target == null || actor.IsDead || target.IsDead)
            return;

        if (actionType == BattleActionType.Attack)
        {
            ApplyNormalAttack(actor, target);
            return;
        }

        switch (actor.Data.CardType)
        {
            case BattleCardType.Ranged:
                target.TakeDamage(actor.CurrentHp);
                break;


            case BattleCardType.Bomber:
                ApplyBomber(actor, target, enemyField);
                break;

            case BattleCardType.Vampire:
                ApplyVampire(actor, target);
                break;

            case BattleCardType.Berserker:
                ApplyBerserker(actor, target);
                break;

            case BattleCardType.Guardian:
                ApplyGuardian(actor, target);
                break;

            case BattleCardType.Piercing:
                ApplyPiercing(actor, target, enemyField);
                break;

            case BattleCardType.Musou:
                ApplyMusou(actor, target, enemyField);
                break;

            case BattleCardType.Healer:
            case BattleCardType.Normal:
            default:
                ApplyNormalAttack(actor, target);
                break;
        }
    }

    // 액티브 카드 효과 버튼을 사용할 수 있는 카드인지 확인한다.
    // 액티브 카드 효과 버튼을 사용할 수 있는 카드인지 확인한다.
    public static bool CanUseCardEffect(BattleCardRuntime card)
    {
        if (card == null || card.IsDead)
            return false;

        return card.Data.CardType == BattleCardType.Ranged
            || card.Data.CardType == BattleCardType.Musou
            || card.Data.CardType == BattleCardType.Bomber
            || card.Data.CardType == BattleCardType.Vampire
            || card.Data.CardType == BattleCardType.Berserker
            || card.Data.CardType == BattleCardType.Guardian
            || card.Data.CardType == BattleCardType.Piercing;
    }

    // 턴 시작 시 전장에 있는 힐러 카드들의 아군 회복 효과를 적용한다.
    public static void ApplyHealerTurnStart(IReadOnlyList<BattleCardRuntime> fieldCards)
    {
        for (int i = 0; i < fieldCards.Count; i++)
        {
            BattleCardRuntime healer = fieldCards[i]; // 회복 효과를 검사 중인 카드
            if (healer == null || healer.IsDead || healer.Data.CardType != BattleCardType.Healer)
                continue;

            for (int j = 0; j < fieldCards.Count; j++)
            {
                BattleCardRuntime target = fieldCards[j]; // 힐러가 회복할 아군 후보
                if (target == null || target == healer || target.IsDead)
                    continue;

                target.Heal(1);
            }
        }
    }

    // 일반 카드 공격을 처리한다. 반격 피해는 공격 전 대상의 현재 HP 기준이다.
    private static void ApplyNormalAttack(BattleCardRuntime actor, BattleCardRuntime target)
    {
        int counterDamage = target.CurrentHp; // 반격으로 받을 피해량
        target.TakeDamage(actor.CurrentHp);
        actor.TakeDamage(counterDamage);
    }

    // 무쌍 카드의 대상 피해와 인접 카드 추가 피해를 처리한다.
    private static void ApplyMusou(BattleCardRuntime actor, BattleCardRuntime target, IReadOnlyList<BattleCardRuntime> enemyField)
    {
        target.TakeDamage(actor.CurrentHp);

        List<BattleCardRuntime> adjacentTargets = new(); // 대상과 인접한 추가 피해 후보
        for (int i = 0; i < enemyField.Count; i++)
        {
            BattleCardRuntime card = enemyField[i]; // 인접 여부를 확인 중인 적 카드
            if (card == null || card == target || card.IsDead)
                continue;

            if (Mathf.Abs(card.SlotIndex - target.SlotIndex) == 1)
                adjacentTargets.Add(card);
        }

        if (adjacentTargets.Count == 0)
            return;

        BattleCardRuntime extraTarget = adjacentTargets[Random.Range(0, adjacentTargets.Count)]; // 랜덤으로 선택된 인접 피해 대상
        int extraDamage = Mathf.Max(1, Mathf.FloorToInt(actor.CurrentHp * 0.5f)); // 현재 HP의 50% 추가 피해
        extraTarget.TakeDamage(extraDamage);
    }

    // 폭탄 카드는 선택 대상에게 큰 피해를 주고 나머지 적 전장 카드에게 1의 광역 피해를 준다.
    private static void ApplyBomber(BattleCardRuntime actor, BattleCardRuntime target, IReadOnlyList<BattleCardRuntime> enemyField)
    {
        target.TakeDamage(actor.CurrentHp);

        for (int i = 0; i < enemyField.Count; i++)
        {
            BattleCardRuntime splashTarget = enemyField[i]; // 폭발 여파를 받을 수 있는 다른 적 카드
            if (splashTarget == null || splashTarget == target || splashTarget.IsDead)
                continue;

            splashTarget.TakeDamage(1);
        }
    }

    // 흡혈 카드는 피해를 준 뒤 자신을 회복해 생존력을 높인다.
    private static void ApplyVampire(BattleCardRuntime actor, BattleCardRuntime target)
    {
        target.TakeDamage(actor.CurrentHp);
        actor.Heal(2);
    }

    // 광전사 카드는 잃은 HP만큼 추가 피해를 주고, 사용 후 반동 피해를 받는다.
    private static void ApplyBerserker(BattleCardRuntime actor, BattleCardRuntime target)
    {
        int missingHp = actor.Data.MaxHp - actor.CurrentHp; // 잃은 체력만큼 증가하는 추가 피해
        target.TakeDamage(actor.CurrentHp + missingHp);
        actor.TakeDamage(1);
    }

    // 수호자 카드는 낮은 피해를 주는 대신 자신을 회복해 방어형 운영을 만든다.
    private static void ApplyGuardian(BattleCardRuntime actor, BattleCardRuntime target)
    {
        int damage = Mathf.Max(1, Mathf.CeilToInt(actor.CurrentHp * 0.5f)); // 수호자 카드효과 피해량
        target.TakeDamage(damage);
        actor.Heal(1);
    }

    // 관통 카드는 선택 대상에게 피해를 주고 양옆 적에게 1 피해를 준다.
    private static void ApplyPiercing(BattleCardRuntime actor, BattleCardRuntime target, IReadOnlyList<BattleCardRuntime> enemyField)
    {
        target.TakeDamage(actor.CurrentHp);

        for (int i = 0; i < enemyField.Count; i++)
        {
            BattleCardRuntime adjacentTarget = enemyField[i]; // 관통 여파를 받을 인접 카드
            if (adjacentTarget == null || adjacentTarget == target || adjacentTarget.IsDead)
                continue;

            if (Mathf.Abs(adjacentTarget.SlotIndex - target.SlotIndex) == 1)
                adjacentTarget.TakeDamage(1);
        }
    }

}
