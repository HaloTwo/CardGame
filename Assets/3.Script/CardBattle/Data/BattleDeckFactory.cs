using System.Collections.Generic;
using UnityEngine;

public static class BattleDeckFactory
{
    // 프로토타입용 기본 6장 덱을 생성합니다. 추후 SO나 CSV로 분리할 수 있습니다.
    public static List<BattleCardRuntime> CreateDefaultDeck(CardOwner owner)
    {
        return new List<BattleCardRuntime>
        {
            Create("버거", BattleCardType.Normal, 6, owner),
            Create("감자튀김", BattleCardType.Ranged, 4, owner),
            Create("몬스터", BattleCardType.Musou, 7, owner),
            Create("소다", BattleCardType.Healer, 5, owner),
            Create("폭탄 소스", BattleCardType.Bomber, 4, owner),
            Create("흡혈 쉐이크", BattleCardType.Vampire, 5, owner)
        };
    }

    // 인스펙터에 연결된 카드 정의 목록으로 전투 덱을 생성합니다.
    public static List<BattleCardRuntime> CreateDeck(BattleCardDefinition[] definitions, CardOwner owner)
    {
        if (owner == CardOwner.Player)
            return CardPlayerProfile.CreatePlayerDeck(owner);

        if (definitions == null || definitions.Length == 0)
            return CreateDefaultDeck(owner);

        List<BattleCardRuntime> deck = new(); // 생성된 전투 카드 목록
        for (int i = 0; i < definitions.Length; i++)
        {
            BattleCardDefinition definition = definitions[i]; // 덱에 넣을 카드 정의
            if (definition == null)
                continue;

            deck.Add(new BattleCardRuntime(definition.ToData(), owner));
        }

        return deck.Count > 0 ? deck : CreateDefaultDeck(owner);
    }

    // 카드 데이터와 전투 소유자 상태를 한 번에 만듭니다.
    private static BattleCardRuntime Create(string cardName, BattleCardType cardType, int maxHp, CardOwner owner)
    {
        string abilityText = GetDefaultAbilityText(cardType); // 카드 타입별 기본 능력 설명
        Color cardColor = GetDefaultColor(cardType); // 이미지가 없을 때 구분할 임시 색상
        return new BattleCardRuntime(new BattleCardData(cardName, cardType, maxHp, null, abilityText, cardColor), owner);
    }

    // 카드 타입별 기본 능력 설명을 반환합니다.
    private static string GetDefaultAbilityText(BattleCardType cardType)
    {
        return cardType switch
        {
            BattleCardType.Ranged => "효과: 반격 없이 현재 HP 피해",
            BattleCardType.Musou => "효과: 대상 100% + 인접 1장 50%",
            BattleCardType.Bomber => "효과: 대상 현재 HP 피해 + 나머지 적 1 피해",
            BattleCardType.Vampire => "효과: 현재 HP 피해 + 자신 HP 2 회복",
            BattleCardType.Berserker => "효과: 잃은 HP만큼 추가 피해, 자신 1 피해",
            BattleCardType.Guardian => "효과: 절반 피해 + 자신 HP 1 회복",
            BattleCardType.Piercing => "효과: 대상 피해 + 양옆 적 1 피해",
            BattleCardType.Healer => "패시브: 턴 시작 시 아군 HP 1 회복",
            _ => "기본: 현재 HP 피해 + 반격"
        };
    }

    // 카드 타입별 임시 색상을 반환합니다.
    private static Color GetDefaultColor(BattleCardType cardType)
    {
        return cardType switch
        {
            BattleCardType.Ranged => new Color(0.45f, 0.75f, 1f, 1f),
            BattleCardType.Musou => new Color(1f, 0.45f, 0.35f, 1f),
            BattleCardType.Bomber => new Color(1f, 0.55f, 0.12f, 1f),
            BattleCardType.Vampire => new Color(0.78f, 0.22f, 0.82f, 1f),
            BattleCardType.Berserker => new Color(0.95f, 0.16f, 0.16f, 1f),
            BattleCardType.Guardian => new Color(0.36f, 0.62f, 1f, 1f),
            BattleCardType.Piercing => new Color(0.82f, 0.82f, 0.92f, 1f),
            BattleCardType.Healer => new Color(0.45f, 1f, 0.55f, 1f),
            _ => new Color(1f, 0.86f, 0.45f, 1f)
        };
    }
}
