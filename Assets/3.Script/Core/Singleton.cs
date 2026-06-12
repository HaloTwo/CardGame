using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; } // 현재 씬에서 살아있는 싱글톤 인스턴스

    // 중복 인스턴스를 제거하고 최초 인스턴스를 전역 접근 대상으로 등록한다.
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = (T)(Component)this;
    }

    // 등록된 인스턴스가 제거될 때 전역 참조를 비운다.
    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
