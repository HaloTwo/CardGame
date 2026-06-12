using System;

public class BattleCardRuntime
{
    public BattleCardData Data { get; } // 카드 고정 데이터
    public CardOwner Owner { get; } // 카드 소유 진영
    public int SlotIndex { get; set; } // 현재 전장 슬롯 위치
    public int CurrentHp { get; private set; } // 현재 체력
    public bool IsDead => CurrentHp <= 0; // 사망 여부

    public event Action<BattleCardRuntime> OnChanged; // HP 변경 시 UI 갱신용 이벤트

    // 전투에 올라갈 카드 런타임 상태를 생성한다.
    public BattleCardRuntime(BattleCardData data, CardOwner owner)
    {
        Data = data;
        Owner = owner;
        CurrentHp = data.MaxHp;
        SlotIndex = -1;
    }

    // 피해를 적용하고 HP 변경 이벤트를 발생시킨다.
    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHp = Math.Max(0, CurrentHp - amount);
        OnChanged?.Invoke(this);
    }

    // 최대 체력을 넘지 않는 범위에서 회복을 적용한다.
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHp = Math.Min(Data.MaxHp, CurrentHp + amount);
        OnChanged?.Invoke(this);
    }
}
