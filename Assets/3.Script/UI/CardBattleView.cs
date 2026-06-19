using UnityEngine;
using UnityEngine.UI;

public class CardBattleView : MonoBehaviour
{
    [SerializeField] private Text turnText; /* 현재 턴 표시 텍스트 */
    [SerializeField] private Text infoText; /* 전투 안내와 결과 메시지 */
    [SerializeField] private Text playerDeckText; /* 플레이어 대기 카드 수 */
    [SerializeField] private Text enemyDeckText; /* 상대 대기 카드 수 */
    [SerializeField] private Button attackButton; /* 기본공격 버튼 */
    [SerializeField] private Button skillButton; /* 카드효과 버튼 */
    [SerializeField] private Button retryButton; /* 결과 화면 다시하기 버튼 */
    [SerializeField] private Button homeButton; /* 결과 화면 홈 버튼 */
    [SerializeField] private Button restartButton; /* 전투 중 옵션 메뉴 버튼 */
    [SerializeField] private Button playerDeckButton; /* 플레이어 대기 카드 설명 버튼 */
    [SerializeField] private Button enemyDeckButton; /* 상대 대기 카드 설명 버튼 */
    [SerializeField] private Button optionRetryButton; /* 옵션 팝업 다시하기 버튼 */
    [SerializeField] private Button optionLobbyButton; /* 옵션 팝업 도감/로비 버튼 */
    [SerializeField] private Button optionCloseButton; /* 옵션 팝업 닫기 버튼 */
    [SerializeField] private GameObject resultPanel; /* 승패 결과 오버레이 */
    [SerializeField] private Text resultText; /* 승패 결과 텍스트 */
    [SerializeField] private GameObject deckInfoPanel; /* 대기 카드 설명 팝업 */
    [SerializeField] private Text deckInfoText; /* 대기 카드 설명 텍스트 */
    [SerializeField] private Button deckInfoCloseButton; /* 대기 카드 설명 닫기 버튼 */
    [SerializeField] private GameObject optionPanel; /* 전투 중 옵션 팝업 */
    [SerializeField] private Image impactFlashImage; /* 공격 순간 플래시 이미지 */
    [SerializeField] private Text impactText; /* 공격 순간 강조 텍스트 */
    [SerializeField] private CardSlotView[] playerSlots; /* 플레이어 전장 슬롯 */
    [SerializeField] private CardSlotView[] enemySlots; /* 상대 전장 슬롯 */

    public Button AttackButton => attackButton;
    public Button SkillButton => skillButton;
    public Button RetryButton => retryButton;
    public Button HomeButton => homeButton;
    public Button RestartButton => restartButton;
    public Button PlayerDeckButton => playerDeckButton;
    public Button EnemyDeckButton => enemyDeckButton;
    public Button OptionRetryButton => optionRetryButton;
    public Button OptionLobbyButton => optionLobbyButton;
    public Button OptionCloseButton => optionCloseButton;
    public Button DeckInfoCloseButton => deckInfoCloseButton;
    public CardSlotView[] PlayerSlots => playerSlots;
    public CardSlotView[] EnemySlots => enemySlots;

    /* 전투 진행에 필요한 UI 참조가 모두 연결됐는지 확인합니다. */
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
            && playerDeckButton != null
            && enemyDeckButton != null
            && optionRetryButton != null
            && optionLobbyButton != null
            && optionCloseButton != null
            && resultPanel != null
            && resultText != null
            && deckInfoPanel != null
            && deckInfoText != null
            && deckInfoCloseButton != null
            && optionPanel != null
            && impactFlashImage != null
            && impactText != null
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

    /* 현재 턴 표시를 갱신합니다. */
    public void SetTurn(BattleTurn turn)
    {
        if (turnText != null)
            turnText.text = turn == BattleTurn.Player ? "PLAYER TURN" : "ENEMY TURN";
    }

    /* 턴 표시 아래의 안내 메시지를 갱신합니다. */
    public void SetInfo(string message)
    {
        if (infoText != null)
            infoText.text = message;
    }

    /* 지정 진영의 대기 카드 수를 갱신합니다. */
    public void SetDeckCount(CardOwner owner, int count)
    {
        Text target = owner == CardOwner.Player ? playerDeckText : enemyDeckText;
        if (target != null)
            target.text = $"\uB300\uAE30\n{count}\uC7A5";
    }

    /* 행동 버튼 표시와 카드효과 가능 여부를 반영합니다. */
    public void SetActionButtons(bool visible, BattleCardRuntime selectedCard = null)
    {
        if (attackButton != null)
        {
            attackButton.gameObject.SetActive(visible);
            SetButtonLabel(attackButton, "\uAE30\uBCF8\uACF5\uACA9");
        }

        if (skillButton != null)
        {
            skillButton.gameObject.SetActive(visible);
            skillButton.interactable = visible && CardBattleRules.CanUseCardEffect(selectedCard);
            SetButtonLabel(skillButton, "\uCE74\uB4DC\uD6A8\uACFC");
        }
    }

