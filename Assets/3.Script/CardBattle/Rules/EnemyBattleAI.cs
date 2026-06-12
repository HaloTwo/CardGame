using System.Collections.Generic;

public class EnemyBattleAI
{
    // 적 전장의 첫 생존 카드를 행동 카드로 고르고, 플레이어의 가장 낮은 HP 카드를 공격 대상으로 고른다.
    public bool TryPickAction(IReadOnlyList<BattleCardRuntime> enemyField, IReadOnlyList<BattleCardRuntime> playerField, out BattleCardRuntime actor, out BattleCardRuntime target)
    {
        actor = null;
        target = null;

        for (int i = 0; i < enemyField.Count; i++)
        {
            if (enemyField[i] != null && !enemyField[i].IsDead)
            {
                actor = enemyField[i];
                break;
            }
        }

        for (int i = 0; i < playerField.Count; i++)
        {
            BattleCardRuntime candidate = playerField[i]; // 공격 대상 후보
            if (candidate == null || candidate.IsDead)
                continue;

            if (target == null || candidate.CurrentHp < target.CurrentHp)
                target = candidate;
        }

        return actor != null && target != null;
    }
}
