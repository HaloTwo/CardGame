using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class CardBattleRuntimeViewBuilder
{
    private const float CardWidth = 270f; /* 카드 UI 가로 크기 */
    private const float CardHeight = 350f; /* 카드 UI 세로 크기 */
    private const float CardGap = 330f; /* 필드 카드 슬롯 간격 */
    private const string CardSlotPrefabPath = "CardSlotView"; /* Resources/CardSlotView.prefab 우선 사용 */
    private static readonly Vector2 ArtworkPosition = new Vector2(0f, 14f); /* 카드 일러스트 위치 */
    private static readonly Vector2 ArtworkSize = new Vector2(160f, 192f); /* 카드 일러스트 크기 */

    /* 씬 UI가 없을 때 사용하는 백업 전투 UI를 생성합니다. */
    public static CardBattleView CreateOrRepair()
    {
        EnsureInputSystemEventSystem();
        DisableOldBattleViews();

        Canvas canvas = CreateCanvas();
        GameObject root = CreateUIObject("CardBattleView_Runtime", canvas.transform, Vector2.zero, Vector2.one);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.055f, 0.062f, 0.075f, 1f);

        CreatePanel("TopGlow", root.transform, new Vector2(0f, 730f), new Vector2(1080f, 350f), new Color(0.11f, 0.08f, 0.05f, 1f));
        CreatePanel("BottomGlow", root.transform, new Vector2(0f, -710f), new Vector2(1080f, 370f), new Color(0.09f, 0.065f, 0.04f, 1f));

        CreateFieldPanel(root.transform, new Vector2(0f, 520f), "ENEMY FIELD", new Color(0.11f, 0.13f, 0.19f, 0.92f));
        CreateFieldPanel(root.transform, new Vector2(0f, -360f), "PLAYER FIELD", new Color(0.16f, 0.12f, 0.075f, 0.94f));

        CardSlotView[] enemySlots = CreateSlots(root.transform, "EnemySlot", 520f);
        CardSlotView[] playerSlots = CreateSlots(root.transform, "PlayerSlot", -360f);

        CreateFieldTitle(root.transform, new Vector2(0f, 728f), "ENEMY FIELD");
        CreateFieldTitle(root.transform, new Vector2(0f, -152f), "PLAYER FIELD");

        Button enemyDeckButton = CreateDeckBack(root.transform, new Vector2(512f, 610f));
        Button playerDeckButton = CreateDeckBack(root.transform, new Vector2(512f, -270f));
        Text enemyDeck = CreateDeckCountText(root.transform, new Vector2(512f, 610f));
        Text playerDeck = CreateDeckCountText(root.transform, new Vector2(512f, -270f));

        CreatePanel("TurnInfoPanel", root.transform, new Vector2(0f, 88f), new Vector2(900f, 138f), new Color(0.08f, 0.07f, 0.06f, 0.96f));
        Text turn = CreateText("TurnText", root.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 126f), "PLAYER TURN", 48, new Color(1f, 0.73f, 0.18f, 1f), new Vector2(620f, 76f));
        Text info = CreateText("InfoText", root.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 58f), "\uC0AC\uC6A9\uD560 \uC544\uAD70 \uCE74\uB4DC\uB97C \uC120\uD0DD\uD558\uC138\uC694.", 32, Color.white, new Vector2(840f, 70f));

        CreatePanel("ActionBar", root.transform, new Vector2(0f, -735f), new Vector2(790f, 118f), new Color(0.075f, 0.058f, 0.045f, 0.98f));
        Button attack = CreateButton("AttackButton", root.transform, new Vector2(-190f, -735f), "\uAE30\uBCF8\uACF5\uACA9", new Vector2(330f, 84f), new Color(0.46f, 0.17f, 0.08f, 1f), 31);
        Button skill = CreateButton("SkillButton", root.transform, new Vector2(190f, -735f), "\uCE74\uB4DC\uD6A8\uACFC", new Vector2(330f, 84f), new Color(0.16f, 0.27f, 0.52f, 1f), 31);
        Button menuButton = CreateButton("MenuButton", root.transform, new Vector2(-452f, 835f), "MENU", new Vector2(190f, 58f), new Color(0.15f, 0.11f, 0.08f, 1f), 28);

        GameObject resultPanel = CreateResultPanel(root.transform, out Text resultText, out Button retryButton, out Button homeButton);
        GameObject deckInfoPanel = CreateDeckInfoPanel(root.transform, out Text deckInfoText, out Button deckInfoCloseButton);
        GameObject optionPanel = CreateOptionPanel(root.transform, out Button optionRetryButton, out Button optionLobbyButton, out Button optionCloseButton);
        CreateImpactLayer(root.transform, out Image impactFlashImage, out Text impactText);

        CardBattleView view = root.AddComponent<CardBattleView>();
        view.SetupReferences(turn, info, playerDeck, enemyDeck, attack, skill, menuButton, retryButton, homeButton, playerDeckButton, enemyDeckButton, optionRetryButton, optionLobbyButton, optionCloseButton, resultPanel, resultText, deckInfoPanel, deckInfoText, deckInfoCloseButton, optionPanel, impactFlashImage, impactText, playerSlots, enemySlots);
        return view;
    }

    /* 기존 백업 UI를 제거해 Canvas 중복을 막습니다. */
    private static void DisableOldBattleViews()
    {
        CardBattleView[] existingViews = Object.FindObjectsByType<CardBattleView>(FindObjectsSortMode.None);
        for (int i = 0; i < existingViews.Length; i++)
            DestroyObject(existingViews[i].gameObject);
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

    /* 세로형 전투 Canvas를 생성합니다. */
    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("CardBattleCanvas");
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

    /* 한 진영의 공개 카드 슬롯 3개를 배치합니다. */
    private static CardSlotView[] CreateSlots(Transform parent, string prefix, float y)
    {
        CardSlotView[] slots = new CardSlotView[3];
        CardSlotView slotPrefab = Resources.Load<CardSlotView>(CardSlotPrefabPath);

        for (int i = 0; i < slots.Length; i++)
        {
            Vector2 slotPosition = new Vector2((i - 1) * CardGap, y);
            CreateSlotShadow(parent, prefix, i, slotPosition);

            slots[i] = slotPrefab != null
                ? CreateSlotFromPrefab(slotPrefab, parent, prefix, i, slotPosition)
                : CreateRuntimeSlot(parent, prefix, i, slotPosition);
        }

        return slots;
    }

    /* 카드 슬롯 그림자를 생성합니다. */
    private static void CreateSlotShadow(Transform parent, string prefix, int index, Vector2 slotPosition)
    {
        GameObject shadow = CreatePanel($"{prefix}{index + 1}_Shadow", parent, slotPosition + new Vector2(10f, -12f), new Vector2(CardWidth, CardHeight), new Color(0f, 0f, 0f, 0.35f));
        shadow.transform.SetAsFirstSibling();
    }

    /* 프리팹 기반 카드 슬롯을 생성합니다. */
    private static CardSlotView CreateSlotFromPrefab(CardSlotView prefab, Transform parent, string prefix, int index, Vector2 slotPosition)
    {
        CardSlotView slot = Object.Instantiate(prefab, parent);
        slot.name = $"{prefix}{index + 1}";

        RectTransform rect = slot.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = AnchorCenter();
            rect.anchorMax = AnchorCenter();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = slotPosition;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            rect.localScale = Vector3.one;
        }

        ApplyCardSlotChildLayout(slot.transform);
        return slot;
    }

    /* 프리팹이 없을 때만 쓰는 백업 카드 슬롯 생성 코드입니다. */
    private static CardSlotView CreateRuntimeSlot(Transform parent, string prefix, int index, Vector2 slotPosition)
    {
        GameObject slotObject = CreateUIObject($"{prefix}{index + 1}", parent, AnchorCenter(), AnchorCenter());
        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.anchoredPosition = slotPosition;
        rect.sizeDelta = new Vector2(CardWidth, CardHeight);

        Image background = slotObject.AddComponent<Image>();
        background.color = new Color(0.98f, 0.92f, 0.76f, 1f);

        Button button = slotObject.AddComponent<Button>();
        button.targetGraphic = background;

        CreatePanel("CardHeader", slotObject.transform, new Vector2(0f, 137f), new Vector2(238f, 54f), new Color(0.16f, 0.11f, 0.07f, 0.94f));
        Text name = CreateText("NameText", slotObject.transform, AnchorTop(), AnchorTop(), new Vector2(0f, -31f), "CARD", 29, Color.white, new Vector2(230f, 44f));
        Image artwork = CreateImage("Artwork", slotObject.transform, AnchorCenter(), AnchorCenter(), ArtworkPosition, ArtworkSize);
        artwork.preserveAspect = true;
        artwork.color = Color.white;
        artwork.raycastTarget = false;
        Text type = CreateText("TypeText", slotObject.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, -97f), "TYPE", 20, new Color(0.18f, 0.15f, 0.12f, 1f), new Vector2(230f, 26f));
        Text hp = CreateText("HpText", slotObject.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, -126f), "HP", 28, new Color(0.12f, 0.08f, 0.045f, 1f), new Vector2(230f, 34f));
        Text ability = CreateText("AbilityText", slotObject.transform, AnchorBottom(), AnchorBottom(), new Vector2(0f, 16f), "\uB2A5\uB825", 15, new Color(0.1f, 0.075f, 0.045f, 1f), new Vector2(232f, 34f));
        ability.resizeTextForBestFit = true;
        ability.resizeTextMinSize = 10;
        ability.resizeTextMaxSize = 15;

        CardSlotView slot = slotObject.AddComponent<CardSlotView>();
        slot.SetupReferences(button, background, artwork, name, type, hp, ability);
        return slot;
    }

    /* 기본 UI 오브젝트를 생성합니다. */
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

    /* 필드 배경 패널을 생성합니다. */
    private static void CreateFieldPanel(Transform parent, Vector2 position, string label, Color color)
    {
        CreatePanel(label, parent, position, new Vector2(1030f, 430f), color);
    }

    /* 카드보다 위에 보이는 필드 타이틀 배너를 생성합니다. */
    private static void CreateFieldTitle(Transform parent, Vector2 position, string label)
    {
        CreatePanel($"{label}_TitleBack", parent, position, new Vector2(360f, 50f), new Color(0.04f, 0.035f, 0.03f, 0.92f));
        Text text = CreateText($"{label}_Title", parent, AnchorCenter(), AnchorCenter(), position + new Vector2(0f, 2f), label, 28, new Color(1f, 0.74f, 0.18f, 1f), new Vector2(360f, 50f));
        text.transform.SetAsLastSibling();
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

    /* 카드와 겹치지 않는 작은 대기 카드 더미 버튼을 생성합니다. */
    private static Button CreateDeckBack(Transform parent, Vector2 position)
    {
        GameObject deck = CreatePanel("DeckBack", parent, position, new Vector2(86f, 118f), new Color(0.055f, 0.135f, 0.32f, 1f));
        Sprite cardBackSprite = Resources.Load<Sprite>("CardBack"); // 카드 뒷면 이미지를 Resources/CardBack에서 자동으로 읽습니다.
        Image deckImage = deck.GetComponent<Image>();
        if (cardBackSprite != null && deckImage != null)
        {
            deckImage.sprite = cardBackSprite;
            deckImage.color = Color.white;
            deckImage.preserveAspect = true;
        }
        else
        {
            CreatePanel("DeckBackInner", deck.transform, Vector2.zero, new Vector2(64f, 92f), new Color(0.08f, 0.23f, 0.48f, 1f));
        }

        Button button = deck.AddComponent<Button>();
        button.targetGraphic = deck.GetComponent<Image>();
        return button;
    }

    /* 대기 카드 수 텍스트를 더미 위에 배치합니다. */
    private static Text CreateDeckCountText(Transform parent, Vector2 position)
    {
        return CreateText("DeckCountText", parent, AnchorCenter(), AnchorCenter(), position, "\uB300\uAE30\n3\uC7A5", 20, Color.white, new Vector2(82f, 84f));
    }

    /* 단순 이미지 영역을 생성합니다. */
    private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, anchorMin, anchorMax);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = Color.gray;
        return image;
    }

    /* 굵은 텍스트를 생성합니다. */
    private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, string value, int fontSize, Color color, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, anchorMin, anchorMax);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    /* 버튼을 생성합니다. */
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

        CreateText("Text", obj.transform, Vector2.zero, Vector2.one, Vector2.zero, label, fontSize, Color.white, size);
        return button;
    }

    /* 승패 결과 오버레이를 생성합니다. */
    private static GameObject CreateResultPanel(Transform parent, out Text resultText, out Button retryButton, out Button homeButton)
    {
        GameObject panel = CreateUIObject("ResultPanel", parent, Vector2.zero, Vector2.one);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject box = CreatePanel("ResultBox", panel.transform, Vector2.zero, new Vector2(760f, 430f), new Color(0.095f, 0.07f, 0.045f, 0.98f));
        resultText = CreateText("ResultText", box.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 105f), "RESULT", 72, new Color(1f, 0.74f, 0.18f, 1f), new Vector2(650f, 110f));
        retryButton = CreateButton("RetryButton", box.transform, new Vector2(-180f, -70f), "\uB2E4\uC2DC\uD558\uAE30", new Vector2(300f, 90f), new Color(0.5f, 0.18f, 0.08f, 1f), 30);
        homeButton = CreateButton("HomeButton", box.transform, new Vector2(180f, -70f), "\uD648\uC73C\uB85C", new Vector2(300f, 90f), new Color(0.18f, 0.25f, 0.42f, 1f), 30);
        panel.SetActive(false);
        return panel;
    }

    /* 대기 카드 설명 팝업을 생성합니다. */
    private static GameObject CreateDeckInfoPanel(Transform parent, out Text infoText, out Button closeButton)
    {
        GameObject panel = CreateUIObject("DeckInfoPanel", parent, Vector2.zero, Vector2.one);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject box = CreatePanel("DeckInfoBox", panel.transform, Vector2.zero, new Vector2(760f, 520f), new Color(0.12f, 0.1f, 0.08f, 0.98f));
        infoText = CreateText("DeckInfoText", box.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 55f), "\uB300\uAE30 \uCE74\uB4DC \uC124\uBA85", 34, Color.white, new Vector2(660f, 330f));
        closeButton = CreateButton("DeckInfoCloseButton", box.transform, new Vector2(0f, -190f), "\uB2EB\uAE30", new Vector2(260f, 78f), new Color(0.32f, 0.18f, 0.09f, 1f), 30);
        panel.SetActive(false);
        return panel;
    }

    /* 전투 중 MENU 버튼으로 여는 옵션 팝업을 생성합니다. */
    private static GameObject CreateOptionPanel(Transform parent, out Button retryButton, out Button lobbyButton, out Button closeButton)
    {
        GameObject panel = CreateUIObject("OptionPanel", parent, Vector2.zero, Vector2.one);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject box = CreatePanel("OptionBox", panel.transform, new Vector2(0f, 0f), new Vector2(620f, 500f), new Color(0.09f, 0.065f, 0.045f, 0.98f));
        CreateText("OptionTitle", box.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 165f), "BATTLE MENU", 44, new Color(1f, 0.74f, 0.18f, 1f), new Vector2(520f, 70f));
        retryButton = CreateButton("OptionRetryButton", box.transform, new Vector2(0f, 70f), "\uC7AC\uC2DC\uC791", new Vector2(410f, 78f), new Color(0.5f, 0.18f, 0.08f, 1f), 30);
        lobbyButton = CreateButton("OptionLobbyButton", box.transform, new Vector2(0f, -30f), "\uB85C\uBE44\uB85C", new Vector2(410f, 78f), new Color(0.18f, 0.25f, 0.42f, 1f), 30);
        closeButton = CreateButton("OptionCloseButton", box.transform, new Vector2(0f, -130f), "\uB2EB\uAE30", new Vector2(410f, 78f), new Color(0.24f, 0.17f, 0.1f, 1f), 30);
        panel.SetActive(false);
        return panel;
    }

    /* 공격 순간 화면 플래시와 HIT 텍스트를 생성합니다. */
    private static void CreateImpactLayer(Transform parent, out Image flashImage, out Text impactText)
    {
        GameObject flash = CreateUIObject("ImpactFlash", parent, Vector2.zero, Vector2.one);
        flashImage = flash.AddComponent<Image>();
        flashImage.color = new Color(1f, 0.2f, 0.08f, 0.22f);
        flash.SetActive(false);

        impactText = CreateText("ImpactText", parent, AnchorCenter(), AnchorCenter(), new Vector2(0f, 0f), "HIT!", 88, new Color(1f, 0.82f, 0.18f, 1f), new Vector2(520f, 120f));
        impactText.gameObject.SetActive(false);
    }

    /* 프리팹 기반 슬롯에서도 Artwork/Text 레이아웃을 강제로 맞춥니다. */
    private static void ApplyCardSlotChildLayout(Transform slotTransform)
    {
        RectTransform artworkRect = FindChildRect(slotTransform, "Artwork");
        if (artworkRect != null)
        {
            artworkRect.anchorMin = AnchorCenter();
            artworkRect.anchorMax = AnchorCenter();
            artworkRect.pivot = new Vector2(0.5f, 0.5f);
            artworkRect.anchoredPosition = ArtworkPosition;
            artworkRect.sizeDelta = ArtworkSize;
            artworkRect.localScale = Vector3.one;

            Image artworkImage = artworkRect.GetComponent<Image>();
            if (artworkImage != null)
            {
                artworkImage.color = Color.white;
                artworkImage.preserveAspect = true;
                artworkImage.raycastTarget = false;
                artworkImage.type = Image.Type.Simple;
            }
        }

        RectTransform typeRect = FindChildRect(slotTransform, "TypeText");
        if (typeRect != null)
        {
            typeRect.anchorMin = AnchorCenter();
            typeRect.anchorMax = AnchorCenter();
            typeRect.pivot = new Vector2(0.5f, 0.5f);
            typeRect.anchoredPosition = new Vector2(0f, -97f);
            typeRect.sizeDelta = new Vector2(230f, 26f);
            typeRect.localScale = Vector3.one;
        }

        RectTransform hpRect = FindChildRect(slotTransform, "HpText");
        if (hpRect != null)
        {
            hpRect.anchorMin = AnchorCenter();
            hpRect.anchorMax = AnchorCenter();
            hpRect.pivot = new Vector2(0.5f, 0.5f);
            hpRect.anchoredPosition = new Vector2(0f, -126f);
            hpRect.sizeDelta = new Vector2(230f, 34f);
            hpRect.localScale = Vector3.one;
        }

        RectTransform abilityRect = FindChildRect(slotTransform, "AbilityText");
        if (abilityRect != null)
        {
            abilityRect.anchorMin = AnchorBottom();
            abilityRect.anchorMax = AnchorBottom();
            abilityRect.pivot = new Vector2(0.5f, 0.5f);
            abilityRect.anchoredPosition = new Vector2(0f, 16f);
            abilityRect.sizeDelta = new Vector2(232f, 34f);
            abilityRect.localScale = Vector3.one;

            Text abilityText = abilityRect.GetComponent<Text>();
            if (abilityText != null)
            {
                abilityText.fontSize = 15;
                abilityText.resizeTextForBestFit = true;
                abilityText.resizeTextMinSize = 10;
                abilityText.resizeTextMaxSize = 15;
            }
        }
    }

    /* 이름으로 하위 RectTransform을 찾습니다. */
    private static RectTransform FindChildRect(Transform parent, string childName)
    {
        RectTransform[] rects = parent.GetComponentsInChildren<RectTransform>(true);

        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i].name == childName)
                return rects[i];
        }

        return null;
    }

    private static Vector2 AnchorCenter()
    {
        return new Vector2(0.5f, 0.5f);
    }

    private static Vector2 AnchorTop()
    {
        return new Vector2(0.5f, 1f);
    }

    private static Vector2 AnchorBottom()
    {
        return new Vector2(0.5f, 0f);
    }

    /* Play Mode와 Edit Mode에 맞게 오브젝트를 제거합니다. */
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
