using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyCardDragHandler : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private StartSceneController owner; /* 드래그 종료 처리를 넘겨받는 로비 컨트롤러 */
    private ScrollRect parentScrollRect; /* 카드 목록을 아래로 끌어 스크롤하기 위한 부모 스크롤 */
    private int cardIndex; /* CardCatalog.Cards 기준 카드 인덱스 */

    /* 카드 버튼 풀을 만들 때 컨트롤러와 카드 인덱스를 연결합니다. */
    public void Setup(StartSceneController newOwner, int newCardIndex)
    {
        owner = newOwner;
        cardIndex = newCardIndex;
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    /* 카드 버튼 위에서 끌어도 ScrollRect가 바로 반응하도록 드래그 임계값을 낮춥니다. */
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
        parentScrollRect?.OnInitializePotentialDrag(eventData);
    }

    /* 카드 버튼 드래그는 카드 이동이 아니라 목록 스크롤로 넘깁니다. */
    public void OnBeginDrag(PointerEventData eventData)
    {
        parentScrollRect?.OnBeginDrag(eventData);
    }

    /* 카드 목록을 아래위로 끌 수 있게 ScrollRect로 드래그를 전달합니다. */
    public void OnDrag(PointerEventData eventData)
    {
        parentScrollRect?.OnDrag(eventData);
    }

    /* 스크롤 드래그 종료를 ScrollRect에 전달합니다. */
    public void OnEndDrag(PointerEventData eventData)
    {
        parentScrollRect?.OnEndDrag(eventData);
    }
}
