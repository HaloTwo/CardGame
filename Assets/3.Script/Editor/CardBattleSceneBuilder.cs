using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class CardBattleSceneBuilder
{
    private const string CardDefinitionFolder = "Assets/9.SO/CardBattle"; // 카드 정의 SO를 저장할 폴더

    // 스크립트 리로드 후 열린 씬에 남아 있는 구 입력 모듈을 자동 정리한다.
    static CardBattleSceneBuilder()
    {
        EditorApplication.delayCall += RepairExistingEventSystemIfNeeded;
    }

    [MenuItem("Tools/Card Battle/Build Prototype Scene")]
    // 프로토타입 전투 화면, 전투 매니저, 기본 카드 데이터를 한 번에 구성한다.
    public static void BuildPrototypeScene()
    {
        EnsureInputSystemEventSystem();

        CardBattleView view = CardBattleRuntimeViewBuilder.CreateOrRepair(); // 플레이 가능한 전투 UI
        CardBattleManager manager = Object.FindFirstObjectByType<CardBattleManager>(); // 전투 흐름 관리자
        if (manager == null)
        {
            GameObject managerObject = new GameObject("CardBattleManager");
            manager = managerObject.AddComponent<CardBattleManager>();
        }

        BattleCardDefinition[] defaultCards = CreateOrLoadDefaultCardDefinitions(); // 인스펙터에서 이미지 교체할 카드 데이터
        manager.SetupDeckDefinitions(defaultCards, defaultCards);

        Selection.activeObject = manager.gameObject;
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Card Battle prototype scene build complete. View Ready: {view != null && view.IsReady()}");
    }

    // 현재 씬에 이미 있는 EventSystem만 검사해서 StandaloneInputModule을 InputSystemUIInputModule로 교체한다.
    private static void RepairExistingEventSystemIfNeeded()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(); // 현재 씬의 UI 이벤트 처리기
        if (eventSystem == null)
            return;

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>(); // 제거할 구 입력 모듈
        if (oldModule == null)
            return;

        EnsureInputSystemEventSystem();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("EventSystem input module changed to InputSystemUIInputModule.");
    }

    // Input System 전용 프로젝트에서 구 Input Manager 모듈이 남아 오류를 내지 않도록 교체한다.
    private static void EnsureInputSystemEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(); // UI 클릭 이벤트 처리기
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>(); // 구 Input Manager 전용 모듈
        if (oldModule != null)
            Object.DestroyImmediate(oldModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    // 기본 카드 SO가 없으면 생성하고, 있으면 로드해서 반환한다.
    private static BattleCardDefinition[] CreateOrLoadDefaultCardDefinitions()
    {
        EnsureFolder(CardDefinitionFolder);

        List<BattleCardDefinition> cards = new(); // 매니저에 연결할 카드 정의 목록
        cards.Add(CreateOrLoadCard("Burger_Normal", "버거", BattleCardType.Normal, 6, "현재 HP만큼 피해, 대상 현재 HP만큼 반격", new Color(1f, 0.86f, 0.45f, 1f)));
        cards.Add(CreateOrLoadCard("Fries_Ranged", "감자튀김", BattleCardType.Ranged, 4, "현재 HP만큼 피해, 반격 없음", new Color(0.45f, 0.75f, 1f, 1f)));
        cards.Add(CreateOrLoadCard("Monster_Musou", "몬스터", BattleCardType.Musou, 7, "대상 100% + 인접 랜덤 1장 50%", new Color(1f, 0.45f, 0.35f, 1f)));
        cards.Add(CreateOrLoadCard("Soda_Healer", "소다", BattleCardType.Healer, 5, "턴 시작 시 자신 제외 아군 HP 1 회복", new Color(0.45f, 1f, 0.55f, 1f)));
        cards.Add(CreateOrLoadCard("BombSauce_Bomber", "폭탄 소스", BattleCardType.Bomber, 4, "카드효과: 대상 현재 HP 피해 + 나머지 적 1 피해", new Color(1f, 0.55f, 0.12f, 1f)));
        cards.Add(CreateOrLoadCard("VampireShake_Vampire", "흡혈 쉐이크", BattleCardType.Vampire, 5, "카드효과: 대상에게 현재 HP 피해, 자신 HP 2 회복", new Color(0.78f, 0.22f, 0.82f, 1f)));

        CreateOrLoadCard("Cheese_Normal", "치즈", BattleCardType.Normal, 5, "현재 HP만큼 피해, 대상 현재 HP만큼 반격", new Color(1f, 0.86f, 0.45f, 1f));
        CreateOrLoadCard("SpicyBerserker_Berserker", "매운 광전사", BattleCardType.Berserker, 6, "카드효과: 잃은 HP만큼 추가 피해, 자신 1 피해", new Color(0.95f, 0.16f, 0.16f, 1f));
        CreateOrLoadCard("GuardBurger_Guardian", "수호 버거", BattleCardType.Guardian, 8, "카드효과: 절반 피해를 주고 자신 HP 1 회복", new Color(0.36f, 0.62f, 1f, 1f));
        CreateOrLoadCard("Skewer_Piercing", "꼬치", BattleCardType.Piercing, 4, "카드효과: 대상 피해 + 양옆 적 1 피해", new Color(0.82f, 0.82f, 0.92f, 1f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return cards.ToArray();
    }

    // 지정한 카드 정의 SO를 생성하거나 기존 에셋을 로드한다.
    private static BattleCardDefinition CreateOrLoadCard(string assetName, string cardName, BattleCardType cardType, int maxHp, string abilityText, Color color)
    {
        string path = $"{CardDefinitionFolder}/{assetName}.asset"; // 카드 정의 에셋 경로
        BattleCardDefinition card = AssetDatabase.LoadAssetAtPath<BattleCardDefinition>(path);
        if (card != null)
            return card;

        card = ScriptableObject.CreateInstance<BattleCardDefinition>();
        SerializedObject serializedCard = new SerializedObject(card); // private SerializeField 값 설정용 객체
        serializedCard.FindProperty("cardName").stringValue = cardName;
        serializedCard.FindProperty("cardType").enumValueIndex = (int)cardType;
        serializedCard.FindProperty("maxHp").intValue = maxHp;
        serializedCard.FindProperty("abilityText").stringValue = abilityText;
        serializedCard.FindProperty("cardColor").colorValue = color;
        serializedCard.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(card, path);
        return card;
    }

    // Unity 에셋 폴더가 없으면 단계별로 생성한다.
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/'); // Assets 기준 하위 폴더 이름 목록
        string current = parts[0]; // 현재까지 생성된 경로
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}"; // 다음에 확인할 경로
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}

[InitializeOnLoad]
public static class StartSceneBuilder
{
    private const string StartScenePath = "Assets/1.Scene/StartScene.unity"; // 로비 UI를 고정 저장할 시작 씬 경로
    private const string AutoBuildVersion = "2026-06-19-StartSceneFixedLobby-v2"; // 자동 저장 중복 실행 방지 키

    // Unity가 이미 열린 상태에서도 리컴파일 후 시작 씬 로비 UI를 실제 씬 파일에 저장합니다.
    static StartSceneBuilder()
    {
        Debug.Log("StartSceneBuilder initialized. StartScene fixed lobby build scheduled.");
        EditorApplication.delayCall += BuildAndSaveStartSceneLayoutOnce;
    }

    [MenuItem("Tools/Card Battle/Build Start Scene Layout")]
    // 시작 씬 로비 UI를 하이어라키에 꺼내 두어 인스펙터에서 직접 위치를 수정할 수 있게 합니다.
    public static void BuildStartSceneLayout()
    {
        EnsureStartSceneController();
        LobbyViewReferences view = StartSceneRuntimeViewBuilder.CreateStartView(); // 씬에 있으면 재사용하고, 없으면 생성합니다.
        if (view?.Panel != null)
            view.Panel.SetActive(false);

        GameObject root = GameObject.Find("LobbyView"); // 수정 대상 로비 루트
        if (root != null)
            Selection.activeObject = root;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Start scene lobby layout is ready. Move LobbyView children in the Hierarchy to adjust UI.");
    }

    // 배치모드나 메뉴에서 StartScene.unity 자체를 열고 로비 UI를 저장합니다.
    public static void BuildAndSaveStartSceneLayout()
    {
        Scene previousActiveScene = SceneManager.GetActiveScene(); // 사용자가 보고 있던 씬
        Scene startScene = FindLoadedScene(StartScenePath);
        bool openedAdditive = false; // 작업 후 닫아야 하는지 판단하는 플래그

        if (!startScene.IsValid())
        {
            startScene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Additive);
            openedAdditive = true;
        }

        SceneManager.SetActiveScene(startScene);
        BuildStartSceneLayout();
        EditorSceneManager.SaveScene(startScene);
        AssetDatabase.SaveAssets();

        if (previousActiveScene.IsValid())
            SceneManager.SetActiveScene(previousActiveScene);

        if (openedAdditive)
            EditorSceneManager.CloseScene(startScene, true);

        Debug.Log("StartScene.unity saved with fixed LobbyView hierarchy.");
    }

    // 열린 Unity에서 한 번만 자동 저장을 시도합니다.
    private static void BuildAndSaveStartSceneLayoutOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += BuildAndSaveStartSceneLayoutOnce;
            return;
        }

        if (EditorPrefs.GetString("CardBattle.StartScene.AutoBuildVersion", string.Empty) == AutoBuildVersion)
            return;

        BuildAndSaveStartSceneLayout();
        EditorPrefs.SetString("CardBattle.StartScene.AutoBuildVersion", AutoBuildVersion);
    }

    // 이미 열려 있는 씬 중 경로가 같은 씬을 찾습니다.
    private static Scene FindLoadedScene(string scenePath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path == scenePath)
                return scene;
        }

        return default;
    }

    // 시작 씬에 컨트롤러가 없으면 생성해 로비 버튼 로직이 동작하도록 합니다.
    private static void EnsureStartSceneController()
    {
        if (Object.FindFirstObjectByType<StartSceneController>() != null)
            return;

        GameObject controllerObject = new GameObject("StartSceneController");
        controllerObject.AddComponent<StartSceneController>();
    }
}
