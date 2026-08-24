using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public sealed class StickyMainMenuView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Integration")]
    [SerializeField] private MainMenuController _controller;
    [SerializeField] private GameSaveManager _gameSaveManager;

    [Header("Presentation")]
    [SerializeField] private CanvasGroup _backgroundDim;
    [SerializeField] private RectTransform _logo;
    [SerializeField] private CanvasGroup _logoGroup;
    [SerializeField] private RectTransform _note;
    [SerializeField] private CanvasGroup _noteGroup;
    [SerializeField] private RectTransform _bestRunCard;
    [SerializeField] private CanvasGroup _bestRunCardGroup;
    [SerializeField] private TMP_Text _idleTimerText;
    [SerializeField] private TMP_Text _bestRunTimeText;
    [SerializeField] private TMP_Text _leaderboardText;
    [SerializeField] private RectTransform _selectionArrow;
    [SerializeField] private CanvasGroup _selectionArrowGroup;
    [SerializeField] private StickyMenuRow[] _rows;

    [Header("Placeholder data")]
    [SerializeField] private string _leaderboardPlaceholder = "RANK #147 · WR 12:34.56 \"cartgod\"";

    [Header("Motion")]
    [SerializeField] private float _backgroundDimAlpha = 0.22f;
    [SerializeField] private float _entranceDelay = 1.25f;

    private const float RowPitch = 47f;
    private const float ArrowTargetX = 2f;

    private StickyMenuRow _selectedRow;
    private float _idleSeconds;
    private bool _pointerOverNote;
    private bool _keyboardNavigationActive;
    private bool _selectingFromPointer;
    private bool _suppressSelectionAnimation;
    private bool _submitting;
    private bool _entrancePlayed;
    private bool _eventNavigationWasEnabled;
    private bool _eventNavigationOverridden;
    private bool _stickNavigationActive;
    private float _nextStickNavigationTime;
    private Vector2 _lastPointerPosition;
    private Coroutine _selectionRoutine;
    private Sequence _entranceSequence;
    private Sequence _submitSequence;
    private Tween _dimTween;
    private Tween _arrowTween;

    public void Configure(
        MainMenuController controller,
        GameSaveManager gameSaveManager,
        CanvasGroup backgroundDim,
        RectTransform logo,
        CanvasGroup logoGroup,
        RectTransform note,
        CanvasGroup noteGroup,
        RectTransform bestRunCard,
        CanvasGroup bestRunCardGroup,
        TMP_Text idleTimerText,
        TMP_Text bestRunTimeText,
        TMP_Text leaderboardText,
        RectTransform selectionArrow,
        CanvasGroup selectionArrowGroup,
        StickyMenuRow[] rows)
    {
        _controller = controller;
        _gameSaveManager = gameSaveManager;
        _backgroundDim = backgroundDim;
        _logo = logo;
        _logoGroup = logoGroup;
        _note = note;
        _noteGroup = noteGroup;
        _bestRunCard = bestRunCard;
        _bestRunCardGroup = bestRunCardGroup;
        _idleTimerText = idleTimerText;
        _bestRunTimeText = bestRunTimeText;
        _leaderboardText = leaderboardText;
        _selectionArrow = selectionArrow;
        _selectionArrowGroup = selectionArrowGroup;
        _rows = rows;
    }

    private void Awake()
    {
        if (_rows == null)
            return;

        for (int i = 0; i < _rows.Length; i++)
        {
            int index = i;
            if (_rows[i] != null && _rows[i].Button != null)
                _rows[i].Button.onClick.AddListener(() => Submit(index));
        }
    }

    private void OnEnable()
    {
        _idleSeconds = 0f;
        _lastPointerPosition = ReadPointerPosition();
        if (EventSystem.current != null)
        {
            _eventNavigationWasEnabled = EventSystem.current.sendNavigationEvents;
            EventSystem.current.sendNavigationEvents = false;
            _eventNavigationOverridden = true;
        }
        ResetSubmitVisuals();
        RefreshBestRun();
        RefreshIdleTimer();

        if (!_entrancePlayed)
        {
            _entrancePlayed = true;
            PlayEntrance();
        }
        else
        {
            SetEntranceElementsVisible();
        }

        if (_selectionRoutine != null)
            StopCoroutine(_selectionRoutine);
        _selectionRoutine = StartCoroutine(SelectDefaultAfterLayout());
    }

    private void ResetSubmitVisuals()
    {
        if (_rows == null)
            return;

        for (int i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] == null)
                continue;

            _rows[i].HideCheckmark();
            _rows[i].RectTransform.localScale = Vector3.one;
        }
    }

    private void OnDisable()
    {
        _pointerOverNote = false;
        _submitting = false;
        if (_eventNavigationOverridden && EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = _eventNavigationWasEnabled;
            _eventNavigationOverridden = false;
        }
        _submitSequence?.Kill();
        _submitSequence = null;
    }

    private void OnDestroy()
    {
        _entranceSequence?.Kill();
        _submitSequence?.Kill();
        _dimTween?.Kill();
        _arrowTween?.Kill();
    }

    private void Update()
    {
        int navigationDirection;
        bool submitInput;
        bool pointerMoved;
        bool anyInput = ReadInput(out navigationDirection, out submitInput, out pointerMoved);

        if (anyInput)
            _idleSeconds = 0f;
        else
            _idleSeconds += Time.unscaledDeltaTime;

        RefreshIdleTimer();

        if (pointerMoved)
        {
            _keyboardNavigationActive = false;
            if (!_pointerOverNote)
                HideSelectionVisuals(true);
        }

        if (navigationDirection != 0)
        {
            _keyboardNavigationActive = true;
            Navigate(navigationDirection);
        }

        if (submitInput && !_submitting)
        {
            _keyboardNavigationActive = true;
            StickyMenuRow current = _selectedRow ?? FindRow(EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null);
            if (current != null)
                Submit(current.Index);
        }
    }

    private void Navigate(int direction)
    {
        if (_rows == null || _rows.Length == 0)
            return;

        StickyMenuRow current = _selectedRow ?? FindRow(EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null);
        int currentIndex = current != null ? current.Index : 0;
        int nextIndex = (currentIndex + direction + _rows.Length) % _rows.Length;
        StickyMenuRow next = _rows[nextIndex];

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(next.gameObject);
        SelectRow(next, true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerOverNote = true;
        SetBackgroundDim(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerOverNote = false;
        if (!_keyboardNavigationActive)
            HideSelectionVisuals(true);
    }

    public void HandlePointerEnter(StickyMenuRow row)
    {
        if (row == null || _submitting)
            return;

        _selectingFromPointer = true;
        row.Button.Select();
        _selectingFromPointer = false;
        SelectRow(row, true);
        ResetIdleTimer();
    }

    public void HandleRowSelected(StickyMenuRow row)
    {
        if (row == null || _submitting)
            return;

        if (!_selectingFromPointer && !_suppressSelectionAnimation)
            _keyboardNavigationActive = true;

        SelectRow(row, !_suppressSelectionAnimation);
    }

    public void ResetIdleTimer()
    {
        _idleSeconds = 0f;
        RefreshIdleTimer();
    }

    private IEnumerator SelectDefaultAfterLayout()
    {
        yield return null;

        if (_rows == null || _rows.Length == 0 || _rows[0] == null)
            yield break;

        _suppressSelectionAnimation = true;
        _keyboardNavigationActive = true;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(_rows[0].gameObject);
        SelectRow(_rows[0], false);
        _suppressSelectionAnimation = false;
        _selectionRoutine = null;
    }

    private void SelectRow(StickyMenuRow row, bool animate)
    {
        if (row == null)
            return;

        if (_selectedRow != null && _selectedRow != row)
            _selectedRow.SetShifted(false, animate);

        _selectedRow = row;
        _selectedRow.SetShifted(true, animate);
        SetBackgroundDim(true);
        ShowArrow(row.Index, animate);
    }

    private void HideSelectionVisuals(bool animate)
    {
        if (_selectedRow != null)
            _selectedRow.SetShifted(false, animate);

        _selectedRow = null;
        SetBackgroundDim(false);

        _arrowTween?.Kill();
        if (_selectionArrowGroup == null)
            return;

        if (animate)
            _arrowTween = _selectionArrowGroup.DOFade(0f, 0.1f).SetUpdate(true);
        else
            _selectionArrowGroup.alpha = 0f;
    }

    private void ShowArrow(int rowIndex, bool animate)
    {
        if (_selectionArrow == null || _selectionArrowGroup == null)
            return;

        _arrowTween?.Kill();
        _selectionArrow.DOKill();

        float y = -(rowIndex * RowPitch + RowPitch * 0.5f);
        Vector2 target = new Vector2(ArrowTargetX, y);
        _selectionArrow.anchoredPosition = animate ? target + Vector2.left * 10f : target;
        _selectionArrowGroup.alpha = animate ? 0f : 1f;

        if (!animate)
            return;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(_selectionArrow.DOAnchorPos(target, 0.18f).SetEase(Ease.OutCubic));
        sequence.Join(_selectionArrowGroup.DOFade(1f, 0.1f).SetEase(Ease.Linear));
        _arrowTween = sequence;
    }

    private void SetBackgroundDim(bool active)
    {
        if (_backgroundDim == null)
            return;

        bool shouldDim = active || _pointerOverNote || _keyboardNavigationActive;
        _dimTween?.Kill();
        _dimTween = _backgroundDim.DOFade(shouldDim ? _backgroundDimAlpha : 0f, 0.35f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private void Submit(int index)
    {
        if (_submitting || _rows == null || index < 0 || index >= _rows.Length)
            return;

        _submitting = true;
        ResetIdleTimer();
        SelectRow(_rows[index], true);
        _rows[index].PlaySubmit();

        _submitSequence?.Kill();
        _submitSequence = DOTween.Sequence().SetUpdate(true);
        _submitSequence.AppendInterval(0.24f);
        _submitSequence.AppendCallback(() => ExecuteAction(index));
    }

    private void ExecuteAction(int index)
    {
        if (_controller == null)
        {
            _submitting = false;
            return;
        }

        switch (index)
        {
            case 0:
                _controller.PlayGame();
                break;
            case 1:
                _controller.ResetGame();
                _controller.PlayGame();
                break;
            case 2:
                PlayLeaderboardUnavailableFeedback();
                break;
            case 3:
                _controller.OpenSettings();
                break;
            case 4:
                _controller.QuitGame();
                _submitting = false;
                break;
        }
    }

    private void PlayLeaderboardUnavailableFeedback()
    {
        if (_selectedRow == null)
        {
            _submitting = false;
            return;
        }

        _selectedRow.RectTransform.DOShakeAnchorPos(0.42f, new Vector2(8f, 0f), 12, 55f, false, true)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _selectedRow.HideCheckmark();
                _submitting = false;
            });
    }

    private void PlayEntrance()
    {
        if (_logo == null || _note == null || _bestRunCard == null)
            return;

        _entranceSequence?.Kill();
        Vector2 logoTarget = _logo.anchoredPosition;
        Vector2 noteTarget = _note.anchoredPosition;
        Vector2 cardTarget = _bestRunCard.anchoredPosition;

        PrepareEntranceElement(_logo, _logoGroup, logoTarget);
        PrepareEntranceElement(_note, _noteGroup, noteTarget);
        PrepareEntranceElement(_bestRunCard, _bestRunCardGroup, cardTarget);

        _entranceSequence = DOTween.Sequence().SetUpdate(true);
        AppendEntrance(_entranceSequence, _logo, _logoGroup, logoTarget, _entranceDelay);
        AppendEntrance(_entranceSequence, _note, _noteGroup, noteTarget, _entranceDelay + 0.06f);
        AppendEntrance(_entranceSequence, _bestRunCard, _bestRunCardGroup, cardTarget, _entranceDelay + 0.12f);
    }

    private static void PrepareEntranceElement(RectTransform element, CanvasGroup group, Vector2 target)
    {
        element.anchoredPosition = target + Vector2.down * 14f;
        if (group != null)
            group.alpha = 0f;
    }

    private static void AppendEntrance(
        Sequence sequence,
        RectTransform element,
        CanvasGroup group,
        Vector2 target,
        float insertTime)
    {
        sequence.Insert(insertTime,
            element.DOAnchorPos(target, 0.46f).SetEase(Ease.OutCubic));
        if (group != null)
        {
            sequence.Insert(insertTime,
                group.DOFade(1f, 0.4f).SetEase(Ease.OutQuad));
        }
    }

    private void SetEntranceElementsVisible()
    {
        if (_logoGroup != null)
            _logoGroup.alpha = 1f;
        if (_noteGroup != null)
            _noteGroup.alpha = 1f;
        if (_bestRunCardGroup != null)
            _bestRunCardGroup.alpha = 1f;
    }

    private void RefreshBestRun()
    {
        if (_leaderboardText != null)
            _leaderboardText.text = _leaderboardPlaceholder;

        if (_bestRunTimeText == null)
            return;

        bool hasCompletedRun = false;
        if (_gameSaveManager != null)
        {
            var entries = _gameSaveManager.LevelTimeEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsCompleted)
                {
                    hasCompletedRun = true;
                    break;
                }
            }
        }

        _bestRunTimeText.text = hasCompletedRun
            ? FormatElapsedTime(_gameSaveManager.TotalTime)
            : "--:--.--";
    }

    private void RefreshIdleTimer()
    {
        if (_idleTimerText != null)
            _idleTimerText.text = "IDLE " + FormatElapsedTime(_idleSeconds);
    }

    private static string FormatElapsedTime(float totalSeconds)
    {
        int totalCentiseconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds * 100f));
        int centiseconds = totalCentiseconds % 100;
        int totalWholeSeconds = totalCentiseconds / 100;
        int seconds = totalWholeSeconds % 60;
        int totalMinutes = totalWholeSeconds / 60;

        if (totalMinutes >= 60)
        {
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours}:{minutes:00}:{seconds:00}.{centiseconds:00}";
        }

        return $"{totalMinutes}:{seconds:00}.{centiseconds:00}";
    }

    private bool ReadInput(out int navigationDirection, out bool submitInput, out bool pointerMoved)
    {
        navigationDirection = 0;
        submitInput = false;
        pointerMoved = false;
        bool anyInput = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            anyInput |= keyboard.anyKey.wasPressedThisFrame;
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                navigationDirection = -1;
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                navigationDirection = 1;
            submitInput = keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 pointerPosition = mouse.position.ReadValue();
            pointerMoved = (pointerPosition - _lastPointerPosition).sqrMagnitude > 0.01f;
            _lastPointerPosition = pointerPosition;
            anyInput |= pointerMoved
                || mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame
                || mouse.scroll.ReadValue().sqrMagnitude > 0.01f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            bool stickMoved = gamepad.leftStick.ReadValue().sqrMagnitude > 0.12f
                || gamepad.rightStick.ReadValue().sqrMagnitude > 0.12f;
            anyInput |= stickMoved;

            for (int i = 0; i < gamepad.allControls.Count; i++)
            {
                if (gamepad.allControls[i] is ButtonControl button && button.wasPressedThisFrame)
                {
                    anyInput = true;
                    break;
                }
            }

            if (gamepad.dpad.up.wasPressedThisFrame)
                navigationDirection = -1;
            else if (gamepad.dpad.down.wasPressedThisFrame)
                navigationDirection = 1;

            float stickY = gamepad.leftStick.y.ReadValue();
            if (Mathf.Abs(stickY) > 0.55f)
            {
                if (!_stickNavigationActive || Time.unscaledTime >= _nextStickNavigationTime)
                {
                    navigationDirection = stickY > 0f ? -1 : 1;
                    _nextStickNavigationTime = Time.unscaledTime + (_stickNavigationActive ? 0.12f : 0.42f);
                }
                _stickNavigationActive = true;
            }
            else
            {
                _stickNavigationActive = false;
            }

            submitInput |= gamepad.buttonSouth.wasPressedThisFrame;
        }

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            anyInput = true;

        return anyInput;
    }

    private static Vector2 ReadPointerPosition()
    {
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private StickyMenuRow FindRow(GameObject selectedObject)
    {
        if (selectedObject == null || _rows == null)
            return null;

        for (int i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] != null && _rows[i].gameObject == selectedObject)
                return _rows[i];
        }

        return null;
    }
}
