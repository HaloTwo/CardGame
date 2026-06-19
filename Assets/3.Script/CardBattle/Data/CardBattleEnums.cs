// 카드가 플레이어 쪽인지 적 쪽인지 구분한다.
public enum CardOwner
{
    Player,
    Enemy
}

// 현재 턴을 가진 진영을 구분한다.
public enum BattleTurn
{
    Player,
    Enemy
}

// 플레이어 입력 단계와 전투 종료 상태를 구분한다.
public enum BattleState
{
    Ready,
    PlayerSelectCard,
    PlayerSelectAction,
    PlayerSelectTarget,
    EnemyActing,
    GameOver
}

// 플레이어가 선택할 수 있는 행동 종류를 구분한다.
public enum BattleActionType
{
    Attack,
    Skill
}

// 과제에서 요구한 카드 타입을 구분한다.
public enum BattleCardType
{
    Normal,
    Ranged,
    Musou,
    Healer,
    Bomber,
    Vampire,
    Berserker,
    Guardian,
    Piercing
}
