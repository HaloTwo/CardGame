using UnityEngine;

[CreateAssetMenu(menuName = "Card Battle/Card Definition", fileName = "CardDefinition")]
public class BattleCardDefinition : ScriptableObject
{
    [SerializeField] private string cardName = "Card"; // 카드 표시 이름
    [SerializeField] private BattleCardType cardType = BattleCardType.Normal; // 카드 전투 타입
    [SerializeField] private int maxHp = 5; // 카드 최대 체력
    [SerializeField] private Sprite cardSprite; // 교체 가능한 카드 일러스트
    [TextArea]
    [SerializeField] private string abilityText = "능력 설명"; // 카드 능력 설명
    [SerializeField] private Color cardColor = Color.white; // 이미지가 없을 때 보이는 임시 색상

    public string CardName => cardName; // UI에 표시할 이름
    public BattleCardType CardType => cardType; // 룰 처리에 사용할 타입
    public int MaxHp => maxHp; // 전투 시작 HP
    public Sprite CardSprite => cardSprite; // UI 카드 이미지
    public string AbilityText => abilityText; // UI 능력 설명
    public Color CardColor => cardColor; // 임시 카드 색상

    // ScriptableObject 값을 전투 중 사용하는 런타임 데이터로 변환한다.
    public BattleCardData ToData()
    {
        return new BattleCardData(cardName, cardType, maxHp, cardSprite, abilityText, cardColor);
    }
}
