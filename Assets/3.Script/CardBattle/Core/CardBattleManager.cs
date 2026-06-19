using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardBattleManager : Singleton<CardBattleManager>
{
    [SerializeField] private CardBattleView view; // 전투 화면 UI 참조
    [SerializeField] private float enemyStepDelay = 1f; // 적 턴 단계별 대기 시간
    [SerializeField] private BattleCardDefinition[] playerDeckDefinitions; // 플레이어 덱 카드 정의 목록
    [SerializeField] private BattleCardDefinition[] enemyDeckDefinitions; // 적 덱 카드 정의 목록

    private readonly BattleField playerField = new(CardOwner.Player); // 플레이어 덱과 전장 관리
    private readonly BattleField enemyField = new(CardOwner.Enemy); // 적 덱과 전장 관리
    private readonly EnemyBattleAI enemyAI = new(); // 적 턴 대상 선택 로직

    private BattleCardRuntime selectedActor; // 현재 선택한 아군 카드
    private BattleActionType selectedAction; // 현재 선택한 공격 또는 스킬 행동
    private BattleTurn currentTurn; // 현재 진행 중인 턴 소유자
    private BattleState state; // 현재 입력 단계와 전투 상태
    private bool battleRewarded; // 전투 결과 보상을 이미 저장했는지 여부

    // 씬 시작 시 UI 이벤트를 연결하고 새 전투를 시작한다.
    // 씬 시작 시 UI 이벤트를 연결하고 타이틀 시작 화면을 표시한다.
    // 씬 시작 시 UI 이벤트를 연결하고 전투를 시작한다.
    private void Start()
    {
        BindViewEvents();
        StartBattle();
    }

    // 오브젝트 제거 시 버튼 이벤트를 해제해 씬 재시작 후 중복 호출을 막는다.
    protected override void OnDestroy()
    {
        if (view != null)
        {
            if (view.AttackButton != null)
                view.AttackButton.onClick.RemoveListener(SelectAttack);

            if (view.SkillButton != null)
                view.SkillButton.onClick.RemoveListener(SelectSkill);


            if (view.RetryButton != null)
                view.RetryButton.onClick.RemoveListener(StartBattle);

            if (view.HomeButton != null)
                view.HomeButton.onClick.RemoveListener(LoadStartScene);

            if (view.PlayerDeckButton != null)
                view.PlayerDeckButton.onClick.RemoveListener(ShowPlayerDeckInfo);

            if (view.EnemyDeckButton != null)
                view.EnemyDeckButton.onClick.RemoveListener(ShowEnemyDeckInfo);

            if (view.DeckInfoCloseButton != null)
                view.DeckInfoCloseButton.onClick.RemoveListener(HideDeckInfo);

            if (view.RestartButton != null)
                view.RestartButton.onClick.RemoveListener(ShowOptionPanel);

            if (view.OptionRetryButton != null)
                view.OptionRetryButton.onClick.RemoveListener(StartBattleFromOption);

            if (view.OptionLobbyButton != null)
                view.OptionLobbyButton.onClick.RemoveListener(LoadStartScene);

            if (view.OptionCloseButton != null)
                view.OptionCloseButton.onClick.RemoveListener(HideOptionPanel);
        }

        base.OnDestroy();
    }

    // 전투를 처음 상태로 되돌린다. 재시작 버튼도 같은 진입점을 사용한다.
    public void StartBattle()
    {
        StopAllCoroutines();

        if (view == null || !view.IsReady())
            BindViewEvents();

        if (view == null || !view.IsReady())
            return;

        view.SetResult(false, false);
        view.HideDeckInfo();
        view.HideImpact();
        view.HideOptionPanel();
        battleRewarded = false;
        playerField.Reset(BattleDeckFactory.CreateDeck(playerDeckDefinitions, CardOwner.Player));
        enemyField.Reset(BattleDeckFactory.CreateDeck(enemyDeckDefinitions, CardOwner.Enemy));
        RefreshAllSlots();

        currentTurn = BattleTurn.Player;
        BeginTurn(BattleTurn.Player);
    }

    // UI 버튼과 카드 슬롯 클릭 이벤트를 전투 흐름 함수에 연결한다.
    private void BindViewEvents()
    {
        if (view == null)
            view = FindFirstObjectByType<CardBattleView>();

        if (view == null || !view.IsReady())
            view = CardBattleRuntimeViewBuilder.CreateOrRepair();

        if (view == null || !view.IsReady())
        {
            Debug.LogError("CardBattleView를 만들지 못했습니다. Canvas와 Input System 패키지 상태를 확인하세요.");
            return;
        }

        foreach (CardSlotView slot in view.PlayerSlots)
            slot.Bind(null, HandlePlayerSlotClicked);

        foreach (CardSlotView slot in view.EnemySlots)
            slot.Bind(null, HandleEnemySlotClicked);

        view.AttackButton.onClick.RemoveListener(SelectAttack);
        view.SkillButton.onClick.RemoveListener(SelectSkill);
        view.RetryButton.onClick.RemoveListener(StartBattle);
        view.HomeButton.onClick.RemoveListener(LoadStartScene);
        view.PlayerDeckButton.onClick.RemoveListener(ShowPlayerDeckInfo);
        view.EnemyDeckButton.onClick.RemoveListener(ShowEnemyDeckInfo);
        view.DeckInfoCloseButton.onClick.RemoveListener(HideDeckInfo);
        view.RestartButton.onClick.RemoveListener(ShowOptionPanel);
        view.OptionRetryButton.onClick.RemoveListener(StartBattleFromOption);
        view.OptionLobbyButton.onClick.RemoveListener(LoadStartScene);
        view.OptionCloseButton.onClick.RemoveListener(HideOptionPanel);

        view.AttackButton.onClick.AddListener(SelectAttack);
        view.SkillButton.onClick.AddListener(SelectSkill);
        view.RetryButton.onClick.AddListener(StartBattle);
        view.HomeButton.onClick.AddListener(LoadStartScene);
        view.PlayerDeckButton.onClick.AddListener(ShowPlayerDeckInfo);
        view.EnemyDeckButton.onClick.AddListener(ShowEnemyDeckInfo);
        view.DeckInfoCloseButton.onClick.AddListener(HideDeckInfo);
        view.RestartButton.onClick.AddListener(ShowOptionPanel);
        view.OptionRetryButton.onClick.AddListener(StartBattleFromOption);
        view.OptionLobbyButton.onClick.AddListener(LoadStartScene);
        view.OptionCloseButton.onClick.AddListener(HideOptionPanel);

        ApplyBattleMenuLabels();
    }

    // 에디터 빌더에서 생성한 카드 정의 배열을 매니저에 연결한다.
    public void SetupDeckDefinitions(BattleCardDefinition[] playerDefinitions, BattleCardDefinition[] enemyDefinitions)
    {
        playerDeckDefinitions = playerDefinitions;
        enemyDeckDefinitions = enemyDefinitions;
    }

    // 턴 시작 효과를 처리하고 플레이어 입력 또는 적 AI 행동으로 분기한다.
    private void BeginTurn(BattleTurn turn)
    {
        currentTurn = turn;
        selectedActor = null;
        view.SetTurn(currentTurn);
        view.SetActionButtons(false);

        if (turn == BattleTurn.Player)
        {
            CardBattleRules.ApplyHealerTurnStart(playerField.FieldCards);
            RefreshAllSlots();
            state = BattleState.PlayerSelectCard;
            view.SetInfo("사용할 아군 카드를 선택하세요.");
            return;
        }

        CardBattleRules.ApplyHealerTurnStart(enemyField.FieldCards);
        RefreshAllSlots();
        state = BattleState.EnemyActing;
        view.SetInfo("상대가 행동 중입니다.");
        StartCoroutine(CoEnemyTurn());
    }

    // 플레이어 전장 슬롯 클릭 시 행동할 카드를 선택한다.
    private void HandlePlayerSlotClicked(CardSlotView slot)
    {
        if (slot.BoundCard == null || slot.BoundCard.IsDead)
            return;

        if (state == BattleState.PlayerSelectCard)
        {
            SelectActor(slot);
            return;
        }

        if (state == BattleState.PlayerSelectAction || state == BattleState.PlayerSelectTarget)
        {
            if (selectedActor == slot.BoundCard)
                ClearActorSelection();
            else
                SelectActor(slot);
        }
    }

    // 적 전장 슬롯 클릭 시 선택된 행동을 해당 카드에게 적용한다.
    private void HandleEnemySlotClicked(CardSlotView slot)
    {
        if (state != BattleState.PlayerSelectTarget || selectedActor == null || slot.BoundCard == null || slot.BoundCard.IsDead)
            return;

        StartCoroutine(CoPlayerAction(selectedActor, slot.BoundCard, selectedAction));
    }

    // 공격 버튼 입력을 대상 선택 단계로 넘긴다.
    private void SelectAttack()
    {
        SelectAction(BattleActionType.Attack, "기본공격 대상을 선택하세요. 피해 후 반격을 받습니다.");
    }

    // 스킬 버튼 입력을 대상 선택 단계로 넘긴다.
    private void SelectSkill()
    {
        if (!CardBattleRules.CanUseCardEffect(selectedActor))
        {
            view.SetInfo("이 카드는 액티브 카드효과가 없습니다.");
            return;
        }

        SelectAction(BattleActionType.Skill, "카드효과 대상을 선택하세요.");
    }

    // 행동할 아군 카드를 선택하고 행동 버튼을 표시한다.
    private void SelectActor(CardSlotView slot)
    {
        selectedActor = slot.BoundCard;
        state = BattleState.PlayerSelectAction;
        view.SetActionButtons(true, selectedActor);
        view.SetInfo($"{selectedActor.Data.CardName}: 기본공격 또는 카드효과를 선택하세요.");
        RefreshSelection(slot);
    }

    // 현재 선택한 아군 카드를 해제하고 다시 카드 선택 단계로 되돌린다.
    private void ClearActorSelection()
    {
        selectedActor = null;
        state = BattleState.PlayerSelectCard;
        view.SetActionButtons(false);
        view.SetInfo("사용할 아군 카드를 선택하세요.");
        RefreshSelection(null);
    }

    // 공격 또는 스킬을 저장하고 다음 클릭을 대상 선택으로 해석하게 한다.
    private void SelectAction(BattleActionType actionType, string message)
    {
        if (state != BattleState.PlayerSelectAction || selectedActor == null)
            return;

        selectedAction = actionType;
        state = BattleState.PlayerSelectTarget;
        view.SetActionButtons(false);
        view.SetInfo(message);
    }

    // 선택된 카드 행동을 처리한 뒤 사망, 자동 배치, 승패, 턴 전환을 순서대로 처리한다.
    private void ExecuteAction(BattleCardRuntime actor, BattleCardRuntime target, BattleActionType actionType)
    {
        BattleField targetField = target.Owner == CardOwner.Player ? playerField : enemyField; // 대상 카드가 속한 전장

        CardBattleRules.ApplyAction(actor, target, targetField.FieldCards, actionType);
        ResolveDeadCards();
        RefreshAllSlots();

        if (TryFinishBattle())
            return;

        BeginTurn(currentTurn == BattleTurn.Player ? BattleTurn.Enemy : BattleTurn.Player);
    }

    // 플레이어 행동을 설명 문구와 짧은 카드 연출 후 처리한다.
    private IEnumerator CoPlayerAction(BattleCardRuntime actor, BattleCardRuntime target, BattleActionType actionType)
    {
        state = BattleState.EnemyActing;
        view.SetActionButtons(false);
        string actionName = actionType == BattleActionType.Skill ? "카드효과" : "기본공격"; // 화면에 표시할 행동 이름
        view.SetInfo($"{actor.Data.CardName} {actionName} 준비...");
        GetSlotByCard(actor)?.PlayFocus();
        yield return new WaitForSeconds(0.45f);

        view.SetInfo($"{target.Data.CardName}에게 {actionName}을 사용합니다.");
        GetSlotByCard(target)?.PlayHit();
        StartCoroutine(CoImpact(actionType == BattleActionType.Skill ? "SKILL HIT!" : "ATTACK!"));
        ShowDamagePreview(actor, target, actionType);
        yield return new WaitForSeconds(0.45f);

        ExecuteAction(actor, target, actionType);
    }

    // 적 턴에서 짧은 대기 후 AI가 고른 카드와 대상에게 공격을 실행한다.
    private IEnumerator CoEnemyTurn()
    {
        view.SetInfo("상대 턴입니다.");
        yield return new WaitForSeconds(enemyStepDelay);

        view.SetInfo("상대가 사용할 카드를 고릅니다...");
        yield return new WaitForSeconds(enemyStepDelay);

        if (enemyAI.TryPickAction(enemyField.FieldCards, playerField.FieldCards, out BattleCardRuntime actor, out BattleCardRuntime target))
        {
            BattleActionType enemyAction = CardBattleRules.CanUseCardEffect(actor) ? BattleActionType.Skill : BattleActionType.Attack; // 적이 사용할 행동
            string actionName = enemyAction == BattleActionType.Skill ? "카드효과" : "기본공격"; // UI에 표시할 행동 이름

            GetSlotByCard(actor)?.PlayFocus();
            view.SetInfo($"{actor.Data.CardName} 선택: {actionName}");
            yield return new WaitForSeconds(enemyStepDelay);

            GetSlotByCard(target)?.PlayHit();
            view.SetInfo($"{target.Data.CardName}에게 {actionName}을 사용합니다.");
            StartCoroutine(CoImpact(enemyAction == BattleActionType.Skill ? "ENEMY SKILL!" : "ENEMY ATTACK!"));
            ShowDamagePreview(actor, target, enemyAction);
            yield return new WaitForSeconds(enemyStepDelay);

            ApplyActionAndRefresh(actor, target, enemyAction);

            if (TryFinishBattle())
                yield break;

            view.SetInfo("당신의 턴입니다.");
            yield return new WaitForSeconds(enemyStepDelay);
            BeginTurn(BattleTurn.Player);
        }
        else
        {
            TryFinishBattle();
        }
    }

    // 행동 효과를 적용하고 사망 카드 처리와 UI 갱신까지만 수행한다.
    private void ApplyActionAndRefresh(BattleCardRuntime actor, BattleCardRuntime target, BattleActionType actionType)
    {
        BattleField targetField = target.Owner == CardOwner.Player ? playerField : enemyField; // 대상 카드가 속한 전장
        CardBattleRules.ApplyAction(actor, target, targetField.FieldCards, actionType);
        ResolveDeadCards();
        RefreshAllSlots();
    }

    // 양쪽 전장에서 사망 카드를 제거하고 빈 슬롯에 대기 카드를 자동 배치한다.
    private void ResolveDeadCards()
    {
        playerField.ResolveDeadAndFill();
        enemyField.ResolveDeadAndFill();
    }

    // 한쪽의 전장과 대기 카드가 모두 비었는지 확인해 승리 또는 패배를 확정한다.
    private bool TryFinishBattle()
    {
        bool playerAlive = playerField.HasAnyRemainCard(); // 플레이어 생존 카드 여부
        bool enemyAlive = enemyField.HasAnyRemainCard(); // 적 생존 카드 여부

        if (playerAlive && enemyAlive)
            return false;

        state = BattleState.GameOver;
        view.SetActionButtons(false);
        bool isWin = !enemyAlive; // 적 카드가 모두 제거되면 승리
        string rewardMessage = string.Empty; // 결과 화면에 표시할 획득/성장 보상

        if (!battleRewarded)
        {
            rewardMessage = CardPlayerProfile.RecordBattleResult(isWin);
            battleRewarded = true;
        }

        view.SetInfo(isWin ? $"승리했습니다. {rewardMessage}" : $"패배했습니다. {rewardMessage}");
        view.SetResult(true, isWin);
        return true;
    }

    // 결과 화면의 홈 버튼에서 시작 씬으로 돌아간다.
    private void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }

    // 전투 메뉴의 도감 버튼에서 로비로 이동한 뒤 카드 도감을 바로 열도록 요청합니다.
    private void LoadCollectionScene()
    {
        view.HideOptionPanel();
        view.ShowInfoPopup(BuildBattleCollectionText());
        view.SetInfo("전투 중 카드 도감을 확인합니다. 닫기를 누르면 전투로 돌아옵니다.");
    }

    // 플레이어 대기 카드 더미를 눌렀을 때 설명 팝업을 연다.
    // 전투 중 MENU > 도감에서 보여줄 카드 효과 요약을 만듭니다.
    private string BuildBattleCollectionText()
    {
        StringBuilder builder = new();
        builder.AppendLine("전투 카드 도감");
        builder.AppendLine();
        builder.Clear();

        builder.AppendLine("카드 종류 설명");
        builder.AppendLine("일반: 현재 HP만큼 피해를 주고 대상 HP만큼 반격 피해를 받습니다.");
        builder.AppendLine("원거리: 현재 HP만큼 피해를 주고 반격 피해를 받지 않습니다.");
        builder.AppendLine("무쌍: 대상에게 100%, 인접한 적 1장에게 50% 피해를 줍니다.");
        builder.AppendLine("힐러: 턴 시작 시 자신 제외 아군 HP를 1 회복합니다. 공격은 일반과 같습니다.");
        builder.AppendLine("폭탄: 대상에게 현재 HP 피해, 나머지 적에게 1 피해를 줍니다.");
        builder.AppendLine("흡혈: 현재 HP 피해를 주고 자신 HP를 2 회복합니다.");
        builder.AppendLine("광전사: 잃은 HP만큼 추가 피해를 주고 자신도 1 피해를 받습니다.");
        builder.AppendLine("수호자: 절반 피해를 주고 자신 HP를 1 회복합니다.");
        builder.AppendLine("관통: 대상 피해와 함께 양옆 적에게 1 피해를 줍니다.");

        return builder.ToString();
    }

    private void ShowPlayerDeckInfo()
    {
        view.ShowDeckInfo(CardOwner.Player, playerField.RemainDeckCount);
    }

    // 상대 대기 카드 더미를 눌렀을 때 설명 팝업을 연다.
    private void ShowEnemyDeckInfo()
    {
        view.ShowDeckInfo(CardOwner.Enemy, enemyField.RemainDeckCount);
    }

    // 대기 카드 설명 팝업을 닫는다.
    private void HideDeckInfo()
    {
        view.HideDeckInfo();
    }

    // 전투 중 MENU 버튼에서 옵션 팝업을 엽니다.
    private void ShowOptionPanel()
    {
        view.ShowOptionPanel();
    }

    // 옵션 팝업을 닫고 전투 화면으로 돌아갑니다.
    private void HideOptionPanel()
    {
        view.HideOptionPanel();
    }

    // 기존 씬에 남아 있는 도감 버튼도 전투 닫기 버튼으로 표시합니다.
    private void ApplyBattleMenuLabels()
    {
        SetButtonText(view.OptionRetryButton, "재시작");
        SetButtonText(view.OptionLobbyButton, "로비로");
        SetButtonText(view.OptionCloseButton, "닫기");
    }

    // 버튼 하위 Text 라벨을 찾아 교체합니다.
    private void SetButtonText(UnityEngine.UI.Button button, string label)
    {
        if (button == null)
            return;

        UnityEngine.UI.Text text = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (text != null)
            text.text = label;
    }

    // 옵션 팝업에서 다시하기를 눌렀을 때 팝업을 닫고 전투를 재시작합니다.
    private void StartBattleFromOption()
    {
        view.HideOptionPanel();
        StartBattle();
    }

    // 타격 순간 화면 플래시와 큰 임팩트 텍스트를 잠깐 표시한다.
    private IEnumerator CoImpact(string message)
    {
        view.ShowImpact(message, new Color(1f, 0.2f, 0.05f, 0.22f));
        yield return new WaitForSeconds(0.16f);
        view.HideImpact();
    }


    // 전장 카드와 대기 카드 수를 UI 슬롯에 다시 바인딩한다.
    private void RefreshAllSlots()
    {
        int playerCardCount = 0; // 플레이어 전장 카드 수
        int enemyCardCount = 0; // 적 전장 카드 수

        for (int i = 0; i < view.PlayerSlots.Length; i++)
        {
            if (playerField.GetSlotCard(i) != null)
                playerCardCount++;

            view.PlayerSlots[i].Bind(playerField.GetSlotCard(i), HandlePlayerSlotClicked);
        }

        for (int i = 0; i < view.EnemySlots.Length; i++)
        {
            if (enemyField.GetSlotCard(i) != null)
                enemyCardCount++;

            view.EnemySlots[i].Bind(enemyField.GetSlotCard(i), HandleEnemySlotClicked);
        }

        view.SetDeckCount(CardOwner.Player, playerField.RemainDeckCount);
        view.SetDeckCount(CardOwner.Enemy, enemyField.RemainDeckCount);
        Debug.Log($"CardBattleManager: 슬롯 갱신 완료 / PlayerCards={playerCardCount}, EnemyCards={enemyCardCount}");
    }

    // 현재 선택된 플레이어 카드만 강조 표시한다.
    private void RefreshSelection(CardSlotView selectedSlot)
    {
        foreach (CardSlotView slot in view.PlayerSlots)
            slot.SetSelected(slot == selectedSlot);
    }

    // 카드 런타임 객체와 연결된 슬롯 UI를 찾는다.
    private CardSlotView GetSlotByCard(BattleCardRuntime card)
    {
        if (card == null)
            return null;

        CardSlotView[] slots = card.Owner == CardOwner.Player ? view.PlayerSlots : view.EnemySlots; // 카드 소유자에 맞는 슬롯 배열
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].BoundCard == card)
                return slots[i];
        }

        return null;
    }

    // 실제 룰 적용 전에 예상 피해 숫자를 카드 위에 표시한다.
    private void ShowDamagePreview(BattleCardRuntime actor, BattleCardRuntime target, BattleActionType actionType)
    {
        if (actor == null || target == null)
            return;

        int targetDamage = GetPreviewDamage(actor, actionType); // 대상에게 줄 예상 피해
        GetSlotByCard(target)?.ShowDamageText(targetDamage);

        if (actionType == BattleActionType.Attack)
        {
            int counterDamage = target.CurrentHp; // 기본공격 반격 피해
            GetSlotByCard(actor)?.ShowDamageText(counterDamage);
            return;
        }

        if (actor.Data.CardType == BattleCardType.Bomber)
            ShowBomberSplashPreview(target);

        if (actor.Data.CardType == BattleCardType.Piercing)
            ShowPiercingSplashPreview(target);
    }

    // 액티브 효과별 실제 룰과 맞는 예상 피해량을 계산한다.
    private int GetPreviewDamage(BattleCardRuntime actor, BattleActionType actionType)
    {
        if (actionType == BattleActionType.Attack || actor == null)
            return actor != null ? actor.CurrentHp : 0;

        return actor.Data.CardType switch
        {
            BattleCardType.Berserker => actor.CurrentHp + (actor.Data.MaxHp - actor.CurrentHp),
            BattleCardType.Guardian => Mathf.Max(1, Mathf.CeilToInt(actor.CurrentHp * 0.5f)),
            _ => actor.CurrentHp
        };
    }

    // 폭탄 카드의 광역 1 피해를 대상 외 전장 카드에 미리 표시한다.
    private void ShowBomberSplashPreview(BattleCardRuntime mainTarget)
    {
        CardSlotView[] slots = mainTarget.Owner == CardOwner.Player ? view.PlayerSlots : view.EnemySlots; // 폭발 피해를 표시할 상대 전장
        for (int i = 0; i < slots.Length; i++)
        {
            BattleCardRuntime splashTarget = slots[i].BoundCard; // 폭발 여파 대상 후보
            if (splashTarget == null || splashTarget == mainTarget || splashTarget.IsDead)
                continue;

            slots[i].PlayHit();
            slots[i].ShowDamageText(1);
        }
    }

    // 관통 카드의 양옆 1 피해를 실제 인접 슬롯에 미리 표시한다.
    private void ShowPiercingSplashPreview(BattleCardRuntime mainTarget)
    {
        CardSlotView[] slots = mainTarget.Owner == CardOwner.Player ? view.PlayerSlots : view.EnemySlots; // 관통 피해를 표시할 상대 전장
        for (int i = 0; i < slots.Length; i++)
        {
            BattleCardRuntime adjacentTarget = slots[i].BoundCard; // 관통 여파 대상 후보
            if (adjacentTarget == null || adjacentTarget == mainTarget || adjacentTarget.IsDead)
                continue;

            if (Mathf.Abs(adjacentTarget.SlotIndex - mainTarget.SlotIndex) != 1)
                continue;

            slots[i].PlayHit();
            slots[i].ShowDamageText(1);
        }
    }
}
