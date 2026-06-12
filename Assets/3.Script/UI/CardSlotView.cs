using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardSlotView : MonoBehaviour
{
    [SerializeField] private Button button; // 카드 클릭 입력 버튼
    [SerializeField] private Image background; // 선택 강조와 기본 배경 이미지
    [SerializeField] private Image artworkImage; // 카드 일러스트 표시 이미지
    [SerializeField] private Text nameText; // 카드 이름 표시 텍스트
    [SerializeField] private Text typeText; // 카드 타입 표시 텍스트
    [SerializeField] private Text hpText; // 카드 HP 표시 텍스트
    [SerializeField] private Text abilityText; // 카드 능력 설명 텍스트

    private BattleCardRuntime boundCard; // 현재 슬롯에 연결된 카드
    private Action<CardSlotView> clicked; // 슬롯 클릭 시 매니저로 전달할 콜백
    private Coroutine scaleRoutine; // 선택/공격 연출 코루틴
        private Coroutine moveRoutine; // 공격 전진과 피격 흔들림 이동 코루틴
private Coroutine damageTextRoutine; // 데미지 숫자 표시 코루틴
        private Vector3 baseLocalPosition; // 기본 카드 위치
private Vector3 baseScale = Vector3.one; // 기본 카드 크기

    public BattleCardRuntime BoundCard => boundCard; // 외부에서 읽는 현재 카드 참조

    // 버튼 참조를 보정하고 클릭 이벤트를 연결한다.
    private void Awake()
    {
                baseLocalPosition = transform.localPosition;
baseScale = transform.localScale;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    // 슬롯 제거 시 버튼 이벤트와 카드 변경 이벤트를 해제한다.
    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);

        UnbindEvent();
    }

    // 슬롯에 카드 데이터를 연결하고 UI를 갱신한다.
    public void Bind(BattleCardRuntime card, Action<CardSlotView> onClicked)
    {
        UnbindEvent();

        boundCard = card;
        clicked = onClicked;

        if (boundCard != null)
            boundCard.OnChanged += HandleCardChanged;

        Refresh();
    }

    // 선택된 카드 슬롯을 노란색으로 강조한다.
    public void SetSelected(bool selected)
    {
        if (background == null)
            return;

        background.color = selected ? new Color(1f, 0.84f, 0.25f, 1f) : new Color(0.96f, 0.93f, 0.86f, 1f);
        transform.localScale = selected ? baseScale * 1.08f : baseScale;
    }

    // 공격 카드가 앞으로 튀어나오는 느낌을 주는 간단한 선택 연출이다.
    // 공격 카드가 살짝 앞으로 튀어나왔다 돌아오게 해서 행동 주체를 보여준다.
    public void PlayFocus()
    {
        PlayScale(baseScale * 1.18f, 0.16f);
        PlayMove(new Vector3(0f, boundCard != null && boundCard.Owner == CardOwner.Player ? 26f : -26f, 0f), 0.16f);
    }

    // 피해를 받는 카드가 짧게 흔들리는 느낌의 피격 연출이다.
    // 피해를 받는 카드를 붉게 점멸시키고 좌우로 흔들어 피격을 명확하게 보여준다.
    // 피해를 받는 카드를 붉게 점멸시키고 좌우로 흔들어 피격을 명확하게 보여준다.
    public void PlayHit()
    {
        PlayScale(baseScale * 0.9f, 0.12f);
        PlayShake(18f, 0.28f);
        StartCoroutine(CoHitFlash());
        StartCoroutine(CoSlashEffect());
    }

    // 카드 위에 데미지 숫자를 잠깐 띄운다.
    public void ShowDamageText(int damage)
    {
        if (damage <= 0)
            return;

        if (damageTextRoutine != null)
            StopCoroutine(damageTextRoutine);

        damageTextRoutine = StartCoroutine(CoDamageText(damage));
    }

    // 연결된 카드 상태를 기준으로 이름, 타입, HP, 버튼 활성화를 갱신한다.
    public void Refresh()
    {
        bool hasCard = boundCard != null && !boundCard.IsDead; // 슬롯에 살아있는 카드가 있는지 여부

        if (button != null)
            button.interactable = hasCard;

        if (nameText != null)
            nameText.text = hasCard ? boundCard.Data.CardName : "EMPTY";

        if (typeText != null)
            typeText.text = hasCard ? GetCardTypeName(boundCard.Data.CardType) : string.Empty;

        if (hpText != null)
            hpText.text = hasCard ? $"HP {boundCard.CurrentHp}/{boundCard.Data.MaxHp}" : string.Empty;

        if (abilityText != null)
            abilityText.text = hasCard ? boundCard.Data.AbilityText : string.Empty;

        if (artworkImage != null)
        {
            artworkImage.enabled = hasCard;
            artworkImage.sprite = hasCard ? boundCard.Data.CardSprite : null;
            artworkImage.color = hasCard ? boundCard.Data.CardColor : Color.clear;
            artworkImage.preserveAspect = true;
        }

        SetSelected(false);
    }

    // 에디터 씬 빌더에서 생성한 UI 컴포넌트 참조를 연결한다.
    public void SetupReferences(Button button, Image background, Image artworkImage, Text nameText, Text typeText, Text hpText, Text abilityText)
    {
        this.button = button;
        this.background = background;
        this.artworkImage = artworkImage;
        this.nameText = nameText;
        this.typeText = typeText;
        this.hpText = hpText;
        this.abilityText = abilityText;
    }

    // 버튼 클릭을 상위 전투 매니저 콜백으로 전달한다.
    private void HandleClick()
    {
        clicked?.Invoke(this);
    }

    // 카드 HP가 바뀌면 슬롯 UI를 다시 그린다.
    private void HandleCardChanged(BattleCardRuntime card)
    {
        Refresh();
    }

    // 기존 카드에 연결된 변경 이벤트를 해제해 중복 갱신을 막는다.
    private void UnbindEvent()
    {
        if (boundCard != null)
            boundCard.OnChanged -= HandleCardChanged;
    }

    // 지정한 크기로 갔다가 기본 크기로 돌아오는 스케일 연출을 실행한다.
    private void PlayScale(Vector3 targetScale, float halfDuration)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(CoScale(targetScale, halfDuration));
    }

    // 카드 크기를 부드럽게 왕복시킨다.
    private IEnumerator CoScale(Vector3 targetScale, float halfDuration)
    {
        Vector3 startScale = transform.localScale; // 연출 시작 시점 크기
        float time = 0f; // 보간 경과 시간

        while (time < halfDuration)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, time / halfDuration);
            yield return null;
        }

        time = 0f;
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, baseScale, time / halfDuration);
            yield return null;
        }

        transform.localScale = baseScale;
        scaleRoutine = null;
    }

    // 피격 시 카드 배경을 붉게 점멸시킨다.
    // 피격 시 카드 배경을 붉게 두 번 점멸시킨다.
    private IEnumerator CoHitFlash()
    {
        if (background == null)
            yield break;

        Color origin = background.color; // 피격 전 배경색

        for (int i = 0; i < 2; i++)
        {
            background.color = new Color(1f, 0.18f, 0.12f, 1f);
            yield return new WaitForSeconds(0.08f);
            background.color = origin;
            yield return new WaitForSeconds(0.05f);
        }
    }

    // 데미지 텍스트를 생성해 위로 이동시키며 사라지게 한다.
    private IEnumerator CoDamageText(int damage)
    {
        GameObject textObject = new GameObject("DamageText"); // 데미지 숫자 오브젝트
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.AddComponent<RectTransform>(); // 데미지 숫자 위치
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 18f);
        rect.sizeDelta = new Vector2(180f, 70f);

        Text text = textObject.AddComponent<Text>(); // 데미지 숫자 텍스트
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = $"-{damage}";
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.18f, 0.12f, 1f);

        float duration = 0.65f; // 데미지 숫자 표시 시간
        float time = 0f; // 경과 시간
        Vector2 start = rect.anchoredPosition; // 시작 위치
        Vector2 end = start + new Vector2(0f, 70f); // 종료 위치

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration; // 보간 비율
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1f - t);
            yield return null;
        }

        Destroy(textObject);
        damageTextRoutine = null;
    }


    // 카드 위치를 부드럽게 왕복시킨다.
    private IEnumerator CoMove(Vector3 targetPosition, float halfDuration)
    {
        Vector3 startPosition = transform.localPosition; // 연출 시작 시점 위치
        float time = 0f; // 보간 경과 시간

        while (time < halfDuration)
        {
            time += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, time / halfDuration);
            yield return null;
        }

        time = 0f;
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(targetPosition, baseLocalPosition, time / halfDuration);
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        moveRoutine = null;
    }

    // 짧은 시간 동안 좌우 흔들림을 반복해 피격감을 만든다.
    private IEnumerator CoShake(float power, float duration)
    {
        float time = 0f; // 흔들림 경과 시간

        while (time < duration)
        {
            time += Time.deltaTime;
            float direction = Mathf.Sin(time * 70f); // 좌우 왕복 방향
            transform.localPosition = baseLocalPosition + new Vector3(direction * power, 0f, 0f);
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        moveRoutine = null;
    }


    // 지정한 위치 오프셋으로 갔다가 기본 위치로 돌아오는 이동 연출을 실행한다.
    private void PlayMove(Vector3 offset, float halfDuration)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(CoMove(baseLocalPosition + offset, halfDuration));
    }

    // 피격 카드가 좌우로 흔들린 뒤 원래 위치로 돌아오게 한다.
    private void PlayShake(float power, float duration)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(CoShake(power, duration));
    }


    // 카드 위를 가로지르는 짧은 사선 이펙트로 타격 순간을 표시한다.
    private IEnumerator CoSlashEffect()
    {
        GameObject slashObject = new GameObject("HitSlash"); // 타격 사선 오브젝트
        slashObject.transform.SetParent(transform, false);

        RectTransform rect = slashObject.AddComponent<RectTransform>(); // 타격 사선 위치
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(230f, 18f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -18f);

        Image image = slashObject.AddComponent<Image>(); // 타격 사선 이미지
        image.color = new Color(1f, 0.82f, 0.18f, 0.95f);

        float duration = 0.22f; // 타격 사선 유지 시간
        float time = 0f; // 경과 시간
        Vector3 startScale = new Vector3(0.35f, 1f, 1f); // 시작 크기
        Vector3 endScale = new Vector3(1.15f, 1f, 1f); // 종료 크기

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration; // 보간 비율
            rect.localScale = Vector3.Lerp(startScale, endScale, t);
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f - t);
            yield return null;
        }

        Destroy(slashObject);
    }


    // 카드 타입 enum을 플레이어가 바로 이해할 수 있는 한글 표시명으로 바꾼다.
    private string GetCardTypeName(BattleCardType cardType)
    {
        return cardType switch
        {
            BattleCardType.Normal => "일반",
            BattleCardType.Ranged => "원거리",
            BattleCardType.Musou => "무쌍",
            BattleCardType.Healer => "힐러",
            BattleCardType.Bomber => "폭탄",
            _ => "카드"
        };
    }
}
