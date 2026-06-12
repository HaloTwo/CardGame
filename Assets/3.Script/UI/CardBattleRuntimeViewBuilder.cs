using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class CardBattleRuntimeViewBuilder
{
    private const float CardWidth = 270f; // 카드 UI 가로 크기
    private const float CardHeight = 350f; // 카드 UI 세로 크기
    private const float CardGap = 330f; // 카드 슬롯 사이 간격

    // 전투 UI를 항상 새로 만든다. 기존 임시 UI는 비활성화해서 깨진 참조 재사용을 막는다.
    public static CardBattleView CreateOrRepair()
    {
        EnsureInputSystemEventSystem();
        DisableOldBattleViews();

        Canvas canvas = CreateCanvas(); // 카드 전투 전용 Canvas
        GameObject root = CreateUIObject("CardBattleView_Runtime", canvas.transform, Vector2.zero, Vector2.one); // 전투 UI 루트

        Image background = root.AddComponent<Image>(); // 전체 배경
        background.color = new Color(0.075f, 0.08f, 0.095f, 1f);

        CreateFieldPanel(root.transform, new Vector2(0f, 520f), "ENEMY FIELD"); // 적 전장 영역
        CreateFieldPanel(root.transform, new Vector2(0f, -360f), "PLAYER FIELD"); // 플레이어 전장 영역
        CreateDeckBack(root.transform, new Vector2(475f, 705f), "대기\n카드"); // 적 대기 카드 더미
        CreateDeckBack(root.transform, new Vector2(475f, -175f), "대기\n카드"); // 플레이어 대기 카드 더미

        Text enemyDeck = CreateText("EnemyDeckText", root.transform, AnchorCenter(), AnchorCenter(), new Vector2(350f, 705f), "대기 카드\n3장", 26, Color.white, new Vector2(170f, 82f)); // 적 덱 수
        Text turn = CreateText("TurnText", root.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 118f), "PLAYER TURN", 48, Color.white, new Vector2(620f, 76f)); // 현재 턴
        Text info = CreateText("InfoText", root.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 62f), "사용할 아군 카드를 선택하세요.", 34, Color.white, new Vector2(860f, 72f)); // 안내 문구
        Text playerDeck = CreateText("PlayerDeckText", root.transform, AnchorCenter(), AnchorCenter(), new Vector2(350f, -175f), "대기 카드\n3장", 26, Color.white, new Vector2(170f, 82f)); // 플레이어 덱 수

        CardSlotView[] enemySlots = CreateSlots(root.transform, "EnemySlot", 520f); // 적 카드 3장
        CardSlotView[] playerSlots = CreateSlots(root.transform, "PlayerSlot", -360f); // 플레이어 카드 3장

        Button attack = CreateButton("AttackButton", root.transform, new Vector2(-170f, -735f), "기본공격", new Vector2(310f, 84f)); // 공격 버튼
        Button skill = CreateButton("SkillButton", root.transform, new Vector2(170f, -735f), "카드효과", new Vector2(310f, 84f)); // 카드효과 버튼
        Button restart = CreateButton("RestartButton", root.transform, new Vector2(-430f, 835f), "RESTART", new Vector2(190f, 58f)); // 재시작 버튼
        GameObject resultPanel = CreateResultPanel(root.transform, out Text resultText, out Button retryButton, out Button homeButton); // 승리/패배 결과 패널

        CardBattleView view = root.AddComponent<CardBattleView>(); // 매니저가 제어할 전투 UI
        view.SetupReferences(turn, info, playerDeck, enemyDeck, attack, skill, restart, retryButton, homeButton, resultPanel, resultText, playerSlots, enemySlots);
        Debug.Log($"CardBattleRuntimeViewBuilder: UI 생성 완료 / PlayerSlots={playerSlots.Length}, EnemySlots={enemySlots.Length}");
        return view;
    }

    // 이전에 생성된 전투 View를 모두 비활성화한다.
    // 이전에 생성된 전투 View를 제거해서 씬에 중복 Canvas가 쌓이지 않게 한다.
    private static void DisableOldBattleViews()
    {
        CardBattleView[] existingViews = Object.FindObjectsByType<CardBattleView>(FindObjectsSortMode.None); // 기존 전투 UI 목록
        for (int i = 0; i < existingViews.Length; i++)
            DestroyObject(existingViews[i].gameObject);
    }

    // Input System 전용 UI 입력 모듈을 보장한다.
    private static void EnsureInputSystemEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(); // UI 이벤트 시스템
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>(); // 구 입력 모듈
        if (oldModule != null)
            DestroyObject(oldModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    // 세로형 화면 기준 Canvas를 생성한다.
    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("CardBattleCanvas"); // 카드 전투 전용 Canvas 오브젝트
        Canvas canvas = canvasObject.AddComponent<Canvas>(); // 화면 출력 Canvas
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); // 해상도 대응 스케일러
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // 지정한 Y 위치에 카드 슬롯 3개를 만든다.
    private static CardSlotView[] CreateSlots(Transform parent, string prefix, float y)
    {
        CardSlotView[] slots = new CardSlotView[3]; // 생성된 슬롯 배열

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObject = CreateUIObject($"{prefix}{i + 1}", parent, AnchorCenter(), AnchorCenter()); // 카드 슬롯 오브젝트
            RectTransform rect = slotObject.GetComponent<RectTransform>(); // 카드 슬롯 위치와 크기
            rect.anchoredPosition = new Vector2((i - 1) * CardGap, y);
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);

            Image background = slotObject.AddComponent<Image>(); // 카드 기본 배경
            background.color = new Color(0.96f, 0.93f, 0.86f, 1f);

            Button button = slotObject.AddComponent<Button>(); // 카드 선택 버튼
            button.targetGraphic = background;

            Text name = CreateText("NameText", slotObject.transform, AnchorTop(), AnchorTop(), new Vector2(0f, -30f), "CARD", 30, Color.black, new Vector2(230f, 46f)); // 카드 이름
            Text type = CreateText("TypeText", slotObject.transform, AnchorTop(), AnchorTop(), new Vector2(0f, -76f), "TYPE", 23, new Color(0.2f, 0.2f, 0.2f, 1f), new Vector2(230f, 38f)); // 카드 타입
            Image artwork = CreateImage("Artwork", slotObject.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 28f), new Vector2(202f, 124f)); // 카드 일러스트
            Text hp = CreateText("HpText", slotObject.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, -60f), "HP", 30, Color.black, new Vector2(230f, 44f)); // 카드 HP
            Text ability = CreateText("AbilityText", slotObject.transform, AnchorBottom(), AnchorBottom(), new Vector2(0f, 44f), "능력", 19, Color.black, new Vector2(232f, 86f)); // 카드 능력 설명
            ability.resizeTextForBestFit = true;
            ability.resizeTextMinSize = 13;
            ability.resizeTextMaxSize = 19;

            CardSlotView slot = slotObject.AddComponent<CardSlotView>(); // 카드 슬롯 제어 스크립트
            slot.SetupReferences(button, background, artwork, name, type, hp, ability);
            slots[i] = slot;
        }

        return slots;
    }

    // 공통 UI 오브젝트를 생성한다.
    private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name); // 생성할 UI 오브젝트
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>(); // UI 위치와 크기 정보
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    // 카드가 놓이는 전장 영역 배경을 생성한다.
    private static void CreateFieldPanel(Transform parent, Vector2 position, string label)
    {
        GameObject panel = CreateUIObject(label, parent, AnchorCenter(), AnchorCenter()); // 전장 배경 오브젝트
        RectTransform rect = panel.GetComponent<RectTransform>(); // 전장 배경 위치와 크기
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1030f, 410f);

        Image image = panel.AddComponent<Image>(); // 전장 배경 이미지
        image.color = new Color(0.13f, 0.14f, 0.16f, 0.75f);
    }

    // 아직 공개되지 않은 대기 카드를 뒤집힌 카드 더미처럼 표시한다.
    private static void CreateDeckBack(Transform parent, Vector2 position, string label)
    {
        GameObject deck = CreateUIObject("DeckBack", parent, AnchorCenter(), AnchorCenter()); // 대기 카드 더미 오브젝트
        RectTransform rect = deck.GetComponent<RectTransform>(); // 대기 카드 위치와 크기
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(88f, 132f);

        Image image = deck.AddComponent<Image>(); // 카드 뒷면 이미지
        image.color = new Color(0.16f, 0.22f, 0.34f, 1f);

        CreateText("DeckBackText", deck.transform, Vector2.zero, Vector2.one, Vector2.zero, label, 18, Color.white, new Vector2(88f, 132f));
    }

    // 지정한 위치와 크기의 Image를 생성한다.
    private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, anchorMin, anchorMax); // 이미지 오브젝트
        RectTransform rect = obj.GetComponent<RectTransform>(); // 이미지 위치와 크기
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>(); // 이미지 표시 컴포넌트
        image.color = Color.gray;
        return image;
    }

    // 지정한 위치와 크기의 Text를 생성한다.
    private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, string value, int fontSize, Color color, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, anchorMin, anchorMax); // 텍스트 오브젝트
        RectTransform rect = obj.GetComponent<RectTransform>(); // 텍스트 위치와 크기
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>(); // 텍스트 표시 컴포넌트
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

    // 버튼 배경과 라벨을 생성한다.
    private static Button CreateButton(string name, Transform parent, Vector2 position, string label, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, AnchorCenter(), AnchorCenter()); // 버튼 오브젝트
        RectTransform rect = obj.GetComponent<RectTransform>(); // 버튼 위치와 크기
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>(); // 버튼 배경
        image.color = new Color(0.18f, 0.16f, 0.12f, 1f);

        Button button = obj.AddComponent<Button>(); // 클릭 버튼
        button.targetGraphic = image;

        CreateText("Text", obj.transform, Vector2.zero, Vector2.one, Vector2.zero, label, 32, Color.white, size); // 버튼 라벨
        return button;
    }

    // 전투 종료 시 표시할 결과 패널을 생성한다.
    // 전투 종료 시 표시할 결과 패널과 다시하기/홈 버튼을 생성한다.
    private static GameObject CreateResultPanel(Transform parent, out Text resultText, out Button retryButton, out Button homeButton)
    {
        GameObject panel = CreateUIObject("ResultPanel", parent, Vector2.zero, Vector2.one); // 결과 화면 루트
        Image image = panel.AddComponent<Image>(); // 반투명 배경
        image.color = new Color(0f, 0f, 0f, 0.78f);

        resultText = CreateText("ResultText", panel.transform, AnchorCenter(), AnchorCenter(), new Vector2(0f, 150f), "RESULT", 72, Color.white, new Vector2(600f, 110f));
        retryButton = CreateButton("RetryButton", panel.transform, new Vector2(-180f, -40f), "다시하기", new Vector2(300f, 90f));
        homeButton = CreateButton("HomeButton", panel.transform, new Vector2(180f, -40f), "홈으로", new Vector2(300f, 90f));
        panel.SetActive(false);
        return panel;
    }

    // 중앙 고정 앵커를 반환한다.
    private static Vector2 AnchorCenter()
    {
        return new Vector2(0.5f, 0.5f);
    }

    // 상단 중앙 고정 앵커를 반환한다.
    private static Vector2 AnchorTop()
    {
        return new Vector2(0.5f, 1f);
    }

    // 하단 중앙 고정 앵커를 반환한다.
    private static Vector2 AnchorBottom()
    {
        return new Vector2(0.5f, 0f);
    }

    // Play Mode와 Edit Mode에 맞는 방식으로 오브젝트를 제거한다.
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
