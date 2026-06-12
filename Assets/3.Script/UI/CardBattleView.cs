using UnityEngine;
using UnityEngine.UI;

public class CardBattleView : MonoBehaviour
{
    [SerializeField] private Text turnText; // 현재 턴 표시 텍스트
    [SerializeField] private Text infoText; // 플레이어 행동 안내 텍스트
    [SerializeField] private Text playerDeckText; // 플레이어 대기 카드 수 텍스트
    [SerializeField] private Text enemyDeckText; // 적 대기 카드 수 텍스트
    [SerializeField] private Button attackButton; // 공격 행동 선택 버튼
    [SerializeField] private Button skillButton; // 스킬 행동 선택 버튼
    [SerializeField] private Button retryButton; // 결과 화면 다시하기 버튼
    [SerializeField] private Button homeButton; // 결과 화면 홈으로 버튼
    [SerializeField] private Button restartButton; // 전투 재시작 버튼
    [SerializeField] private GameObject resultPanel; // 승리/패배 결과 패널
    [SerializeField] private Text resultText; // 승리/패배 결과 텍스트
    [SerializeField] private CardSlotView[] playerSlots; // 플레이어 전장 슬롯 배열
    [SerializeField] private CardSlotView[] enemySlots; // 적 전장 슬롯 배열

    public Button AttackButton => attackButton; // 매니저에서 이벤트를 연결할 공격 버튼
    public Button SkillButton => skillButton; // 매니저에서 이벤트를 연결할 스킬 버튼
    public Button RetryButton => retryButton; // 매니저에서 이벤트를 연결할 다시하기 버튼
    public Button HomeButton => homeButton; // 매니저에서 이벤트를 연결할 홈 버튼
    public Button RestartButton => restartButton; // 매니저에서 이벤트를 연결할 재시작 버튼
    public CardSlotView[] PlayerSlots => playerSlots; // 플레이어 카드 슬롯 참조
    public CardSlotView[] EnemySlots => enemySlots; // 적 카드 슬롯 참조

    // 전투가 진행될 수 있을 만큼 핵심 UI 참조가 연결되어 있는지 확인한다.
    public bool IsReady()
    {
        bool hasBaseReferences = turnText != null
            && infoText != null
            && playerDeckText != null
            && enemyDeckText != null
            && attackButton != null
            && skillButton != null
            && retryButton != null
            && homeButton != null
            && restartButton != null
            && resultPanel != null
            && resultText != null
            && playerSlots != null
            && enemySlots != null
            && playerSlots.Length >= 3
            && enemySlots.Length >= 3;

        if (!hasBaseReferences)
            return false;

        for (int i = 0; i < 3; i++)
        {
            if (playerSlots[i] == null || enemySlots[i] == null)
                return false;
        }

        return true;
    }

    // 현재 턴 소유자를 화면 중앙에 표시한다.
    public void SetTurn(BattleTurn turn)
    {
        if (turnText != null)
            turnText.text = turn == BattleTurn.Player ? "PLAYER TURN" : "ENEMY TURN";
    }

    // 선택 안내, 승패 결과 같은 상태 메시지를 표시한다.
    public void SetInfo(string message)
    {
        if (infoText != null)
            infoText.text = message;
    }

    // 지정한 진영의 대기 카드 수를 갱신한다.
    public void SetDeckCount(CardOwner owner, int count)
    {
        Text target = owner == CardOwner.Player ? playerDeckText : enemyDeckText; // 갱신할 덱 카운트 텍스트
        if (target != null)
            target.text = $"대기 카드\n{count}장";
    }

    // 행동 선택 단계에서만 기본공격/카드효과 버튼을 보여주고, 카드효과 가능 여부를 반영한다.
    public void SetActionButtons(bool visible, BattleCardRuntime selectedCard = null)
    {
        if (attackButton != null)
        {
            attackButton.gameObject.SetActive(visible);
            SetButtonLabel(attackButton, "기본공격");
        }

        if (skillButton != null)
        {
            skillButton.gameObject.SetActive(visible);
            skillButton.interactable = visible && CardBattleRules.CanUseCardEffect(selectedCard);
            SetButtonLabel(skillButton, "카드효과");
        }
    }

    // 승리 또는 패배 결과 화면을 표시하거나 숨긴다.
    public void SetResult(bool visible, bool isWin)
    {
        if (resultPanel != null)
            resultPanel.SetActive(visible);

        if (resultText != null)
            resultText.text = isWin ? "VICTORY" : "DEFEAT";
    }

    // 버튼 자식 Text를 찾아 라벨을 갱신한다.
    private void SetButtonLabel(Button button, string label)
    {
        Text text = button.GetComponentInChildren<Text>(true); // 버튼 안에 있는 라벨 텍스트
        if (text != null)
            text.text = label;
    }

    // 에디터 씬 빌더가 만든 UI 컴포넌트 참조를 한 번에 연결한다.
    // 에디터 씬 빌더가 만든 UI 컴포넌트 참조를 한 번에 연결한다.
    // 에디터 씬 빌더가 만든 UI 컴포넌트 참조를 한 번에 연결한다.
    public void SetupReferences(Text turnText, Text infoText, Text playerDeckText, Text enemyDeckText, Button attackButton, Button skillButton, Button restartButton, Button retryButton, Button homeButton, GameObject resultPanel, Text resultText, CardSlotView[] playerSlots, CardSlotView[] enemySlots)
    {
        this.turnText = turnText;
        this.infoText = infoText;
        this.playerDeckText = playerDeckText;
        this.enemyDeckText = enemyDeckText;
        this.attackButton = attackButton;
        this.skillButton = skillButton;
        this.restartButton = restartButton;
        this.retryButton = retryButton;
        this.homeButton = homeButton;
        this.resultPanel = resultPanel;
        this.resultText = resultText;
        this.playerSlots = playerSlots;
        this.enemySlots = enemySlots;
    }
}
