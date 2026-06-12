using System.Collections.Generic;

public class BattleField
{
    private const int FieldSlotCount = 3; // 전장에 공개되는 카드 슬롯 수

    private readonly List<BattleCardRuntime> deck = new(); // 아직 공개되지 않은 대기 카드 목록
    private readonly List<BattleCardRuntime> fieldCards = new(FieldSlotCount); // 현재 전장에 공개된 카드 목록

    public CardOwner Owner { get; } // 이 전장의 소유자
    public IReadOnlyList<BattleCardRuntime> FieldCards => fieldCards; // 룰과 UI에서 읽는 공개 카드 목록
    public int RemainDeckCount => deck.Count; // UI에 표시할 대기 카드 수

    // 소유자를 지정하고 전장 슬롯 3칸을 비워둔다.
    public BattleField(CardOwner owner)
    {
        Owner = owner;

        for (int i = 0; i < FieldSlotCount; i++)
            fieldCards.Add(null);
    }

    // 전투 시작 또는 재시작 시 덱과 전장 슬롯을 초기 상태로 되돌린다.
    public void Reset(List<BattleCardRuntime> startDeck)
    {
        deck.Clear();
        deck.AddRange(startDeck);

        for (int i = 0; i < fieldCards.Count; i++)
            fieldCards[i] = null;

        FillEmptySlots();
    }

    // 카드 사망 후 빈 슬롯이 생겼을 때 대기 카드가 있으면 즉시 공개 배치한다.
    public void ResolveDeadAndFill()
    {
        for (int i = 0; i < fieldCards.Count; i++)
        {
            if (fieldCards[i] != null && fieldCards[i].IsDead)
                fieldCards[i] = null;
        }

        FillEmptySlots();
    }

    // UI 슬롯 인덱스에 대응하는 전장 카드를 반환한다.
    public BattleCardRuntime GetSlotCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= fieldCards.Count)
            return null;

        return fieldCards[slotIndex];
    }

    // 전장 위에 살아있는 카드가 하나라도 있는지 확인한다.
    public bool HasAnyAliveCard()
    {
        for (int i = 0; i < fieldCards.Count; i++)
        {
            BattleCardRuntime card = fieldCards[i]; // 검사 중인 전장 카드
            if (card != null && !card.IsDead)
                return true;
        }

        return false;
    }

    // 전장 또는 대기 덱에 아직 사용할 카드가 남았는지 확인한다.
    public bool HasAnyRemainCard()
    {
        return HasAnyAliveCard() || deck.Count > 0;
    }

    // 빈 전장 슬롯을 앞쪽 대기 카드로 자동 채운다.
    private void FillEmptySlots()
    {
        for (int i = 0; i < fieldCards.Count; i++)
        {
            if (fieldCards[i] != null || deck.Count == 0)
                continue;

            BattleCardRuntime next = deck[0]; // 새로 공개할 대기 카드
            deck.RemoveAt(0);
            next.SlotIndex = i;
            fieldCards[i] = next;
        }
    }
}