    /* 최종 승패 오버레이를 표시하거나 숨깁니다. */
    public void SetResult(bool visible, bool isWin)
    {
        if (resultPanel != null)
            resultPanel.SetActive(visible);

        if (resultText != null)
            resultText.text = isWin ? "VICTORY" : "DEFEAT";
    }

    /* 전투 중 옵션 팝업을 엽니다. */
    public void ShowOptionPanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(true);
    }

    /* 전투 중 옵션 팝업을 닫습니다. */
    public void HideOptionPanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(false);
    }

    /* 뒤집힌 대기 카드의 역할을 팝업으로 설명합니다. */
    public void ShowDeckInfo(CardOwner owner, int remainCount)
    {
        if (deckInfoPanel != null)
            deckInfoPanel.SetActive(true);

        if (deckInfoText == null)
            return;

        string ownerName = owner == CardOwner.Player ? "\uD50C\uB808\uC774\uC5B4" : "\uC0C1\uB300";
        deckInfoText.text = $"{ownerName} \uB300\uAE30 \uCE74\uB4DC\n\n\uC544\uC9C1 \uC804\uC7A5\uC5D0 \uB098\uC624\uC9C0 \uC54A\uC740 \uB4A4\uC9D1\uD78C \uCE74\uB4DC\uC785\uB2C8\uB2E4.\n\uC804\uC7A5 \uCE74\uB4DC\uAC00 \uC81C\uAC70\uB418\uC5B4 \uBE48 \uC2AC\uB86F\uC774 \uC0DD\uAE30\uBA74 \uC5EC\uAE30\uC11C \uC790\uB3D9\uC73C\uB85C 1\uC7A5\uC774 \uBC30\uCE58\uB429\uB2C8\uB2E4.\n\n\uB0A8\uC740 \uB300\uAE30 \uCE74\uB4DC: {remainCount}\uC7A5";
    }

    /* 대기 카드 설명 팝업을 닫습니다. */
    public void HideDeckInfo()
    {
        if (deckInfoPanel != null)
            deckInfoPanel.SetActive(false);
    }

    /* 공격 순간 플래시와 강조 텍스트를 표시합니다. */
    // 전투를 나가지 않고 메뉴 안에서 카드 도감/도움말 내용을 보여줍니다.
    public void ShowInfoPopup(string body)
    {
        if (deckInfoPanel != null)
            deckInfoPanel.SetActive(true);

        if (deckInfoText != null)
            deckInfoText.text = body;
    }

    public void ShowImpact(string message, Color color)
    {
        if (impactFlashImage != null)
        {
            impactFlashImage.gameObject.SetActive(true);
            impactFlashImage.color = color;
        }

        if (impactText != null)
        {
            impactText.gameObject.SetActive(true);
            impactText.text = message;
            impactText.color = Color.white;
        }
    }

    /* 공격 강조 UI를 숨깁니다. */
    public void HideImpact()
    {
        if (impactFlashImage != null)
            impactFlashImage.gameObject.SetActive(false);

        if (impactText != null)
            impactText.gameObject.SetActive(false);
    }

    /* 버튼 자식 Text를 찾아 라벨을 갱신합니다. */
    private void SetButtonLabel(Button button, string label)
    {
        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
            text.text = label;
    }

    /* 런타임 UI 빌더가 만든 참조를 한 번에 연결합니다. */
    public void SetupReferences(Text turnText, Text infoText, Text playerDeckText, Text enemyDeckText, Button attackButton, Button skillButton, Button restartButton, Button retryButton, Button homeButton, Button playerDeckButton, Button enemyDeckButton, Button optionRetryButton, Button optionLobbyButton, Button optionCloseButton, GameObject resultPanel, Text resultText, GameObject deckInfoPanel, Text deckInfoText, Button deckInfoCloseButton, GameObject optionPanel, Image impactFlashImage, Text impactText, CardSlotView[] playerSlots, CardSlotView[] enemySlots)
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
        this.playerDeckButton = playerDeckButton;
        this.enemyDeckButton = enemyDeckButton;
        this.optionRetryButton = optionRetryButton;
        this.optionLobbyButton = optionLobbyButton;
        this.optionCloseButton = optionCloseButton;
        this.resultPanel = resultPanel;
        this.resultText = resultText;
        this.deckInfoPanel = deckInfoPanel;
        this.deckInfoText = deckInfoText;
        this.deckInfoCloseButton = deckInfoCloseButton;
        this.optionPanel = optionPanel;
        this.impactFlashImage = impactFlashImage;
        this.impactText = impactText;
        this.playerSlots = playerSlots;
        this.enemySlots = enemySlots;
    }
}
