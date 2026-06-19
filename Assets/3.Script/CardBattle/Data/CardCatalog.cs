using System;
using UnityEngine;

public static class CardCatalog
{
    public const int DeckSize = 6; /* 전투에 편성하는 카드 수 */

    private static readonly CardCatalogEntry[] cards =
    {
        new("burger", "버거", BattleCardType.Normal, 6, "기본 공격: 현재 HP 피해를 주고 반격 피해를 받습니다.", new Color(1f, 0.86f, 0.45f, 1f), true),
        new("fries", "감자튀김", BattleCardType.Ranged, 4, "스킬: 반격 없이 현재 HP만큼 피해를 줍니다.", new Color(0.45f, 0.75f, 1f, 1f), true),
        new("monster", "몬스터", BattleCardType.Musou, 7, "스킬: 대상에게 100%, 인접한 적 1장에게 50% 피해를 줍니다.", new Color(1f, 0.45f, 0.35f, 1f), true),
        new("soda", "소다", BattleCardType.Healer, 5, "패시브: 턴 시작 시 자신을 제외한 아군 HP를 1 회복합니다.", new Color(0.45f, 1f, 0.55f, 1f), true),
        new("bomb_sauce", "폭탄 소스", BattleCardType.Bomber, 4, "스킬: 대상에게 현재 HP 피해를 주고 나머지 적에게 1 피해를 줍니다.", new Color(1f, 0.55f, 0.12f, 1f), true),
        new("vampire_shake", "흡혈 쉐이크", BattleCardType.Vampire, 5, "스킬: 현재 HP 피해를 주고 자신 HP를 2 회복합니다.", new Color(0.78f, 0.22f, 0.82f, 1f), true),
        new("spicy_berserker", "매운 광전사", BattleCardType.Berserker, 6, "스킬: 잃은 HP만큼 추가 피해를 주고 자신도 1 피해를 받습니다.", new Color(0.95f, 0.16f, 0.16f, 1f), false),
        new("guard_burger", "수호 버거", BattleCardType.Guardian, 8, "스킬: 절반 피해를 주고 자신 HP를 1 회복합니다.", new Color(0.36f, 0.62f, 1f, 1f), false),
        new("skewer", "꼬치", BattleCardType.Piercing, 4, "스킬: 대상 피해와 함께 양옆 적에게 1 피해를 줍니다.", new Color(0.82f, 0.82f, 0.92f, 1f), false),
        new("double_patty", "더블 패티", BattleCardType.Normal, 8, "기본 공격 특화: 높은 HP로 강한 피해를 노립니다.", new Color(0.8f, 0.48f, 0.25f, 1f), false),
        new("ice_cream", "아이스크림", BattleCardType.Healer, 4, "패시브: 턴 시작 시 자신을 제외한 아군 HP를 1 회복합니다.", new Color(0.95f, 0.75f, 1f, 1f), false),
        new("cola_sniper", "콜라 저격수", BattleCardType.Ranged, 5, "스킬: 반격 없이 현재 HP만큼 피해를 줍니다.", new Color(0.25f, 0.55f, 1f, 1f), false)
    };

    public static CardCatalogEntry[] Cards => cards; /* 전체 카드 기획 목록 */

    /* 저장된 ID로 카드 기획 데이터를 찾습니다. */
    public static CardCatalogEntry Get(string id)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].Id == id)
                return cards[i];
        }

        return cards[0];
    }

    /* 도감 데이터를 전투에서 쓰는 런타임 카드 데이터로 변환합니다. */
    public static BattleCardData ToBattleData(CardCatalogEntry entry, int level)
    {
        int bonusHp = Mathf.Max(0, level - 1); /* 성장 레벨에 따른 HP 보너스 */
        string ability = $"Lv.{level} / {entry.AbilityText}";
        return new BattleCardData(entry.CardName, entry.CardType, entry.MaxHp + bonusHp, entry.CardSprite, ability, entry.CardColor);
    }
}

[Serializable]
public class CardCatalogEntry
{
    public string Id { get; } /* 저장과 덱 편집에 사용하는 카드 ID */
    public string CardName { get; } /* 화면에 표시할 카드 이름 */
    public BattleCardType CardType { get; } /* 전투에서 적용할 카드 타입 */
    public int MaxHp { get; } /* 카드 기본 최대 체력 */
    public string AbilityText { get; } /* 카드 효과 설명 */
    public Color CardColor { get; } /* 임시 일러스트 색상 */
    public bool IsStarter { get; } /* 최초 지급 카드 여부 */

    /* 도감과 저장 시스템에서 공유하는 고정 카드 기획 데이터를 만듭니다. */
    public Sprite CardSprite => CardCatalogSpriteLoader.Load(Id); /* 도감/덱 UI에 표시할 카드 이미지 */

    public CardCatalogEntry(string id, string cardName, BattleCardType cardType, int maxHp, string abilityText, Color cardColor, bool isStarter)
    {
        Id = id;
        CardName = cardName;
        CardType = cardType;
        MaxHp = maxHp;
        AbilityText = abilityText;
        CardColor = cardColor;
        IsStarter = isStarter;
    }
}

public static class CardCatalogSpriteLoader
{
    /* 카드 ID에 맞는 이미지 파일명을 반환합니다. */
    private static string GetSpriteName(string id)
    {
        return id switch
        {
            "burger" => "\uBC84\uAC70",
            "fries" => "\uAC10\uC790\uD280\uAE40",
            "monster" => "\uBAAC\uC2A4\uD130",
            "soda" => "\uC18C\uB2E4",
            "bomb_sauce" => "\uD3ED\uD0C4\uC18C\uC2A4",
            "vampire_shake" => "\uD761\uD608 \uC250\uC774\uD06C",
            "spicy_berserker" => "\uB9E4\uC6B4\uC18C\uC2A4",
            "guard_burger" => "\uC218\uD638\uBC84\uAC70",
            "skewer" => "\uAF2C\uCE58",
            "double_patty" => "\uCE58\uC988",
            "ice_cream" => "\uC591\uD30C",
            "cola_sniper" => "\uC218\uD638\uBC84\uAC70",
            _ => id
        };
    }

    /* 에디터에서는 4.Sprite 원본을 읽고, 빌드에서는 Resources/CardImages 폴더를 우선 사용합니다. */
    public static Sprite Load(string id)
    {
        string spriteName = GetSpriteName(id);
        Sprite sprite = Resources.Load<Sprite>($"CardImages/{spriteName}");
        if (sprite != null)
            return sprite;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/4.Sprite/{spriteName}.png");
#else
        return null;
#endif
    }
}
