using System.Collections;
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

            if (view.RestartButton != null)
                view.RestartButton.onClick.RemoveListener(StartBattle);
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
        playerField.Reset(BattleDeckFactory.CreateDeck(playerDeckDefinitions, CardOwner.Player));
        enemyField.Reset(BattleDeckFactory.CreateDeck(enemyDeckDefinitions, CardOwner.Enemy));
        RefreshAllSlots();

        currentTurn = BattleTurn.Player;
        BeginTurn(BattleTurn.Player);
    }

    // UI 버튼과 카드 슬롯 클릭 이벤트를 전투 흐름 함수에 연결한다.
    private void BindViewEvents()
    {
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
        view.RestartButton.onClick.RemoveListener(StartBattle);

        view.AttackButton.onClick.AddListener(SelectAttack);
        view.SkillButton.onClick.AddListener(SelectSkill);
        view.RetryButton.onClick.AddListener(StartBattle);
        view.HomeButton.onClick.AddListener(LoadStartScene);
        view.RestartButton.onClick.AddListener(StartBattle);
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
        view.SetInfo(enemyAlive ? "패배했습니다. 다시 도전하세요." : "승리했습니다.");
        view.SetResult(true, !enemyAlive);
        return true;
    }

    // 결과 화면의 홈 버튼에서 시작 씬으로 돌아간다.
    private void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
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
    // 실제 룰 적용 전에 예상 피해 숫자를 카드 위에 표시한다.
    private void ShowDamagePreview(BattleCardRuntime actor, BattleCardRuntime target, BattleActionType actionType)
    {
        if (actor == null || target == null)
            return;

        int targetDamage = actor.CurrentHp; // 대상에게 줄 기본 피해
        GetSlotByCard(target)?.ShowDamageText(targetDamage);

        if (actionType == BattleActionType.Attack)
        {
            int counterDamage = target.CurrentHp; // 기본공격 반격 피해
            GetSlotByCard(actor)?.ShowDamageText(counterDamage);
            return;
        }

        if (actor.Data.CardType == BattleCardType.Bomber)
            ShowBomberSplashPreview(target);
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
}
