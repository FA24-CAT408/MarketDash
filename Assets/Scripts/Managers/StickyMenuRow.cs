using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class StickyMenuRow : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerDownHandler
{
    [SerializeField] private StickyMainMenuView _owner;
    [SerializeField] private Button _button;
    [SerializeField] private RectTransform _checkmark;
    [SerializeField] private int _index;

    private Tween _shiftTween;
    private Sequence _submitTween;

    public Button Button => _button;
    public RectTransform RectTransform => (RectTransform)transform;
    public int Index => _index;

    public void Configure(StickyMainMenuView owner, Button button, RectTransform checkmark, int index)
    {
        _owner = owner;
        _button = button;
        _checkmark = checkmark;
        _index = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _owner?.HandlePointerEnter(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _owner?.ResetIdleTimer();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _owner?.HandleRowSelected(this);
    }

    public void SetShifted(bool shifted, bool animate)
    {
        RectTransform rect = RectTransform;
        _shiftTween?.Kill();
        float targetX = shifted ? 10f : 0f;

        if (animate)
        {
            _shiftTween = rect.DOAnchorPosX(targetX, 0.18f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }
        else
        {
            Vector2 position = rect.anchoredPosition;
            position.x = targetX;
            rect.anchoredPosition = position;
        }
    }

    public void PlaySubmit()
    {
        _submitTween?.Kill();
        if (_checkmark != null)
        {
            _checkmark.gameObject.SetActive(true);
            _checkmark.localScale = Vector3.zero;
        }

        _submitTween = DOTween.Sequence().SetUpdate(true);
        if (_checkmark != null)
            _submitTween.Join(_checkmark.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        _submitTween.Join(RectTransform.DOScale(new Vector3(0.96f, 0.96f, 1f), 0.14f)
            .SetEase(Ease.OutQuad));
        _submitTween.Append(RectTransform.DOScale(Vector3.one, 0.36f)
            .SetEase(Ease.OutCubic));
    }

    public void HideCheckmark()
    {
        if (_checkmark != null)
            _checkmark.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _shiftTween?.Kill();
        _submitTween?.Kill();
    }
}
