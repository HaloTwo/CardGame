using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
        cards.Add(CreateOrLoadCard("Burger_Normal", "Burger", BattleCardType.Normal, 6, "현재 HP만큼 피해, 대상 현재 HP만큼 반격", new Color(1f, 0.86f, 0.45f, 1f)));
        cards.Add(CreateOrLoadCard("Fries_Ranged", "Fries", BattleCardType.Ranged, 4, "현재 HP만큼 피해, 반격 없음", new Color(0.45f, 0.75f, 1f, 1f)));
        cards.Add(CreateOrLoadCard("Monster_Musou", "Monster", BattleCardType.Musou, 7, "대상 100% + 인접 랜덤 1장 50%", new Color(1f, 0.45f, 0.35f, 1f)));
        cards.Add(CreateOrLoadCard("Soda_Healer", "Soda", BattleCardType.Healer, 5, "턴 시작 시 자신 제외 아군 HP 1 회복", new Color(0.45f, 1f, 0.55f, 1f)));
        cards.Add(CreateOrLoadCard("Cheese_Normal", "Cheese", BattleCardType.Normal, 5, "현재 HP만큼 피해, 대상 현재 HP만큼 반격", new Color(1f, 0.86f, 0.45f, 1f)));
        cards.Add(CreateOrLoadCard("BombSauce_Bomber", "Bomb Sauce", BattleCardType.Bomber, 4, "카드효과: 대상 현재 HP 피해 + 나머지 적 1 피해", new Color(1f, 0.55f, 0.12f, 1f)));

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
