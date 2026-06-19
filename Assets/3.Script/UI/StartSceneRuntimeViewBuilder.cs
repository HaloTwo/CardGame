using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class StartSceneRuntimeViewBuilder
{
    private static int CollectionCardCount => Mathf.Max(12, CardCatalog.Cards.Length); /* 카드가 늘어나도 자동으로 확보할 버튼 풀 개수 */

    /* 로비 씬에 필요한 UI를 구성합니다. 카드 버튼은 시작 시 미리 생성해 재사용합니다. */
    public static LobbyViewReferences CreateStartView()
    {
        EnsureInputSystemEventSystem();

        GameObject existingRoot = GameObject.Find("LobbyView");
        LobbyViewReferences existingView = existingRoot != null ? CollectExistingView(existingRoot) : null;
        if (existingView != null)
            return existingView;

        if (existingRoot != null)
            DestroyObject(existingRoot);

        Canvas canvas = CreateCanvas();
        GameObject root = CreateUIObject("LobbyView", canvas.transform, Vector2.zero, Vector2.one);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.035f, 0.032f, 0.03f, 1f);

        CreatePanel("TopBand", root.transform, new Vector2(0f, 735f), new Vector2(1080f, 330f), new Color(0.16f, 0.07f, 0.035f, 1f));
        CreatePanel("BottomBand", root.transform, new Vector2(0f, -705f), new Vector2(1080f, 350f), new Color(0.06f, 0.045f, 0.035f, 1f));
        CreatePanel("GoldLine", root.transform, new Vector2(0f, 390f), new Vector2(880f, 8f), new Color(0.95f, 0.57f, 0.13f, 1f));
        CreatePanel("BlueLine", root.transform, new Vector2(0f, -455f), new Vector2(880f, 8f), new Color(0.08f, 0.4f, 0.78f, 1f));
        CreateDecorCard(root.transform, new Vector2(-405f, 530f), new Color(0.95f, 0.34f, 0.12f, 1f), "HP");
        CreateDecorCard(root.transform, new Vector2(405f, 520f), new Color(0.22f, 0.56f, 1f, 1f), "FX");
        CreateDecorCard(root.transform, new Vector2(-460f, -390f), new Color(0.45f, 1f, 0.55f, 1f), "Lv");
        CreateDecorCard(root.transform, new Vector2(460f, -390f), new Color(0.78f, 0.22f, 0.82f, 1f), "SK");

        CreateText("TitleShadow", root.transform, new Vector2(5f, 790f), "BURGER MONSTER", 78, new Color(0f, 0f, 0f, 0.48f), new Vector2(950f, 110f));
        CreateText("TitleText", root.transform, new Vector2(0f, 798f), "BURGER MONSTER", 78, new Color(1f, 0.72f, 0.18f, 1f), new Vector2(950f, 110f));
        CreateText("SubTitleText", root.transform, new Vector2(0f, 705f), "CARD BATTLE LOBBY", 40, Color.white, new Vector2(850f, 70f));

        GameObject infoBox = CreatePanel("MainInfoBox", root.transform, new Vector2(0f, 565f), new Vector2(900f, 138f), new Color(0.12f, 0.085f, 0.055f, 0.94f));
        Text mainInfoText = CreateText("MainInfoText", infoBox.transform, Vector2.zero, string.Empty, 31, new Color(0.98f, 0.91f, 0.74f, 1f), new Vector2(830f, 112f));

        GameObject menuBox = CreatePanel("MenuBox", root.transform, new Vector2(0f, 145f), new Vector2(660f, 600f), new Color(0.09f, 0.064f, 0.043f, 0.96f));
        CreateText("MenuTitle", menuBox.transform, new Vector2(0f, 240f), "LOBBY MENU", 32, new Color(1f, 0.68f, 0.18f, 1f), new Vector2(520f, 60f));

        Button startButton = CreateButton("StartBattleButton", menuBox.transform, new Vector2(0f, 130f), "\uC804\uD22C \uC2DC\uC791", new Vector2(500f, 96f), new Color(0.82f, 0.27f, 0.08f, 1f), 34);
        Button deckButton = CreateButton("DeckEditButton", menuBox.transform, new Vector2(0f, 18f), "\uB371 \uD3B8\uC9D1", new Vector2(500f, 82f), new Color(0.24f, 0.17f, 0.1f, 1f), 31);
        Button collectionButton = CreateButton("CollectionButton", menuBox.transform, new Vector2(0f, -82f), "\uCE74\uB4DC \uB3C4\uAC10", new Vector2(500f, 82f), new Color(0.24f, 0.17f, 0.1f, 1f), 31);
        Button growthButton = CreateButton("GrowthButton", menuBox.transform, new Vector2(0f, -182f), "\uC131\uC7A5", new Vector2(500f, 82f), new Color(0.24f, 0.17f, 0.1f, 1f), 31);
        Button resetButton = CreateButton("ResetButton", menuBox.transform, new Vector2(0f, -278f), "\uC800\uC7A5 \uCD08\uAE30\uD654", new Vector2(420f, 62f), new Color(0.13f, 0.105f, 0.085f, 1f), 25);

        GameObject panel = CreatePanel("LobbyPanel", root.transform, new Vector2(0f, -60f), new Vector2(980f, 1400f), new Color(0.052f, 0.047f, 0.041f, 0.99f));
        CreatePanel("LobbyPanelHeader", panel.transform, new Vector2(0f, 610f), new Vector2(910f, 92f), new Color(0.17f, 0.08f, 0.035f, 1f));
        Text panelTitleText = CreateText("PanelTitleText", panel.transform, new Vector2(0f, 610f), string.Empty, 42, new Color(1f, 0.72f, 0.18f, 1f), new Vector2(860f, 70f));
        Text panelBodyText = CreateText("PanelBodyText", panel.transform, new Vector2(0f, 515f), string.Empty, 25, Color.white, new Vector2(880f, 74f));

        Button tabDeckButton = CreateButton("TabDeckButton", panel.transform, new Vector2(-250f, 445f), "\uB371", new Vector2(180f, 58f), new Color(0.18f, 0.12f, 0.07f, 1f), 24);
        Button tabCollectionButton = CreateButton("TabCollectionButton", panel.transform, new Vector2(0f, 445f), "\uB3C4\uAC10", new Vector2(180f, 58f), new Color(0.18f, 0.12f, 0.07f, 1f), 24);
        Button tabGrowthButton = CreateButton("TabGrowthButton", panel.transform, new Vector2(250f, 445f), "\uC131\uC7A5", new Vector2(180f, 58f), new Color(0.18f, 0.12f, 0.07f, 1f), 24);

        Image detailArt = CreateImage("DetailArt", panel.transform, new Vector2(-285f, 260f), new Vector2(250f, 255f), new Color(0.24f, 0.18f, 0.12f, 1f));
        Text detailName = CreateText("DetailName", panel.transform, new Vector2(155f, 355f), "CARD", 42, new Color(1f, 0.76f, 0.2f, 1f), new Vector2(500f, 70f));
        Text detailMeta = CreateText("DetailMeta", panel.transform, new Vector2(155f, 288f), "TYPE / HP", 28, Color.white, new Vector2(500f, 56f));
        Text detailDesc = CreateText("DetailDesc", panel.transform, new Vector2(155f, 185f), string.Empty, 24, new Color(0.92f, 0.88f, 0.78f, 1f), new Vector2(505f, 130f));
        detailDesc.alignment = TextAnchor.UpperLeft;
        detailDesc.verticalOverflow = VerticalWrapMode.Overflow;
        Button upgradeButton = CreateButton("UpgradeButton", panel.transform, new Vector2(155f, 84f), "\uC131\uC7A5", new Vector2(300f, 66f), new Color(0.5f, 0.18f, 0.08f, 1f), 27);

        Button[] deckSlotButtons = new Button[CardCatalog.DeckSize];
        Image[] deckSlotImages = new Image[CardCatalog.DeckSize];
        Text[] deckSlotTexts = new Text[CardCatalog.DeckSize];
        for (int i = 0; i < deckSlotButtons.Length; i++)
        {
            int column = i % 3;
            int row = i / 3;
            Vector2 position = new Vector2(-280f + column * 280f, 255f - row * 165f);
            deckSlotButtons[i] = CreateMiniCardButton($"DeckSlotButton{i}", panel.transform, position, new Vector2(230f, 145f), out deckSlotImages[i], out deckSlotTexts[i]);
        }

        GameObject scrollRoot = CreateScrollArea(panel.transform, new Vector2(0f, -250f), new Vector2(900f, 500f), out RectTransform scrollContent);
        Button[] cardButtons = new Button[CollectionCardCount];
        Image[] cardImages = new Image[CollectionCardCount];
        Text[] cardNameTexts = new Text[CollectionCardCount];
        Text[] cardMetaTexts = new Text[CollectionCardCount];
        for (int i = 0; i < cardButtons.Length; i++)
        {
            cardButtons[i] = CreateCatalogCardButton($"CatalogCardButton{i}", scrollContent, Vector2.zero, out cardImages[i], out cardNameTexts[i], out cardMetaTexts[i]);
        }

        Button backButton = CreateButton("BackButton", panel.transform, new Vector2(0f, -635f), "\uB2EB\uAE30", new Vector2(320f, 72f), new Color(0.32f, 0.18f, 0.09f, 1f), 30);
        panel.SetActive(false);

        return new LobbyViewReferences
        {
            StartButton = startButton,
            DeckButton = deckButton,
            CollectionButton = collectionButton,
            GrowthButton = growthButton,
            ResetButton = resetButton,
            BackButton = backButton,
            TabDeckButton = tabDeckButton,
            TabCollectionButton = tabCollectionButton,
            TabGrowthButton = tabGrowthButton,
            UpgradeButton = upgradeButton,
            MainInfoText = mainInfoText,
            PanelTitleText = panelTitleText,
            PanelBodyText = panelBodyText,
            Panel = panel,
            CardScrollRoot = scrollRoot,
            CardScrollContent = scrollContent,
            DeckSlotButtons = deckSlotButtons,
            DeckSlotImages = deckSlotImages,
            DeckSlotTexts = deckSlotTexts,
            CardButtons = cardButtons,
            CardImages = cardImages,
            CardNameTexts = cardNameTexts,
            CardMetaTexts = cardMetaTexts,
            DetailArtImage = detailArt,
            DetailNameText = detailName,
            DetailMetaText = detailMeta,
            DetailDescriptionText = detailDesc
        };
    }

    /* Input System UI 모듈을 보장합니다. */
    private static void EnsureInputSystemEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
            DestroyObject(oldModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    /* 세로형 모바일 기준 Canvas를 생성합니다. */
    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("LobbyCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /* 스크롤 가능한 카드 목록 영역을 생성합니다. */
    private static GameObject CreateScrollArea(Transform parent, Vector2 position, Vector2 size, out RectTransform content)
    {
        GameObject viewport = CreatePanel("CardScrollViewport", parent, position, size, new Color(0.028f, 0.034f, 0.04f, 1f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewport.AddComponent<Mask>().showMaskGraphic = true;

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 35f;

        GameObject contentObject = CreateUIObject("CardScrollContent", viewport.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        content = contentObject.GetComponent<RectTransform>();
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(size.x, 0f);
        ConfigureCardGridContent(content);
        scrollRect.viewport = viewportRect;
        scrollRect.content = content;
        return viewport;
    }

    /* 카드 목록 Content에 자동 정렬 컴포넌트를 붙입니다. */
    private static void ConfigureCardGridContent(RectTransform content)
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
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /* 씬에 이미 배치된 로비 UI가 있으면 이름 기준으로 참조를 다시 모읍니다. */
    private static LobbyViewReferences CollectExistingView(GameObject root)
    {
        Button[] deckSlotButtons = new Button[CardCatalog.DeckSize];
        Image[] deckSlotImages = new Image[CardCatalog.DeckSize];
        Text[] deckSlotTexts = new Text[CardCatalog.DeckSize];
        for (int i = 0; i < deckSlotButtons.Length; i++)
        {
            Transform slot = FindDeepChild(root.transform, $"DeckSlotButton{i}");
            if (slot == null)
                return null;

            deckSlotButtons[i] = slot.GetComponent<Button>();
            deckSlotImages[i] = FindDeepComponent<Image>(slot, "Art");
            deckSlotTexts[i] = FindDeepComponent<Text>(slot, "Label");
        }

        Button[] cardButtons = new Button[CollectionCardCount];
        Image[] cardImages = new Image[CollectionCardCount];
        Text[] cardNameTexts = new Text[CollectionCardCount];
        Text[] cardMetaTexts = new Text[CollectionCardCount];
        for (int i = 0; i < cardButtons.Length; i++)
        {
            Transform card = FindDeepChild(root.transform, $"CatalogCardButton{i}");
            if (card == null)
                return null;

            cardButtons[i] = card.GetComponent<Button>();
            cardImages[i] = FindDeepComponent<Image>(card, "Art");
            cardNameTexts[i] = FindDeepComponent<Text>(card, "Name");
            cardMetaTexts[i] = FindDeepComponent<Text>(card, "Meta");
        }

        Transform scrollContent = FindDeepChild(root.transform, "CardScrollContent");
        LobbyViewReferences view = new()
        {
            StartButton = FindDeepComponent<Button>(root.transform, "StartBattleButton"),
            DeckButton = FindDeepComponent<Button>(root.transform, "DeckEditButton"),
            CollectionButton = FindDeepComponent<Button>(root.transform, "CollectionButton"),
            GrowthButton = FindDeepComponent<Button>(root.transform, "GrowthButton"),
            ResetButton = FindDeepComponent<Button>(root.transform, "ResetButton"),
            BackButton = FindDeepComponent<Button>(root.transform, "BackButton"),
            TabDeckButton = FindDeepComponent<Button>(root.transform, "TabDeckButton"),
            TabCollectionButton = FindDeepComponent<Button>(root.transform, "TabCollectionButton"),
            TabGrowthButton = FindDeepComponent<Button>(root.transform, "TabGrowthButton"),
            UpgradeButton = FindDeepComponent<Button>(root.transform, "UpgradeButton"),
            MainInfoText = FindDeepComponent<Text>(root.transform, "MainInfoText"),
            PanelTitleText = FindDeepComponent<Text>(root.transform, "PanelTitleText"),
            PanelBodyText = FindDeepComponent<Text>(root.transform, "PanelBodyText"),
            Panel = FindDeepChild(root.transform, "LobbyPanel")?.gameObject,
            CardScrollRoot = FindDeepChild(root.transform, "CardScrollViewport")?.gameObject,
            CardScrollContent = scrollContent as RectTransform,
            DeckSlotButtons = deckSlotButtons,
            DeckSlotImages = deckSlotImages,
            DeckSlotTexts = deckSlotTexts,
            CardButtons = cardButtons,
            CardImages = cardImages,
            CardNameTexts = cardNameTexts,
            CardMetaTexts = cardMetaTexts,
            DetailArtImage = FindDeepComponent<Image>(root.transform, "DetailArt"),
            DetailNameText = FindDeepComponent<Text>(root.transform, "DetailName"),
            DetailMetaText = FindDeepComponent<Text>(root.transform, "DetailMeta"),
            DetailDescriptionText = FindDeepComponent<Text>(root.transform, "DetailDesc")
        };

        return HasRequiredReferences(view) ? view : null;
    }

    /* 기존 씬 UI를 재사용할 수 있는 최소 참조가 있는지 검사합니다. */
    private static bool HasRequiredReferences(LobbyViewReferences view)
    {
        if (view.DeckSlotButtons == null || view.DeckSlotImages == null || view.DeckSlotTexts == null || view.CardButtons == null || view.CardImages == null || view.CardNameTexts == null || view.CardMetaTexts == null)
            return false;

        for (int i = 0; i < view.DeckSlotButtons.Length; i++)
        {
            if (view.DeckSlotButtons[i] == null || view.DeckSlotImages[i] == null || view.DeckSlotTexts[i] == null)
                return false;
        }

        for (int i = 0; i < view.CardButtons.Length; i++)
        {
            if (view.CardButtons[i] == null || view.CardImages[i] == null || view.CardNameTexts[i] == null || view.CardMetaTexts[i] == null)
                return false;
        }

        return view.StartButton != null
            && view.DeckButton != null
            && view.CollectionButton != null
            && view.GrowthButton != null
            && view.ResetButton != null
            && view.BackButton != null
            && view.TabDeckButton != null
            && view.TabCollectionButton != null
            && view.TabGrowthButton != null
            && view.UpgradeButton != null
            && view.MainInfoText != null
            && view.PanelTitleText != null
            && view.PanelBodyText != null
            && view.Panel != null
            && view.CardScrollRoot != null
            && view.CardScrollContent != null
            && view.DetailArtImage != null
            && view.DetailNameText != null
            && view.DetailMetaText != null
            && view.DetailDescriptionText != null;
    }

    /* 자식 오브젝트 이름으로 깊이 탐색합니다. */
    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    /* 이름으로 찾은 자식에서 필요한 컴포넌트를 가져옵니다. */
    private static T FindDeepComponent<T>(Transform parent, string childName) where T : Component
    {
        Transform child = FindDeepChild(parent, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    /* 기본 UI 오브젝트를 생성하고 앵커를 설정합니다. */
    private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    /* 로비 배경 장식 카드를 배치합니다. */
    private static void CreateDecorCard(Transform parent, Vector2 position, Color color, string label)
    {
        GameObject card = CreatePanel("DecorCard", parent, position, new Vector2(172f, 238f), new Color(0.09f, 0.07f, 0.055f, 1f));
        CreatePanel("DecorCardArt", card.transform, new Vector2(0f, 28f), new Vector2(134f, 115f), color);
        CreateText("DecorCardText", card.transform, new Vector2(0f, -58f), label, 30, Color.white, new Vector2(148f, 50f));
        card.transform.localRotation = Quaternion.Euler(0f, 0f, position.x < 0f ? -7f : 7f);
    }

    /* 단색 패널을 생성합니다. */
    private static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = CreateUIObject(name, parent, AnchorCenter(), AnchorCenter());
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = CreatePanel(name, parent, position, size, color);
        return obj.GetComponent<Image>();
    }

    /* 카드 도감용 세로 카드 버튼을 생성합니다. */
    private static Button CreateCatalogCardButton(string name, Transform parent, Vector2 position, out Image artImage, out Text nameText, out Text metaText)
    {
        GameObject card = CreatePanel(name, parent, position, new Vector2(178f, 232f), new Color(0.07f, 0.09f, 0.11f, 1f));
        Button button = card.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();

        CreatePanel("Frame", card.transform, Vector2.zero, new Vector2(164f, 218f), new Color(0.05f, 0.18f, 0.25f, 1f));
        artImage = CreateImage("Art", card.transform, new Vector2(0f, 18f), new Vector2(128f, 134f), Color.gray);
        nameText = CreateText("Name", card.transform, new Vector2(0f, 95f), "CARD", 18, Color.white, new Vector2(156f, 30f));
        metaText = CreateText("Meta", card.transform, new Vector2(0f, -87f), "Lv.1 HP 0", 16, new Color(1f, 0.78f, 0.2f, 1f), new Vector2(156f, 50f));
        nameText.transform.SetAsLastSibling();
        metaText.transform.SetAsLastSibling();
        return button;
    }

    /* 덱 편집용 카드 슬롯 버튼을 생성합니다. */
    private static Button CreateMiniCardButton(string name, Transform parent, Vector2 position, Vector2 size, out Image artImage, out Text labelText)
    {
        GameObject card = CreatePanel(name, parent, position, size, new Color(0.12f, 0.08f, 0.055f, 1f));
        Button button = card.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();

        artImage = CreateImage("Art", card.transform, new Vector2(-58f, 0f), new Vector2(92f, 104f), Color.gray);
        labelText = CreateText("Label", card.transform, new Vector2(45f, 0f), "CARD", 20, Color.white, new Vector2(118f, 120f));
        labelText.alignment = TextAnchor.MiddleLeft;
        return button;
    }

    private static Text CreateText(string name, Transform parent, Vector2 position, string value, int fontSize, Color color, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, AnchorCenter(), AnchorCenter());
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 position, string label, Vector2 size, Color color, int fontSize)
    {
        GameObject obj = CreateUIObject(name, parent, AnchorCenter(), AnchorCenter());
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = color;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        Text labelText = CreateText("Text", obj.transform, Vector2.zero, label, fontSize, Color.white, new Vector2(size.x - 42f, size.y));
        labelText.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static Vector2 AnchorCenter()
    {
        return new Vector2(0.5f, 0.5f);
    }

    /* Play Mode와 Edit Mode에 맞춰 안전하게 오브젝트를 제거합니다. */
    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }
}

public class LobbyViewReferences
{
    public Button StartButton { get; set; } /* 전투 시작 버튼 */
    public Button DeckButton { get; set; } /* 덱 편집 버튼 */
    public Button CollectionButton { get; set; } /* 카드 도감 버튼 */
    public Button GrowthButton { get; set; } /* 성장 버튼 */
    public Button ResetButton { get; set; } /* 저장 초기화 버튼 */
    public Button BackButton { get; set; } /* 패널 닫기 버튼 */
    public Button TabDeckButton { get; set; } /* 패널 내부 덱 탭 */
    public Button TabCollectionButton { get; set; } /* 패널 내부 도감 탭 */
    public Button TabGrowthButton { get; set; } /* 패널 내부 성장 탭 */
    public Button UpgradeButton { get; set; } /* 선택 카드 성장 버튼 */
    public Text MainInfoText { get; set; } /* 로비 요약 정보 */
    public Text PanelTitleText { get; set; } /* 팝업 제목 */
    public Text PanelBodyText { get; set; } /* 팝업 안내 문구 */
    public GameObject Panel { get; set; } /* 공용 팝업 패널 */
    public GameObject CardScrollRoot { get; set; } /* 카드 목록 스크롤 루트 */
    public RectTransform CardScrollContent { get; set; } /* 카드 목록 스크롤 콘텐츠 */
    public Button[] DeckSlotButtons { get; set; } /* 덱 슬롯 버튼 목록 */
    public Image[] DeckSlotImages { get; set; } /* 덱 슬롯 카드 이미지 */
    public Text[] DeckSlotTexts { get; set; } /* 덱 슬롯 텍스트 목록 */
    public Button[] CardButtons { get; set; } /* 카드 버튼 풀 */
    public Image[] CardImages { get; set; } /* 카드 이미지 풀 */
    public Text[] CardNameTexts { get; set; } /* 카드 이름 텍스트 풀 */
    public Text[] CardMetaTexts { get; set; } /* 카드 상태 텍스트 풀 */
    public Image DetailArtImage { get; set; } /* 선택 카드 큰 이미지 */
    public Text DetailNameText { get; set; } /* 선택 카드 이름 */
    public Text DetailMetaText { get; set; } /* 선택 카드 타입/HP/성장 정보 */
    public Text DetailDescriptionText { get; set; } /* 선택 카드 설명 */
}
