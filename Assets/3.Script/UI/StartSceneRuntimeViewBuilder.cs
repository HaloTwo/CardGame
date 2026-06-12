using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class StartSceneRuntimeViewBuilder
{
    // 시작 씬에서 사용할 타이틀 UI를 런타임에 구성한다.
    public static Button CreateStartView()
    {
        EnsureInputSystemEventSystem();

        Canvas canvas = CreateCanvas(); // 시작 화면 전용 Canvas
        GameObject root = CreateUIObject("StartSceneView", canvas.transform, Vector2.zero, Vector2.one); // 시작 화면 루트

        Image background = root.AddComponent<Image>(); // 전체 배경
        background.color = new Color(0.055f, 0.045f, 0.035f, 1f);

        CreateText("TitleText", root.transform, new Vector2(0f, 330f), "BURGER MONSTER", 82, new Color(1f, 0.72f, 0.18f, 1f), new Vector2(900f, 120f));
        CreateText("SubTitleText", root.transform, new Vector2(0f, 230f), "CARD BATTLE", 48, Color.white, new Vector2(720f, 80f));
        CreateText("RuleText", root.transform, new Vector2(0f, 60f), "3장의 공개 카드와 뒤집힌 대기 카드로 싸우는\n세로형 턴제 카드 배틀", 32, new Color(0.92f, 0.9f, 0.82f, 1f), new Vector2(840f, 120f));
        CreateText("FeatureText", root.transform, new Vector2(0f, -85f), "폭탄 / 무쌍 / 원거리 / 힐러 카드 효과", 30, new Color(1f, 0.58f, 0.22f, 1f), new Vector2(800f, 70f));

        return CreateButton("StartButton", root.transform, new Vector2(0f, -270f), "START BATTLE", new Vector2(440f, 96f));
    }

    // Input System 전용 UI 이벤트 시스템을 보장한다.
    private static void EnsureInputSystemEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(); // 현재 씬 UI 이벤트 시스템
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>(); // 구 입력 모듈
        if (oldModule != null)
            Object.Destroy(oldModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    // 세로형 화면 기준 Canvas를 생성한다.
    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("StartSceneCanvas"); // 시작 화면 Canvas 오브젝트
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

    // 중앙 고정 텍스트를 생성한다.
    private static Text CreateText(string name, Transform parent, Vector2 position, string value, int fontSize, Color color, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, AnchorCenter(), AnchorCenter()); // 텍스트 오브젝트
        RectTransform rect = obj.GetComponent<RectTransform>(); // 텍스트 위치와 크기
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>(); // 텍스트 표시 컴포넌트
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

    // 중앙 고정 버튼을 생성한다.
    private static Button CreateButton(string name, Transform parent, Vector2 position, string label, Vector2 size)
    {
        GameObject obj = CreateUIObject(name, parent, AnchorCenter(), AnchorCenter()); // 버튼 오브젝트
        RectTransform rect = obj.GetComponent<RectTransform>(); // 버튼 위치와 크기
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>(); // 버튼 배경
        image.color = new Color(0.18f, 0.13f, 0.08f, 1f);

        Button button = obj.AddComponent<Button>(); // 클릭 버튼
        button.targetGraphic = image;

        CreateText("Text", obj.transform, Vector2.zero, label, 34, Color.white, size);
        return button;
    }

    // 중앙 앵커를 반환한다.
    private static Vector2 AnchorCenter()
    {
        return new Vector2(0.5f, 0.5f);
    }
}
