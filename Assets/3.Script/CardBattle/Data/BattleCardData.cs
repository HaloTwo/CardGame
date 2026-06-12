using System;
using UnityEngine;

[Serializable]
public class BattleCardData
{
    [SerializeField] private string cardName; // 카드 표시 이름
    [SerializeField] private BattleCardType cardType; // 카드 전투 타입
    [SerializeField] private int maxHp; // 카드 최대 체력
    [SerializeField] private Sprite cardSprite; // 카드 일러스트 이미지
    [SerializeField] private string abilityText; // 카드 능력 설명
    [SerializeField] private Color cardColor = Color.white; // 이미지가 없을 때 사용할 임시 카드 색상

    public string CardName => cardName; // UI에 표시할 이름
    public BattleCardType CardType => cardType; // 룰 처리에 사용할 타입
    public int MaxHp => maxHp; // 전투 시작 체력
    public Sprite CardSprite => cardSprite; // UI에 표시할 카드 이미지
    public string AbilityText => abilityText; // UI에 표시할 능력 설명
    public Color CardColor => cardColor; // 카드 임시 배경 색상

    // 프로토타입 카드의 고정 데이터를 생성한다.
    public BattleCardData(string cardName, BattleCardType cardType, int maxHp, Sprite cardSprite, string abilityText, Color cardColor)
    {
        this.cardName = cardName;
        this.cardType = cardType;
        this.maxHp = Mathf.Max(1, maxHp);
        this.cardSprite = cardSprite;
        this.abilityText = abilityText;
        this.cardColor = cardColor;
    }
}
