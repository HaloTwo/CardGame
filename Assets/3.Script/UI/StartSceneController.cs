using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneController : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "SampleScene"; // 시작 버튼으로 이동할 전투 씬 이름

    private bool canStartBattle; // 시작 버튼 입력을 받을 수 있는지 여부
    private Button startButton; // 시작 화면의 전투 시작 버튼

    // 시작 씬 진입 시 타이틀 UI를 만들고 버튼 이벤트를 연결한다.
    // 시작 씬 진입 시 타이틀 UI를 만들고 버튼 이벤트를 연결한다.
    private void Start()
    {
        canStartBattle = false;
        startButton = StartSceneRuntimeViewBuilder.CreateStartView();
        if (startButton != null)
        {
            startButton.interactable = false;
            startButton.onClick.AddListener(LoadBattleScene);
            StartCoroutine(CoEnableStartButton());
        }

        Debug.Log("StartSceneController: 시작 화면 생성 완료");
    }

    // 오브젝트 제거 시 버튼 이벤트를 해제한다.
    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(LoadBattleScene);
    }

    // 전투 씬으로 이동한다.
    // 전투 씬으로 이동한다.
    private void LoadBattleScene()
    {
        if (!canStartBattle)
            return;

        SceneManager.LoadScene(battleSceneName);
    }


    // 씬 전환 직후 남아 있는 입력이 시작 버튼에 바로 먹지 않도록 잠깐 늦게 활성화한다.
    private IEnumerator CoEnableStartButton()
    {
        yield return new WaitForSeconds(0.35f);

        canStartBattle = true;
        if (startButton != null)
            startButton.interactable = true;
    }
}
