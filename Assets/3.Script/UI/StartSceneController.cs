using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneController : MonoBehaviour
{
    private const string OpenPanelKey = "StartScene.OpenPanel"; /* 전투 메뉴에서 로비 특정 패널을 바로 열기 위한 키 */

    [SerializeField] private string battleSceneName = "SampleScene"; /* 전투 시작 버튼으로 이동할 전투 씬 이름 */

    private bool canStartBattle; /* 씬 진입 직후 중복 클릭을 막는 플래그 */
    private int selectedDeckSlotIndex; /* 덱 편집에서 현재 교체 대상으로 선택한 슬롯 */
    private int selectedCardIndex; /* 도감/성장/덱 편집에서 현재 선택한 카드 인덱스 */
    private LobbyPanelMode currentMode; /* 현재 로비 팝업 표시 모드 */
    private LobbyViewReferences lobbyView; /* 런타임에 생성된 로비 UI 참조 */

    /* 저장 데이터를 준비하고 로비 UI 버튼 이벤트를 연결합니다. */
    private Coroutine resetCatalogScrollCoroutine; /* 카드 목록을 열 때 첫 줄로 되돌리는 코루틴 */

    private void Start()
    {
        canStartBattle = false;
        selectedDeckSlotIndex = 0;
        selectedCardIndex = 0;
        currentMode = LobbyPanelMode.None;
        CardPlayerProfile.EnsureInitialized();
        lobbyView = StartSceneRuntimeViewBuilder.CreateStartView();
        BindLobbyEvents();
        RefreshMainInfo();
        OpenRequestedPanel();
        StartCoroutine(CoEnableStartButton());
    }

    /* 씬 전환 전에 버튼 이벤트를 정리해 중복 호출을 막습니다. */
    private void OnDestroy()
    {
        UnbindLobbyEvents();
    }

    /* 로비 버튼과 카드 버튼 이벤트를 한 번에 연결합니다. */
    private void BindLobbyEvents()
    {
        if (lobbyView == null)
            return;

        lobbyView.StartButton.interactable = false;
        lobbyView.StartButton.onClick.AddListener(LoadBattleScene);
        lobbyView.DeckButton.onClick.AddListener(ShowDeckPanel);
        lobbyView.CollectionButton.onClick.AddListener(ShowCollectionPanel);
        lobbyView.GrowthButton.onClick.AddListener(ShowGrowthPanel);
        lobbyView.ResetButton.onClick.AddListener(ResetProfile);
        lobbyView.BackButton.onClick.AddListener(HidePanel);
        lobbyView.TabDeckButton.onClick.AddListener(ShowDeckPanel);
        lobbyView.TabCollectionButton.onClick.AddListener(ShowCollectionPanel);
        lobbyView.TabGrowthButton.onClick.AddListener(ShowGrowthPanel);
        lobbyView.UpgradeButton.onClick.AddListener(UpgradeSelectedCard);

        for (int i = 0; i < lobbyView.DeckSlotButtons.Length; i++)
        {
            int slotIndex = i;
            lobbyView.DeckSlotButtons[i].onClick.AddListener(() => HandleDeckSlotClicked(slotIndex));
        }

        for (int i = 0; i < lobbyView.CardButtons.Length; i++)
        {
            int cardIndex = i;
            lobbyView.CardButtons[i].onClick.AddListener(() => HandleCatalogCardClicked(cardIndex));

            LobbyCardDragHandler dragHandler = lobbyView.CardButtons[i].GetComponent<LobbyCardDragHandler>();
            if (dragHandler == null)
                dragHandler = lobbyView.CardButtons[i].gameObject.AddComponent<LobbyCardDragHandler>();

            dragHandler.Setup(this, cardIndex);
        }
    }

    /* 로비 버튼과 카드 버튼 이벤트를 해제합니다. */
    private void UnbindLobbyEvents()
    {
        if (lobbyView == null)
            return;

        lobbyView.StartButton.onClick.RemoveListener(LoadBattleScene);
        lobbyView.DeckButton.onClick.RemoveListener(ShowDeckPanel);
        lobbyView.CollectionButton.onClick.RemoveListener(ShowCollectionPanel);
        lobbyView.GrowthButton.onClick.RemoveListener(ShowGrowthPanel);
        lobbyView.ResetButton.onClick.RemoveListener(ResetProfile);
        lobbyView.BackButton.onClick.RemoveListener(HidePanel);
        lobbyView.TabDeckButton.onClick.RemoveListener(ShowDeckPanel);
        lobbyView.TabCollectionButton.onClick.RemoveListener(ShowCollectionPanel);
        lobbyView.TabGrowthButton.onClick.RemoveListener(ShowGrowthPanel);
        lobbyView.UpgradeButton.onClick.RemoveListener(UpgradeSelectedCard);

        for (int i = 0; i < lobbyView.DeckSlotButtons.Length; i++)
            lobbyView.DeckSlotButtons[i].onClick.RemoveAllListeners();

        for (int i = 0; i < lobbyView.CardButtons.Length; i++)
            lobbyView.CardButtons[i].onClick.RemoveAllListeners();
    }

    /* 전투 시작 버튼 입력 시 전투 씬으로 이동합니다. */
    private void LoadBattleScene()
    {
        if (!canStartBattle)
            return;

        SceneManager.LoadScene(battleSceneName);
    }

    /* 보유 카드, 덱, 재화 정보를 로비 메인 영역에 표시합니다. */
    private void RefreshMainInfo()
    {
        if (lobbyView?.MainInfoText == null)
            return;

        int ownedCount = CardPlayerProfile.GetOwnedCards().Count;
        lobbyView.MainInfoText.text =
            $"보유 재화: {CardPlayerProfile.GetGold()}G\n" +
            $"보유 카드: {ownedCount}/{CardCatalog.Cards.Length}장  |  덱: {CardCatalog.DeckSize}장\n" +
            $"승리 시 +{CardPlayerProfile.WinGoldReward}G, 패배 시 보상 없음";
    }

    /* 다른 씬에서 로비로 돌아올 때 요청된 패널을 바로 엽니다. */
    private void OpenRequestedPanel()
    {
        string panelName = PlayerPrefs.GetString(OpenPanelKey, string.Empty);
        if (string.IsNullOrEmpty(panelName))
            return;

        PlayerPrefs.DeleteKey(OpenPanelKey);
        if (panelName == "Collection")
            ShowCollectionPanel();
    }

    /* 덱 편집 패널을 엽니다. */
    private void ShowDeckPanel()
    {
        currentMode = LobbyPanelMode.Deck;
        selectedDeckSlotIndex = Mathf.Clamp(selectedDeckSlotIndex, 0, CardCatalog.DeckSize - 1);
        SetPanelBase("덱 편집", "덱 슬롯을 선택한 뒤 아래 카드를 누르거나 슬롯 위로 드래그하면 교체됩니다. 같은 카드는 중복 장착할 수 없습니다.");
        SetDeckSlotsVisible(true);
        SetDetailVisible(false);
        SetUpgradeVisible(false);
        SetScrollAreaLayout(new Vector2(0f, -250f), new Vector2(900f, 500f));
        ApplyCatalogCardLayout();
        RefreshDeckSlots();
        RefreshCatalogGrid(true);
        ResetCatalogScrollToTop();
    }

    /* 카드 도감 패널을 엽니다. */
    private void ShowCollectionPanel()
    {
        currentMode = LobbyPanelMode.Collection;
        SetPanelBase("카드 도감", "카드를 누르면 큰 이미지, 이름, 타입, 설명을 확인합니다.");
        SetDeckSlotsVisible(false);
        SetDetailVisible(true);
        SetUpgradeVisible(false);
        SetScrollAreaLayout(new Vector2(0f, -250f), new Vector2(900f, 500f));
        ApplyCatalogCardLayout();
        RefreshCatalogGrid(false);
        SelectFirstVisibleCard();
        ResetCatalogScrollToTop();
    }

    /* 성장 패널을 엽니다. */
    private void ShowGrowthPanel()
    {
        currentMode = LobbyPanelMode.Growth;
        SetPanelBase("성장", $"보유 재화: {CardPlayerProfile.GetGold()}G  |  보유 카드를 선택하고 성장 버튼을 누르세요.");
        SetDeckSlotsVisible(false);
        SetDetailVisible(true);
        SetUpgradeVisible(true);
        SetScrollAreaLayout(new Vector2(0f, -250f), new Vector2(900f, 500f));
        ApplyCatalogCardLayout();
        RefreshCatalogGrid(true);
        SelectFirstVisibleCard();
        ResetCatalogScrollToTop();
    }

    /* 공용 패널 제목과 안내 문구를 설정합니다. */
    private void SetPanelBase(string title, string body)
    {
        lobbyView.Panel.SetActive(true);
        lobbyView.PanelTitleText.text = title;
        lobbyView.PanelBodyText.text = body;
        RefreshTabColors();
    }

    /* 공용 팝업을 닫습니다. */
    private void HidePanel()
    {
        if (lobbyView?.Panel == null)
            return;

        lobbyView.Panel.SetActive(false);
    }

    /* 덱 슬롯 카드 UI를 갱신합니다. */
    private void RefreshDeckSlots()
    {
        string[] deckIds = CardPlayerProfile.GetDeckIds();
        for (int i = 0; i < lobbyView.DeckSlotButtons.Length; i++)
        {
            CardCatalogEntry entry = CardCatalog.Get(deckIds[i]);
            int level = CardPlayerProfile.GetLevel(entry.Id);
            bool selected = i == selectedDeckSlotIndex;

            ApplyCardImage(lobbyView.DeckSlotImages[i], entry);
            lobbyView.DeckSlotTexts[i].text = $"{i + 1}. {entry.CardName}\n{GetTypeName(entry.CardType)}  Lv.{level}\nHP {entry.MaxHp + level - 1}";
            lobbyView.DeckSlotButtons[i].targetGraphic.color = selected ? new Color(0.95f, 0.52f, 0.12f, 1f) : new Color(0.12f, 0.08f, 0.055f, 1f);
        }
    }

    /* 도감/덱 편집/성장 카드 목록을 갱신합니다. */
    private void RefreshCatalogGrid(bool ownedOnly)
    {
        for (int i = 0; i < lobbyView.CardButtons.Length; i++)
        {
            bool hasCard = i < CardCatalog.Cards.Length;
            lobbyView.CardButtons[i].gameObject.SetActive(hasCard);
            if (!hasCard)
                continue;

            CardCatalogEntry entry = CardCatalog.Cards[i];
            bool owned = CardPlayerProfile.IsOwned(entry.Id);
            int level = CardPlayerProfile.GetLevel(entry.Id);
            bool equipped = CardPlayerProfile.IsCardInDeck(entry.Id);
            bool equippedElsewhere = currentMode == LobbyPanelMode.Deck && CardPlayerProfile.IsCardEquippedInAnotherSlot(entry.Id, selectedDeckSlotIndex);
            bool enabled = (!ownedOnly || owned) && !equippedElsewhere;
            bool selected = i == selectedCardIndex;

            lobbyView.CardButtons[i].interactable = enabled;
            lobbyView.CardButtons[i].targetGraphic.color = GetCatalogCardFrameColor(selected, equipped);
            ApplyCardImage(lobbyView.CardImages[i], entry, owned);
            lobbyView.CardNameTexts[i].text = owned ? entry.CardName : "???";
            lobbyView.CardMetaTexts[i].text = owned
                ? $"Lv.{level} HP {entry.MaxHp + level - 1}\n{GetTypeName(entry.CardType)}"
                : "미획득";
        }
    }

    /* 카드 목록 테두리 색으로 선택/장착 상태를 표시합니다. */
    private Color GetCatalogCardFrameColor(bool selected, bool equipped)
    {
        if (selected)
            return new Color(0.95f, 0.52f, 0.12f, 1f);

        if (equipped)
            return new Color(0.95f, 0.72f, 0.18f, 1f);

        return new Color(0.07f, 0.09f, 0.11f, 1f);
    }

    /* 카드 이미지가 있으면 색상 틴트를 빼고 원본 스프라이트를 표시합니다. */
    private void ApplyCardImage(Image image, CardCatalogEntry entry, bool visible = true)
    {
        if (image == null)
            return;

        Sprite sprite = visible ? entry.CardSprite : null;
        image.sprite = sprite;
        image.enabled = true;
        image.preserveAspect = true;
        image.color = sprite != null
            ? Color.white
            : (visible ? entry.CardColor : new Color(0.14f, 0.14f, 0.14f, 1f));
    }

    /* 덱 슬롯을 선택하고 카드 목록 테두리를 갱신합니다. */
    private void HandleDeckSlotClicked(int slotIndex)
    {
        if (currentMode != LobbyPanelMode.Deck)
            return;

        selectedDeckSlotIndex = slotIndex;
        RefreshDeckSlots();
        RefreshCatalogGrid(true);
    }

    /* 카드 클릭 시 상세 표시 또는 덱 교체를 처리합니다. */
    private void HandleCatalogCardClicked(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= CardCatalog.Cards.Length)
            return;

        selectedCardIndex = cardIndex;
        CardCatalogEntry entry = CardCatalog.Cards[cardIndex];
        bool owned = CardPlayerProfile.IsOwned(entry.Id);

        if (currentMode == LobbyPanelMode.Deck)
        {
            if (!owned)
                return;

            if (CardPlayerProfile.IsCardEquippedInAnotherSlot(entry.Id, selectedDeckSlotIndex))
            {
                lobbyView.PanelBodyText.text = "이미 다른 덱 슬롯에 장착된 카드입니다. 같은 카드는 중복 장착할 수 없습니다.";
                RefreshCatalogGrid(true);
                return;
            }

            CardPlayerProfile.SetDeckCardToSlot(selectedDeckSlotIndex, entry.Id);
            RefreshDeckSlots();
            RefreshMainInfo();
            lobbyView.PanelBodyText.text = $"{selectedDeckSlotIndex + 1}번 슬롯에 {entry.CardName} 장착 완료";
        }

        RefreshCatalogGrid(currentMode != LobbyPanelMode.Collection);
        ShowCardDetail(entry, owned);
    }

    /* 덱 편집에서 보유 카드 버튼을 슬롯 위로 드롭하면 해당 슬롯 카드로 교체합니다. */
    public void HandleCardDragEnd(int cardIndex, Vector2 screenPosition)
    {
        if (currentMode != LobbyPanelMode.Deck || cardIndex < 0 || cardIndex >= CardCatalog.Cards.Length)
            return;

        if (!CardPlayerProfile.IsOwned(CardCatalog.Cards[cardIndex].Id))
            return;

        for (int i = 0; i < lobbyView.DeckSlotButtons.Length; i++)
        {
            RectTransform slotRect = lobbyView.DeckSlotButtons[i].transform as RectTransform;
            if (slotRect == null)
                continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition))
                continue;

            selectedDeckSlotIndex = i;
            HandleCatalogCardClicked(cardIndex);
            return;
        }
    }

    /* 성장 버튼 입력 시 선택 카드의 레벨을 올립니다. */
    private void UpgradeSelectedCard()
    {
        if (currentMode != LobbyPanelMode.Growth || selectedCardIndex < 0 || selectedCardIndex >= CardCatalog.Cards.Length)
            return;

        CardCatalogEntry entry = CardCatalog.Cards[selectedCardIndex];
        CardPlayerProfile.TryUpgradeCard(entry.Id, out string message);
        RefreshMainInfo();
        ShowGrowthPanel();
        selectedCardIndex = System.Array.FindIndex(CardCatalog.Cards, card => card.Id == entry.Id);
        ShowCardDetail(entry, CardPlayerProfile.IsOwned(entry.Id));
        lobbyView.PanelBodyText.text = $"보유 재화: {CardPlayerProfile.GetGold()}G  |  {message}";
    }

    /* 현재 보이는 목록에서 첫 번째 유효 카드를 선택합니다. */
    private void SelectFirstVisibleCard()
    {
        bool ownedOnly = currentMode != LobbyPanelMode.Collection;
        for (int i = 0; i < CardCatalog.Cards.Length; i++)
        {
            if (!ownedOnly || CardPlayerProfile.IsOwned(CardCatalog.Cards[i].Id))
            {
                selectedCardIndex = i;
                RefreshCatalogGrid(ownedOnly);
                ShowCardDetail(CardCatalog.Cards[i], CardPlayerProfile.IsOwned(CardCatalog.Cards[i].Id));
                return;
            }
        }
    }

    /* 선택 카드의 이미지, 이름, 타입, 설명, 성장 정보를 표시합니다. */
    private void ShowCardDetail(CardCatalogEntry entry, bool owned)
    {
        int level = owned ? CardPlayerProfile.GetLevel(entry.Id) : 1;
        int hp = entry.MaxHp + Mathf.Max(0, level - 1);
        int cost = CardPlayerProfile.GetUpgradeCost(entry.Id);

        ApplyCardImage(lobbyView.DetailArtImage, entry, owned);
        lobbyView.DetailNameText.text = owned ? entry.CardName : "미획득 카드";
        lobbyView.DetailMetaText.text = BuildDetailMeta(entry, owned, hp, level, cost);
        lobbyView.DetailDescriptionText.text = owned
            ? entry.AbilityText
            : "현재 프로토타입에서는 모든 카드를 기본 보유합니다.";

        lobbyView.UpgradeButton.interactable = currentMode == LobbyPanelMode.Growth && owned && CardPlayerProfile.GetGold() >= cost;
    }

    /* 상세 카드 메타 정보는 현재 탭에 필요한 정보만 보여줍니다. */
    private string BuildDetailMeta(CardCatalogEntry entry, bool owned, int hp, int level, int cost)
    {
        if (!owned)
            return $"{GetTypeName(entry.CardType)} / HP {entry.MaxHp}";

        if (currentMode == LobbyPanelMode.Growth)
            return $"{GetTypeName(entry.CardType)} / HP {hp} / Lv.{level} / 성장비 {cost}G";

        return $"{GetTypeName(entry.CardType)} / HP {hp} / Lv.{level}";
    }

    /* 덱 편집 모드에서만 덱 슬롯 UI를 보여줍니다. */
    private void SetDeckSlotsVisible(bool visible)
    {
        for (int i = 0; i < lobbyView.DeckSlotButtons.Length; i++)
            lobbyView.DeckSlotButtons[i].gameObject.SetActive(visible);
    }

    /* 덱 편집과 도감/성장 모드가 서로 겹치지 않도록 상세 카드 영역을 켜고 끕니다. */
    private void SetDetailVisible(bool visible)
    {
        lobbyView.DetailArtImage.gameObject.SetActive(visible);
        lobbyView.DetailNameText.gameObject.SetActive(visible);
        lobbyView.DetailMetaText.gameObject.SetActive(visible);
        lobbyView.DetailDescriptionText.gameObject.SetActive(visible);
    }

    /* 모드별 카드 목록 위치와 크기를 조정합니다. */
    private void SetScrollAreaLayout(Vector2 position, Vector2 size)
    {
        if (lobbyView.CardScrollRoot == null)
            return;

        RectTransform rect = lobbyView.CardScrollRoot.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        ScrollRect scrollRect = lobbyView.CardScrollRoot.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 45f;
        }

        if (lobbyView.CardScrollContent != null)
        {
            lobbyView.CardScrollContent.anchorMin = new Vector2(0.5f, 1f);
            lobbyView.CardScrollContent.anchorMax = new Vector2(0.5f, 1f);
            lobbyView.CardScrollContent.pivot = new Vector2(0.5f, 1f);
            lobbyView.CardScrollContent.anchoredPosition = Vector2.zero;
            lobbyView.CardScrollContent.sizeDelta = new Vector2(size.x, 0f);
            ConfigureCatalogGridContent(lobbyView.CardScrollContent);
            UpdateCatalogContentHeight();
        }
    }

    /* 씬에 이미 배치된 카드 버튼도 카드형 비율과 스크롤 가능한 위치로 강제 정렬합니다. */
    private void ApplyCatalogCardLayout()
    {
        if (lobbyView?.CardButtons == null)
            return;

        for (int i = 0; i < lobbyView.CardButtons.Length; i++)
        {
            Button button = lobbyView.CardButtons[i];
            if (button == null)
                continue;

            RectTransform cardRect = button.transform as RectTransform;
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(178f, 232f);
            }

            SetChildRect(button.transform, "Frame", Vector2.zero, new Vector2(164f, 218f));
            SetChildRect(button.transform, "Art", new Vector2(0f, 18f), new Vector2(128f, 134f));
            SetChildRect(button.transform, "Name", new Vector2(0f, 95f), new Vector2(156f, 30f));
            SetChildRect(button.transform, "Meta", new Vector2(0f, -87f), new Vector2(156f, 50f));
        }
    }

    /* 카드가 늘어나도 자동으로 4열 정렬되고 Content 높이가 늘어나도록 설정합니다. */
    private void ConfigureCatalogGridContent(RectTransform content)
    {
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = content.gameObject.AddComponent<GridLayoutGroup>();

        grid.padding = new RectOffset(18, 18, 8, 28);
        grid.cellSize = new Vector2(178f, 232f);
        grid.spacing = new Vector2(32f, 24f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    /* 패널을 열 때 카드 목록이 항상 첫 줄부터 보이도록 스크롤 위치를 초기화합니다. */
    /* 활성 카드 개수 기준으로 Content 높이를 직접 계산해서 ScrollRect가 아래쪽에서 시작하지 않게 합니다. */
    private void UpdateCatalogContentHeight()
    {
        if (lobbyView?.CardScrollContent == null || lobbyView.CardScrollRoot == null)
            return;

        GridLayoutGroup grid = lobbyView.CardScrollContent.GetComponent<GridLayoutGroup>();
        RectTransform viewportRect = lobbyView.CardScrollRoot.GetComponent<RectTransform>();
        if (grid == null || viewportRect == null)
            return;

        int activeCount = 0;
        for (int i = 0; i < lobbyView.CardScrollContent.childCount; i++)
        {
            if (lobbyView.CardScrollContent.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }

        int columnCount = Mathf.Max(1, grid.constraintCount);
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(activeCount / (float)columnCount));
        float contentHeight = grid.padding.top
            + grid.padding.bottom
            + rowCount * grid.cellSize.y
            + Mathf.Max(0, rowCount - 1) * grid.spacing.y;

        Vector2 size = lobbyView.CardScrollContent.sizeDelta;
        size.y = Mathf.Max(viewportRect.rect.height + 1f, contentHeight);
        lobbyView.CardScrollContent.sizeDelta = size;
    }

    private void ResetCatalogScrollToTop()
    {
        if (lobbyView?.CardScrollRoot == null || lobbyView.CardScrollContent == null)
            return;

        if (resetCatalogScrollCoroutine != null)
            StopCoroutine(resetCatalogScrollCoroutine);

        resetCatalogScrollCoroutine = StartCoroutine(CoResetCatalogScrollToTop());
    }

    /* GridLayoutGroup 계산이 끝난 뒤 한 번 더 맨 위 위치로 고정합니다. */
    private IEnumerator CoResetCatalogScrollToTop()
    {
        UpdateCatalogContentHeight();
        Canvas.ForceUpdateCanvases();
        SetCatalogScrollTopNow();

        yield return null;

        UpdateCatalogContentHeight();
        Canvas.ForceUpdateCanvases();
        SetCatalogScrollTopNow();

        yield return new WaitForEndOfFrame();

        UpdateCatalogContentHeight();
        Canvas.ForceUpdateCanvases();
        SetCatalogScrollTopNow();
        resetCatalogScrollCoroutine = null;
    }

    /* ScrollRect와 Content 좌표를 맨 위 기준으로 즉시 맞춥니다. */
    private void SetCatalogScrollTopNow()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(lobbyView.CardScrollContent);

        ScrollRect scrollRect = lobbyView.CardScrollRoot.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.velocity = Vector2.zero;
            scrollRect.content = lobbyView.CardScrollContent;
            scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        lobbyView.CardScrollContent.anchoredPosition = new Vector2(0f, 0f);
    }

    /* 카드 버튼의 자식 RectTransform 위치와 크기를 조정합니다. */
    private void SetChildRect(Transform parent, string childName, Vector2 position, Vector2 size)
    {
        Transform child = parent.Find(childName);
        RectTransform rect = child as RectTransform;
        if (rect == null)
            return;

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /* 성장 모드에서만 성장 버튼을 보여줍니다. */
    private void SetUpgradeVisible(bool visible)
    {
        lobbyView.UpgradeButton.gameObject.SetActive(visible);
    }

    /* 현재 모드에 맞게 탭 색상을 갱신합니다. */
    private void RefreshTabColors()
    {
        SetTabColor(lobbyView.TabDeckButton, currentMode == LobbyPanelMode.Deck);
        SetTabColor(lobbyView.TabCollectionButton, currentMode == LobbyPanelMode.Collection);
        SetTabColor(lobbyView.TabGrowthButton, currentMode == LobbyPanelMode.Growth);
    }

    /* 탭 선택 상태에 따라 버튼 색상을 바꿉니다. */
    private void SetTabColor(Button button, bool selected)
    {
        if (button?.targetGraphic != null)
            button.targetGraphic.color = selected ? new Color(0.5f, 0.18f, 0.08f, 1f) : new Color(0.18f, 0.12f, 0.07f, 1f);
    }

    /* 카드게임 저장 데이터를 초기화하고 성장 패널을 다시 엽니다. */
    private void ResetProfile()
    {
        CardPlayerProfile.ResetProfile();
        RefreshMainInfo();
        ShowGrowthPanel();
    }

    /* 카드 타입 enum을 화면 표시 이름으로 변환합니다. */
    private string GetTypeName(BattleCardType type)
    {
        return type switch
        {
            BattleCardType.Normal => "일반",
            BattleCardType.Ranged => "원거리",
            BattleCardType.Musou => "무쌍",
            BattleCardType.Healer => "힐러",
            BattleCardType.Bomber => "폭탄",
            BattleCardType.Vampire => "흡혈",
            BattleCardType.Berserker => "광전사",
            BattleCardType.Guardian => "수호자",
            BattleCardType.Piercing => "관통",
            _ => "기타"
        };
    }

    /* 씬 전환 직후 실수 클릭으로 전투 시작이 바로 눌리지 않게 지연합니다. */
    private IEnumerator CoEnableStartButton()
    {
        yield return new WaitForSeconds(0.35f);

        canStartBattle = true;
        if (lobbyView?.StartButton != null)
            lobbyView.StartButton.interactable = true;
    }
}

public enum LobbyPanelMode
{
    None,
    Deck,
    Collection,
    Growth
}
