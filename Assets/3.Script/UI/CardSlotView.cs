using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardSlotView : MonoBehaviour
{
    [SerializeField] private Button button; /* 카드 클릭 입력 버튼 */
    [SerializeField] private Image background; /* 선택/피격 상태를 보여주는 카드 배경 */
    [SerializeField] private Image artworkImage; /* 카드 일러스트 또는 임시 색상 영역 */
    [SerializeField] private Text nameText; /* 카드 이름 텍스트 */
    [SerializeField] private Text typeText; /* 카드 타입 텍스트 */
    [SerializeField] private Text hpText; /* 카드 체력 텍스트 */
    [SerializeField] private Text abilityText; /* 카드 효과 설명 텍스트 */

    private BattleCardRuntime boundCard; /* 현재 슬롯에 연결된 전투 카드 */
    private Action<CardSlotView> clicked; /* 슬롯 클릭 시 전투 매니저로 전달할 콜백 */
    private Coroutine scaleRoutine; /* 선택/공격 확대 연출 코루틴 */
    private Coroutine moveRoutine; /* 공격 전진/피격 흔들림 코루틴 */
    private Coroutine damageTextRoutine; /* 피해 숫자 표시 코루틴 */
    private Vector3 baseLocalPosition; /* 슬롯 기본 위치 */
    private Vector3 baseScale = Vector3.one; /* 슬롯 기본 크기 */

    [SerializeField] private Sprite cardBackSprite; /* 카드 배치 연출 때 보여줄 카드 뒷면 이미지 */

    private Coroutine flipRoutine; /* 새 카드가 슬롯에 들어올 때 카드 뒷면에서 앞면으로 뒤집는 코루틴 */

    public BattleCardRuntime BoundCard => boundCard;

    /* 버튼 이벤트와 기본 위치를 초기화합니다. */
    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseScale = transform.localScale;
        ApplyRuntimeLayout();

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    /* 슬롯 제거 시 이벤트를 정리해 중복 호출을 막습니다. */
    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);

        UnbindEvent();
    }

    /* 전투 카드 데이터를 슬롯에 연결하고 화면을 갱신합니다. */
    public void Bind(BattleCardRuntime card, Action<CardSlotView> onClicked)
    {
        // 같은 카드가 다시 바인딩되는 경우.
        // RefreshAllSlots가 여러 번 호출되면서 뒤집기 연출을 중간에 덮는 문제를 막는다.
        if (boundCard == card)
        {
            clicked = onClicked;

            if (flipRoutine == null)
                Refresh();

            return;
        }

        // 다른 카드로 교체되는 경우 기존 연출 정리
        if (flipRoutine != null)
        {
            StopCoroutine(flipRoutine);
            flipRoutine = null;
            transform.localScale = baseScale;
        }

        UnbindEvent();

        boundCard = card;
        clicked = onClicked;

        if (boundCard != null)
            boundCard.OnChanged += HandleCardChanged;

        bool shouldPlayDeployFlip = boundCard != null && !boundCard.IsDead;

        if (shouldPlayDeployFlip)
            PlayDeployFlip();
        else
            Refresh();
    }

    /* 선택된 아군 카드를 색과 크기로 강조합니다. */
    public void SetSelected(bool selected)
    {
        if (background == null)
            return;

        background.color = selected ? new Color(1f, 0.78f, 0.18f, 1f) : GetBaseCardColor();
        transform.localScale = selected ? baseScale * 1.08f : baseScale;
    }

    /* 행동 주체 카드가 앞으로 나오는 짧은 연출입니다. */
    public void PlayFocus()
    {
        PlayScale(baseScale * 1.16f, 0.16f);
        PlayMove(new Vector3(0f, boundCard != null && boundCard.Owner == CardOwner.Player ? 28f : -28f, 0f), 0.16f);
    }

    /* 피격 카드가 흔들리고 붉게 반짝이는 연출입니다. */
    public void PlayHit()
    {
        PlayScale(baseScale * 0.9f, 0.12f);
        PlayShake(18f, 0.28f);
        StartCoroutine(CoHitFlash());
        StartCoroutine(CoSlashEffect());
    }

    /* 피해 숫자를 카드 위에 띄웁니다. */
    public void ShowDamageText(int damage)
    {
        if (damage <= 0)
            return;

        if (damageTextRoutine != null)
            StopCoroutine(damageTextRoutine);

        damageTextRoutine = StartCoroutine(CoDamageText(damage));
    }

    /* 연결된 카드 상태를 기준으로 이름, 타입, HP, 능력, 일러스트를 갱신합니다. */
    public void Refresh()
    {
        bool hasCard = boundCard != null && !boundCard.IsDead;
        EnsureTextLayerOrder();

        if (button != null)
            button.interactable = hasCard;

        if (nameText != null)
            nameText.text = hasCard ? boundCard.Data.CardName : "EMPTY";

        if (typeText != null)
            typeText.text = hasCard ? GetCardTypeName(boundCard.Data.CardType) : string.Empty;

        if (hpText != null)
            hpText.text = hasCard ? $"HP {boundCard.CurrentHp}/{boundCard.Data.MaxHp}" : string.Empty;

        if (abilityText != null)
            abilityText.text = hasCard ? GetShortAbilityText(boundCard.Data.CardType) : string.Empty;

        if (artworkImage != null)
        {
            artworkImage.enabled = hasCard;

            Sprite sprite = hasCard ? ResolveCardSprite() : null;
            artworkImage.sprite = sprite;

            if (!hasCard)
            {
                artworkImage.color = Color.clear;
            }
            else if (sprite != null)
            {
                artworkImage.color = Color.white;
            }
            else
            {
                // 이미지가 없을 때도 투명 처리하지 말고 카드 타입 색상이라도 보여준다.
                artworkImage.color = boundCard.Data.CardColor;
            }

            artworkImage.preserveAspect = true;
        }

        if (background != null)
        {
            background.sprite = null;
            background.color = hasCard ? GetBaseCardColor() : new Color(0.18f, 0.16f, 0.13f, 1f);
        }

        transform.localScale = baseScale;
    }

    private string GetShortAbilityText(BattleCardType cardType)
    {
        return cardType switch
        {
            BattleCardType.Normal => "현재 HP 피해\n대상 HP 반격",
            BattleCardType.Ranged => "현재 HP 피해\n반격 없음",
            BattleCardType.Musou => "대상 100%\n인접 1장 50%",
            BattleCardType.Healer => "턴 시작\n아군 HP +1",
            BattleCardType.Bomber => "대상 피해\n나머지 1 피해",
            BattleCardType.Vampire => "피해 후\n자신 HP +2",
            BattleCardType.Berserker => "잃은 HP만큼\n추가 피해",
            BattleCardType.Guardian => "절반 피해\n자신 HP +1",
            BattleCardType.Piercing => "대상 피해\n양옆 1 피해",
            _ => string.Empty
        };
    }

    /* 런타임 UI 빌더가 생성한 하위 컴포넌트를 연결합니다. */
    public void SetupReferences(Button button, Image background, Image artworkImage, Text nameText, Text typeText, Text hpText, Text abilityText)
    {
        this.button = button;
        this.background = background;
        this.artworkImage = artworkImage;
        this.nameText = nameText;
        this.typeText = typeText;
        this.hpText = hpText;
        this.abilityText = abilityText;
        ApplyRuntimeLayout();
        EnsureTextLayerOrder();
    }

    /* 인스펙터나 런타임 빌더에서 카드 뒷면 이미지를 교체할 수 있게 합니다. */
    public void SetCardBackSprite(Sprite sprite)
    {
        cardBackSprite = sprite;
    }

    /* 카드 클릭을 전투 매니저 콜백으로 전달합니다. */
    // 카드 일러스트가 타입/HP/효과 글자를 덮지 않도록 텍스트 렌더 순서를 항상 위로 올립니다.
    private void EnsureTextLayerOrder()
    {
        if (nameText != null)
            nameText.transform.SetAsLastSibling();

        if (typeText != null)
            typeText.transform.SetAsLastSibling();

        if (hpText != null)
            hpText.transform.SetAsLastSibling();

        if (abilityText != null)
            abilityText.transform.SetAsLastSibling();
    }

    /* 전투 카드 내부 이미지/텍스트 영역을 카드 비율에 맞게 정리합니다. */
    private void ApplyRuntimeLayout()
    {
        SetRect(nameText, new Vector2(0f, 137f), new Vector2(230f, 40f));

        // 이미지 크게
        SetRect(artworkImage, new Vector2(0f, 55f), new Vector2(190f, 145f));

        // 텍스트 아래로 정리
        SetRect(typeText, new Vector2(0f, -35f), new Vector2(230f, 28f));
        SetRect(hpText, new Vector2(0f, -72f), new Vector2(230f, 34f));
        SetRect(abilityText, new Vector2(0f, -128f), new Vector2(232f, 58f));

        if (nameText != null)
        {
            nameText.fontSize = 25;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
        }

        if (typeText != null)
        {
            typeText.fontSize = 18;
            typeText.alignment = TextAnchor.MiddleCenter;
        }

        if (hpText != null)
        {
            hpText.fontSize = 24;
            hpText.fontStyle = FontStyle.Bold;
            hpText.alignment = TextAnchor.MiddleCenter;
        }

        if (abilityText != null)
        {
            abilityText.resizeTextForBestFit = true;
            abilityText.resizeTextMinSize = 12;
            abilityText.resizeTextMaxSize = 15;
            abilityText.alignment = TextAnchor.MiddleCenter;
        }
    }

    /* 연결된 UI 컴포넌트의 RectTransform 위치와 크기를 바꿉니다. */
    private void SetRect(Graphic graphic, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = graphic != null ? graphic.transform as RectTransform : null;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void HandleClick()
    {
        clicked?.Invoke(this);
    }

    /* 카드 HP가 바뀌면 슬롯 UI를 다시 그립니다. */
    private void HandleCardChanged(BattleCardRuntime card)
    {
        if (flipRoutine != null)
            return;

        Refresh();
    }

    /* 이전 카드에 연결된 변경 이벤트를 해제합니다. */
    private void UnbindEvent()
    {
        if (boundCard != null)
            boundCard.OnChanged -= HandleCardChanged;
    }

    /* 소유자에 따라 기본 카드 배경 톤을 다르게 줍니다. */
    private Color GetBaseCardColor()
    {
        if (boundCard == null)
            return new Color(0.96f, 0.92f, 0.82f, 1f);

        return boundCard.Owner == CardOwner.Player
            ? new Color(0.98f, 0.92f, 0.76f, 1f)
            : new Color(0.86f, 0.9f, 1f, 1f);
    }

    /* 지정한 크기로 커졌다가 원래 크기로 돌아오는 연출을 시작합니다. */
    private void PlayScale(Vector3 targetScale, float halfDuration)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(CoScale(targetScale, halfDuration));
    }

    /* 부드러운 카드 확대/복귀 연출입니다. */
    private IEnumerator CoScale(Vector3 targetScale, float halfDuration)
    {
        Vector3 startScale = transform.localScale;
        float time = 0f;

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

    /* 지정한 위치로 이동했다가 기본 위치로 돌아오는 연출을 시작합니다. */
    /* 새 카드가 전장에 공개될 때 뒷면을 먼저 보여준 뒤 앞면으로 뒤집습니다. */
    private void PlayDeployFlip()
    {
        if (flipRoutine != null)
            StopCoroutine(flipRoutine);

        flipRoutine = StartCoroutine(CoDeployFlip());
    }

    /* X축 스케일을 접었다 펴서 카드가 뒤집히는 느낌을 만듭니다. */
    private IEnumerator CoDeployFlip()
    {
        SetBackSideVisible();
        transform.localScale = new Vector3(0f, baseScale.y, baseScale.z);

        yield return CoFlipScale(0f, baseScale.x, 0.16f);
        yield return new WaitForSeconds(0.1f);
        yield return CoFlipScale(baseScale.x, 0f, 0.16f);

        Refresh();
        transform.localScale = new Vector3(0f, baseScale.y, baseScale.z);
        yield return CoFlipScale(0f, baseScale.x, 0.16f);

        transform.localScale = baseScale;
        flipRoutine = null;
    }

    /* 카드 뒤집기 연출에서 X축 크기만 보간합니다. */
    private IEnumerator CoFlipScale(float fromX, float toX, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float x = Mathf.Lerp(fromX, toX, time / duration);
            transform.localScale = new Vector3(x, baseScale.y, baseScale.z);
            yield return null;
        }
    }

    /* 카드 앞면 정보는 숨기고 뒷면 이미지나 임시 색상을 보여줍니다. */
    private void SetBackSideVisible()
    {
        if (button != null)
            button.interactable = false;

        if (nameText != null)
            nameText.text = string.Empty;

        if (typeText != null)
            typeText.text = string.Empty;

        if (hpText != null)
            hpText.text = string.Empty;

        if (abilityText != null)
            abilityText.text = string.Empty;

        Sprite backSprite = GetCardBackSprite();

        if (background != null)
        {
            background.sprite = backSprite;
            background.color = backSprite != null ? Color.white : new Color(0.055f, 0.135f, 0.32f, 1f);
            background.preserveAspect = true;
        }

        if (artworkImage != null)
        {
            artworkImage.enabled = backSprite == null;
            artworkImage.sprite = null;
            artworkImage.color = new Color(0.08f, 0.23f, 0.48f, 1f);
            artworkImage.preserveAspect = true;
        }
    }

    /* 인스펙터 값이 없으면 Resources/CardBack 이미지를 자동으로 찾습니다. */
    private Sprite GetCardBackSprite()
    {
        if (cardBackSprite == null)
            cardBackSprite = Resources.Load<Sprite>("CardBack");

        return cardBackSprite;
    }

    private void PlayMove(Vector3 offset, float halfDuration)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(CoMove(baseLocalPosition + offset, halfDuration));
    }

    /* 카드가 전진했다가 돌아오는 이동 연출입니다. */
    private IEnumerator CoMove(Vector3 targetPosition, float halfDuration)
    {
        Vector3 startPosition = transform.localPosition;
        float time = 0f;

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

    /* 좌우 흔들림으로 피격감을 만듭니다. */
    private void PlayShake(float power, float duration)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(CoShake(power, duration));
    }

    /* 짧은 시간 동안 좌우 흔들림을 반복합니다. */
    private IEnumerator CoShake(float power, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float direction = Mathf.Sin(time * 70f);
            transform.localPosition = baseLocalPosition + new Vector3(direction * power, 0f, 0f);
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        moveRoutine = null;
    }

    /* 피격 순간 카드 배경을 붉게 점멸시킵니다. */
    private IEnumerator CoHitFlash()
    {
        if (background == null)
            yield break;

        Color origin = background.color;
        for (int i = 0; i < 2; i++)
        {
            background.color = new Color(1f, 0.18f, 0.12f, 1f);
            yield return new WaitForSeconds(0.08f);
            background.color = origin;
            yield return new WaitForSeconds(0.05f);
        }
    }

    /* 피해 숫자를 위로 띄우면서 사라지게 합니다. */
    private IEnumerator CoDamageText(int damage)
    {
        GameObject textObject = new GameObject("DamageText");
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 18f);
        rect.sizeDelta = new Vector2(180f, 70f);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = $"-{damage}";
        text.fontSize = 44;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.18f, 0.12f, 1f);

        float duration = 0.65f;
        float time = 0f;
        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + new Vector2(0f, 70f);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1f - t);
            yield return null;
        }

        Destroy(textObject);
        damageTextRoutine = null;
    }

    /* 카드 위를 가로지르는 짧은 베기 이펙트를 생성합니다. */
    private IEnumerator CoSlashEffect()
    {
        GameObject slashObject = new GameObject("HitSlash");
        slashObject.transform.SetParent(transform, false);

        RectTransform rect = slashObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(230f, 18f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -18f);

        Image image = slashObject.AddComponent<Image>();
        image.color = new Color(1f, 0.82f, 0.18f, 0.95f);

        float duration = 0.22f;
        float time = 0f;
        Vector3 startScale = new Vector3(0.35f, 1f, 1f);
        Vector3 endScale = new Vector3(1.15f, 1f, 1f);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            rect.localScale = Vector3.Lerp(startScale, endScale, t);
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f - t);
            yield return null;
        }

        Destroy(slashObject);
    }

    /* 카드 타입 enum을 화면 표시용 한글명으로 변환합니다. */
    private string GetCardTypeName(BattleCardType cardType)
    {
        return cardType switch
        {
            BattleCardType.Normal => "\uC77C\uBC18",
            BattleCardType.Ranged => "\uC6D0\uAC70\uB9AC",
            BattleCardType.Musou => "\uBB34\uC30D",
            BattleCardType.Healer => "\uD790\uB7EC",
            BattleCardType.Bomber => "\uD3ED\uD0C4",
            BattleCardType.Vampire => "\uD761\uD608",
            BattleCardType.Berserker => "\uAD11\uC804\uC0AC",
            BattleCardType.Guardian => "\uC218\uD638\uC790",
            BattleCardType.Piercing => "\uAD00\uD1B5",
            _ => "\uCE74\uB4DC"
        };
    }

    private Sprite ResolveCardSprite()
    {
        if (boundCard == null || boundCard.Data == null)
            return null;

        if (boundCard.Data.CardSprite != null)
            return boundCard.Data.CardSprite;

        string cardName = boundCard.Data.CardName;
        string compactName = cardName.Replace(" ", string.Empty);

        Sprite sprite = Resources.Load<Sprite>($"CardImages/{cardName}");
        if (sprite != null)
            return sprite;

        sprite = Resources.Load<Sprite>($"CardImages/{compactName}");
        if (sprite != null)
            return sprite;

#if UNITY_EDITOR
        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/4.Sprite/{cardName}.png");
        if (sprite != null)
            return sprite;

        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/4.Sprite/{compactName}.png");
        if (sprite != null)
            return sprite;
#endif

        return null;
    }
}
