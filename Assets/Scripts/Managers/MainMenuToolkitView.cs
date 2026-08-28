using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class MainMenuToolkitView : MonoBehaviour
{
    private enum ReceiptPage
    {
        Main,
        Settings
    }

    [Header("Integration")]
    [SerializeField] private MainMenuController _controller;
    [SerializeField] private GameSaveManager _gameSaveManager;

    [Header("Motion")]
    [SerializeField] private float _entranceDelay = 1.25f;
    [SerializeField] private bool _reduceMotion;

    private const float SubmitFeedbackDuration = 0.28f;
    private const float ReceiptFeedDistance = 700f;
    private const float ReceiptFeedDuration = 0.64f;
    private const float StageWidth = 1120f;
    private const float StageHeight = 720f;
    private const int SettingStepCount = 10;
    private const float SensitivityMinimum = 0.1f;
    private const float SensitivityMaximum = 5f;

    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _stage;
    private VisualElement _feedMask;
    private VisualElement _fallLayer;
    private VisualElement _mainPaper;
    private VisualElement _settingsPaper;
    private VisualElement _mainPaperShadow;
    private VisualElement _settingsPaperShadow;
    private VisualElement _printerShell;
    private VisualElement _printerStatusLight;
    private VisualElement _printerStatusHalo;
    private VisualElement _invertCheck;
    private Label _idleTimerText;
    private Label _bestRunTagTimeText;
    private Label _playthroughCountText;
    private Label _sensitivityValueText;
    private Label _volumeValueText;
    private Label _invertValueText;

    private readonly List<Button> _mainRows = new();
    private readonly List<Button> _settingsRows = new();
    private readonly List<VisualElement> _entranceElements = new();
    private readonly List<VisualElement> _sensitivityNotches = new();
    private readonly List<VisualElement> _volumeNotches = new();

    private ReceiptPage _activePage = ReceiptPage.Main;
    private int _mainSelectedIndex;
    private int _settingsSelectedIndex;
    private float _idleSeconds;
    private bool _submitting;
    private bool _transitioning;
    private bool _entrancePlayed;
    private bool _verticalStickActive;
    private bool _horizontalStickActive;
    private float _nextVerticalStickTime;
    private float _nextHorizontalStickTime;
    private Vector2 _lastPointerPosition;
    private Button _pressedRow;
    private Tween _pointerScaleTween;
    private Sequence _submitSequence;
    private Tween _receiptFeedTween;
    private Sequence _tearSequence;

    public void Configure(MainMenuController controller, GameSaveManager gameSaveManager)
    {
        _controller = controller;
        _gameSaveManager = gameSaveManager;
    }

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        HidePapersBeforeRuntimeInitialization();
        StartCoroutine(InitializeView());
    }

    private void HidePapersBeforeRuntimeInitialization()
    {
        if (!Application.isPlaying || _document == null)
            return;

        VisualElement root = _document.rootVisualElement;
        VisualElement mainPaper = root?.Q<VisualElement>("menu-surface");
        VisualElement settingsPaper = root?.Q<VisualElement>("settings-surface");
        VisualElement mainShadow = root?.Q<VisualElement>("menu-shadow");
        VisualElement settingsShadow = root?.Q<VisualElement>("settings-shadow");
        if (mainPaper != null && !_entrancePlayed)
            mainPaper.style.translate = new Translate(0f, -ReceiptFeedDistance);
        if (mainShadow != null && !_entrancePlayed)
            mainShadow.style.translate = new Translate(0f, -ReceiptFeedDistance);
        if (settingsPaper != null)
            settingsPaper.style.translate = new Translate(0f, -ReceiptFeedDistance);
        if (settingsShadow != null)
            settingsShadow.style.translate = new Translate(0f, -ReceiptFeedDistance);

        root?.Q<VisualElement>("best-run-card")?.RemoveFromClassList("is-visible");
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        KillAllTweens();
        _submitting = false;
        _transitioning = false;
        _pressedRow = null;
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    private void Update()
    {
        if (_root == null)
            return;

        bool anyInput = ReadInput(
            out int verticalDirection,
            out int horizontalDirection,
            out bool submitInput,
            out bool cancelInput);

        if (anyInput)
            _idleSeconds = 0f;
        else
            _idleSeconds += Time.unscaledDeltaTime;

        RefreshIdleTimer();
        if (_transitioning)
            return;

        if (cancelInput && _activePage == ReceiptPage.Settings)
        {
            BeginReceiptSwap(ReceiptPage.Main);
            return;
        }

        if (verticalDirection != 0)
            Navigate(verticalDirection);
        if (horizontalDirection != 0 && _activePage == ReceiptPage.Settings)
            AdjustSelectedSetting(horizontalDirection);
        if (submitInput && !_submitting)
            SubmitActiveRow();
    }

    private IEnumerator InitializeView()
    {
        // UIDocument rebuilds its visual tree when its GameObject is re-enabled.
        yield return null;
        BuildView();
        if (enabled && _root != null)
            ResetView();
    }

    private void BuildView()
    {
        ClearCachedElements();
        _root = _document.rootVisualElement.Q<VisualElement>("main-menu-root");
        _stage = _root?.Q<VisualElement>("centered-stage");
        _feedMask = _root?.Q<VisualElement>(className: "paper-feed-mask");
        _fallLayer = _root?.Q<VisualElement>("receipt-fall-layer");
        _mainPaper = _root?.Q<VisualElement>("menu-surface");
        _settingsPaper = _root?.Q<VisualElement>("settings-surface");
        _mainPaperShadow = _root?.Q<VisualElement>("menu-shadow");
        _settingsPaperShadow = _root?.Q<VisualElement>("settings-shadow");
        _printerShell = _root?.Q<VisualElement>(className: "printer-shell");
        _printerStatusLight = _root?.Q<VisualElement>(className: "printer-status-light");
        _printerStatusHalo = _root?.Q<VisualElement>(className: "printer-status-halo");
        _invertCheck = _root?.Q<VisualElement>("invert-check");
        _idleTimerText = _root?.Q<Label>("idle-timer");
        _bestRunTagTimeText = _root?.Q<Label>("best-run-tag-time");
        _playthroughCountText = _root?.Q<Label>("playthrough-count");
        _sensitivityValueText = _root?.Q<Label>("sensitivity-value");
        _volumeValueText = _root?.Q<Label>("volume-value");
        _invertValueText = _root?.Q<Label>("invert-value");

        if (_root == null || _mainPaper == null || _settingsPaper == null || _feedMask == null || _fallLayer == null)
        {
            Debug.LogError("Main Menu UI Toolkit document is missing its required receipt elements.", this);
            enabled = false;
            return;
        }

        BuildMainRows();
        BuildSettingsRows();
        VisualElement sensitivityTrack = _root.Q<VisualElement>("sensitivity-notches");
        VisualElement volumeTrack = _root.Q<VisualElement>("volume-notches");
        sensitivityTrack?.Query<VisualElement>(className: "notch").ForEach(_sensitivityNotches.Add);
        volumeTrack?.Query<VisualElement>(className: "notch").ForEach(_volumeNotches.Add);
        _entranceElements.Add(_root.Q<VisualElement>("best-run-card"));
        _root.RegisterCallback<NavigationMoveEvent>(SuppressToolkitNavigation, TrickleDown.TrickleDown);
        _root.RegisterCallback<GeometryChangedEvent>(HandleRootGeometryChanged);
        ApplyResponsiveScale();
    }

    private void ClearCachedElements()
    {
        _mainRows.Clear();
        _settingsRows.Clear();
        _entranceElements.Clear();
        _sensitivityNotches.Clear();
        _volumeNotches.Clear();
    }

    private void BuildMainRows()
    {
        string[] rowNames = { "continue", "new-game", "leaderboards", "options", "quit" };
        for (int i = 0; i < rowNames.Length; i++)
        {
            Button row = _root.Q<Button>(rowNames[i]);
            if (row == null)
            {
                Debug.LogError($"Main Menu UI Toolkit document is missing row '{rowNames[i]}'.", this);
                enabled = false;
                return;
            }

            int index = i;
            bool available = index != 2;
            row.focusable = available;
            row.tabIndex = available ? index : -1;
            row.SetEnabled(available);
            if (available)
            {
                row.clicked += () => SubmitMainRow(index);
                RegisterRowInteraction(row, ReceiptPage.Main, index);
            }
            _mainRows.Add(row);
        }
    }

    private void BuildSettingsRows()
    {
        string[] rowNames = { "set-sensitivity", "set-volume", "set-invert", "set-back" };
        for (int i = 0; i < rowNames.Length; i++)
        {
            Button row = _root.Q<Button>(rowNames[i]);
            if (row == null)
            {
                Debug.LogError($"Main Menu UI Toolkit document is missing settings row '{rowNames[i]}'.", this);
                enabled = false;
                return;
            }

            int index = i;
            row.focusable = true;
            row.tabIndex = index;
            row.clicked += () => ActivateSetting(index, true);
            RegisterRowInteraction(row, ReceiptPage.Settings, index);
            _settingsRows.Add(row);
        }
    }

    private void RegisterRowInteraction(Button row, ReceiptPage page, int index)
    {
        row.RegisterCallback<PointerEnterEvent>(_ => HandlePointerEnter(page, index));
        row.RegisterCallback<FocusInEvent>(_ =>
        {
            if (!_transitioning && !_submitting && _activePage == page)
                SelectRow(page, index);
        });
        row.RegisterCallback<PointerDownEvent>(evt => HandlePointerDown(row, page, index, evt));
        row.RegisterCallback<PointerUpEvent>(_ => HandlePointerRelease(row));
        row.RegisterCallback<PointerCancelEvent>(_ => HandlePointerRelease(row));
        row.RegisterCallback<PointerCaptureOutEvent>(_ => HandlePointerRelease(row));
        row.RegisterCallback<PointerLeaveEvent>(_ => HandlePointerRelease(row));
    }

    private void ResetView()
    {
        KillAllTweens();
        _activePage = ReceiptPage.Main;
        _idleSeconds = 0f;
        _lastPointerPosition = ReadPointerPosition();
        _submitting = false;
        _transitioning = false;
        _pressedRow = null;
        _mainSelectedIndex = 0;
        _settingsSelectedIndex = 0;

        RestorePaperToFeedMask(_mainPaper);
        RestorePaperToFeedMask(_settingsPaper);
        ResetAllRows();
        SetPageInteractive(ReceiptPage.Main, true);
        SetPageInteractive(ReceiptPage.Settings, false);
        ParkPaper(_settingsPaper);
        RefreshBestRun();
        RefreshSettings(false);
        RefreshIdleTimer();
        SelectRow(ReceiptPage.Main, 0, false);
        _mainRows[0].Focus();

        if (!_entrancePlayed)
        {
            _entrancePlayed = true;
            StartCoroutine(PlayEntrance());
        }
        else
            SetEntranceElementsVisible();
    }

    private void ResetAllRows()
    {
        for (int i = 0; i < _mainRows.Count; i++)
            ResetRow(_mainRows[i]);
        for (int i = 0; i < _settingsRows.Count; i++)
            ResetRow(_settingsRows[i]);
    }

    private static void ResetRow(Button row)
    {
        ResetRowInteractionVisuals(row);
        row.RemoveFromClassList("is-selected");
        SetRowSelectionVisuals(row, false, false);
    }

    private IEnumerator PlayEntrance()
    {
        for (int i = 0; i < _entranceElements.Count; i++)
            _entranceElements[i]?.RemoveFromClassList("is-visible");
        ParkPaper(_mainPaper);
        yield return new WaitForSecondsRealtime(Mathf.Min(_entranceDelay, 0.28f));
        PlayReceiptFeed(_mainPaper, ReceiptFeedDuration);
        yield return new WaitForSecondsRealtime(0.52f);
        for (int i = 0; i < _entranceElements.Count; i++)
        {
            _entranceElements[i]?.AddToClassList("is-visible");
            yield return new WaitForSecondsRealtime(0.06f);
        }
    }

    private void SetEntranceElementsVisible()
    {
        SetPaperTransform(_mainPaper, 0f, 0f, 0f, 1f);
        for (int i = 0; i < _entranceElements.Count; i++)
            _entranceElements[i]?.AddToClassList("is-visible");
    }

    private void PlayReceiptFeed(VisualElement paper, float duration)
    {
        if (paper == null)
            return;
        KillReceiptFeedTween();
        SetPrinterFeeding(true);
        _receiptFeedTween = DOVirtual.Float(0f, 1f, duration, progress =>
        {
            float steppedProgress = Mathf.Floor(progress * 22f) / 22f;
            SetPaperTransform(paper, 0f, Mathf.Lerp(-ReceiptFeedDistance, 0f, steppedProgress), 0f, 1f);
        }).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() =>
        {
            if (paper != null)
                SetPaperTransform(paper, 0f, 0f, 0f, 1f);
            SetPrinterFeeding(false);
        });
    }

    private void BeginReceiptSwap(ReceiptPage targetPage)
    {
        if (_transitioning || targetPage == _activePage)
            return;

        _transitioning = true;
        _submitting = false;
        _pressedRow = null;
        KillReceiptFeedTween();
        KillTearSequence();

        ReceiptPage outgoingPage = _activePage;
        VisualElement outgoing = GetPaper(outgoingPage);
        VisualElement incoming = GetPaper(targetPage);
        if (targetPage == ReceiptPage.Settings)
            RefreshSettings(false);

        SetPageInteractive(outgoingPage, false);
        SetPageInteractive(targetPage, false);
        _feedMask.pickingMode = PickingMode.Ignore;
        MovePaperToFallLayer(outgoing);
        SetPaperTransform(outgoing, 0f, 0f, 0f, 1f);
        SetPaperTransform(incoming, 0f, -ReceiptFeedDistance, 0f, 1f);
        SetPrinterFeeding(true);

        if (_reduceMotion)
        {
            SetPaperTransform(incoming, 0f, 0f, 0f, 0f);
            _tearSequence = DOTween.Sequence().SetUpdate(true);
            _tearSequence.Join(TweenPaperOpacity(outgoing, 0f, 0.12f, Ease.Linear));
            _tearSequence.Join(TweenPaperOpacity(incoming, 1f, 0.12f, Ease.Linear));
            _tearSequence.OnComplete(() => CompleteReceiptSwap(outgoingPage, targetPage));
            return;
        }

        _tearSequence = DOTween.Sequence().SetUpdate(true);
        const float fallDriftDirection = -1f;
        float spinDirection = targetPage == ReceiptPage.Settings ? -1f : 1f;
        _tearSequence.Insert(0f, DOVirtual.Float(0f, 1f, 0.18f, progress =>
        {
            float kick = Mathf.Sin(progress * Mathf.PI * 3f) * 2f;
            if (_printerShell != null)
                _printerShell.style.translate = new Translate(0f, kick);
        }).SetEase(Ease.Linear));
        _tearSequence.Insert(0.03f, DOVirtual.Float(0f, 1f, 0.09f, progress =>
        {
            SetPaperTransform(outgoing,
                Mathf.Lerp(0f, fallDriftDirection * 8f, progress),
                Mathf.Lerp(0f, 10f, progress),
                Mathf.Lerp(0f, spinDirection * 2f, progress),
                1f);
        }).SetEase(Ease.OutQuad));
        const float fallDuration = 0.64f;
        _tearSequence.Insert(0.12f, DOVirtual.Float(0f, 1f, fallDuration, progress =>
        {
            float elapsed = progress * fallDuration;
            float x = fallDriftDirection * (8f + 120f * elapsed + 28f * elapsed * elapsed);
            float y = 10f + 45f * elapsed + 1700f * elapsed * elapsed;
            float rotation = spinDirection * (2f + 46f * elapsed + 12f * elapsed * elapsed);
            SetPaperTransform(outgoing, x, y, rotation, 1f);
        }).SetEase(Ease.Linear));
        _tearSequence.Insert(0.3f, DOVirtual.Float(0f, 1f, 0.62f, progress =>
        {
            float steppedProgress = Mathf.Floor(progress * 22f) / 22f;
            SetPaperTransform(incoming, 0f, Mathf.Lerp(-ReceiptFeedDistance, 0f, steppedProgress), 0f, 1f);
        }).SetEase(Ease.Linear));
        _tearSequence.Insert(0.92f, DOVirtual.Float(0f, 1f, 0.12f, progress =>
        {
            SetPaperTransform(incoming, 0f, Mathf.Sin(progress * Mathf.PI) * 4f, 0f, 1f);
        }).SetEase(Ease.OutQuad));
        _tearSequence.OnComplete(() => CompleteReceiptSwap(outgoingPage, targetPage));
    }

    private void CompleteReceiptSwap(ReceiptPage outgoingPage, ReceiptPage targetPage)
    {
        VisualElement outgoing = GetPaper(outgoingPage);
        VisualElement incoming = GetPaper(targetPage);
        ParkPaper(outgoing);
        RestorePaperToFeedMask(outgoing);
        SetPaperTransform(incoming, 0f, 0f, 0f, 1f);
        if (_printerShell != null)
            _printerShell.style.translate = new Translate(0f, 0f);

        _activePage = targetPage;
        SetPageInteractive(outgoingPage, false);
        SetPageInteractive(targetPage, true);
        _feedMask.pickingMode = PickingMode.Position;
        SetPrinterFeeding(false);
        _transitioning = false;

        if (targetPage == ReceiptPage.Settings)
        {
            SelectRow(ReceiptPage.Settings, 0, false);
            _settingsRows[0].Focus();
        }
        else
        {
            ResetRowInteractionVisuals(_mainRows[3]);
            SelectRow(ReceiptPage.Main, 3, false);
            _mainRows[3].Focus();
        }
    }

    private void SetPageInteractive(ReceiptPage page, bool interactive)
    {
        VisualElement paper = GetPaper(page);
        if (paper != null)
            paper.pickingMode = interactive ? PickingMode.Position : PickingMode.Ignore;

        List<Button> rows = GetRows(page);
        for (int i = 0; i < rows.Count; i++)
        {
            bool rowEnabled = interactive && (page != ReceiptPage.Main || i != 2);
            rows[i].SetEnabled(rowEnabled);
            rows[i].focusable = rowEnabled;
            rows[i].tabIndex = rowEnabled ? i : -1;
        }
    }

    private VisualElement GetPaper(ReceiptPage page) => page == ReceiptPage.Main ? _mainPaper : _settingsPaper;
    private List<Button> GetRows(ReceiptPage page) => page == ReceiptPage.Main ? _mainRows : _settingsRows;

    private static void SuppressToolkitNavigation(NavigationMoveEvent navigationEvent)
    {
        navigationEvent.PreventDefault();
        navigationEvent.StopImmediatePropagation();
    }

    private void HandleRootGeometryChanged(GeometryChangedEvent _) => ApplyResponsiveScale();

    private void ApplyResponsiveScale()
    {
        if (_root == null || _stage == null)
            return;
        float availableWidth = _root.contentRect.width;
        float availableHeight = _root.contentRect.height;
        if (availableWidth <= 0f || availableHeight <= 0f)
            return;
        float scale = Mathf.Min(1f, availableWidth / StageWidth, availableHeight / StageHeight);
        _stage.style.scale = new Scale(new Vector3(scale, scale, 1f));
    }

    private void HandlePointerEnter(ReceiptPage page, int index)
    {
        if (_transitioning || _submitting || page != _activePage)
            return;
        SelectRow(page, index);
        ResetIdleTimer();
    }

    private void HandlePointerDown(Button row, ReceiptPage page, int index, PointerDownEvent evt)
    {
        if (_transitioning || _submitting || page != _activePage || evt.button != 0 || !row.enabledInHierarchy)
            return;
        SelectRow(page, index);
        ResetIdleTimer();
        _pressedRow = row;
        _pointerScaleTween?.Kill();
        _pointerScaleTween = TweenScale(row, 0.96f, 0.08f, Ease.OutQuad).SetUpdate(true);
    }

    private void HandlePointerRelease(Button row)
    {
        if (_pressedRow != row)
            return;
        _pressedRow = null;
        _pointerScaleTween?.Kill();
        _pointerScaleTween = TweenScale(row, 1f, 0.11f, Ease.OutCubic).SetUpdate(true);
    }

    private void ResetIdleTimer()
    {
        _idleSeconds = 0f;
        RefreshIdleTimer();
    }

    private void Navigate(int direction)
    {
        if (_transitioning || _submitting)
            return;
        List<Button> rows = GetRows(_activePage);
        if (rows.Count == 0)
            return;
        int currentIndex = _activePage == ReceiptPage.Main ? _mainSelectedIndex : _settingsSelectedIndex;
        for (int step = 1; step <= rows.Count; step++)
        {
            int candidate = (currentIndex + direction * step + rows.Count) % rows.Count;
            if (!rows[candidate].enabledInHierarchy)
                continue;
            SelectRow(_activePage, candidate);
            rows[candidate].Focus();
            return;
        }
    }

    private void SelectRow(ReceiptPage page, int index, bool animate = true)
    {
        List<Button> rows = GetRows(page);
        if (page != _activePage || index < 0 || index >= rows.Count || !rows[index].enabledInHierarchy)
            return;
        int previousIndex = page == ReceiptPage.Main ? _mainSelectedIndex : _settingsSelectedIndex;
        if (previousIndex >= 0 && previousIndex < rows.Count && previousIndex != index)
        {
            rows[previousIndex].RemoveFromClassList("is-selected");
            SetRowSelectionVisuals(rows[previousIndex], false, animate);
        }
        if (page == ReceiptPage.Main)
            _mainSelectedIndex = index;
        else
            _settingsSelectedIndex = index;
        rows[index].AddToClassList("is-selected");
        SetRowSelectionVisuals(rows[index], true, animate);
    }

    private void SubmitActiveRow()
    {
        if (_activePage == ReceiptPage.Main)
            SubmitMainRow(_mainSelectedIndex);
        else
            ActivateSetting(_settingsSelectedIndex, true);
    }

    private void SubmitMainRow(int index)
    {
        if (_transitioning || _submitting || _activePage != ReceiptPage.Main || index < 0 || index >= _mainRows.Count || !_mainRows[index].enabledInHierarchy)
            return;
        _submitting = true;
        ResetIdleTimer();
        SelectRow(ReceiptPage.Main, index);
        PlaySubmitFeedback(_mainRows[index]);
        StartCoroutine(ExecuteMainActionAfterFeedback(index));
    }

    private void PlaySubmitFeedback(Button row)
    {
        VisualElement stamp = row.Q<VisualElement>(className: "row-stamp");
        _pressedRow = null;
        _pointerScaleTween?.Kill();
        _pointerScaleTween = null;
        _submitSequence?.Kill();
        if (stamp != null)
        {
            stamp.style.opacity = 0f;
            stamp.style.scale = new Scale(Vector3.one * 1.25f);
        }
        row.AddToClassList("is-submitting");
        _submitSequence = DOTween.Sequence().SetUpdate(true);
        _submitSequence.Join(TweenPressPulse(row));
        if (stamp != null)
        {
            _submitSequence.Insert(0.03f, TweenOpacity(stamp, 0.92f, 0.1f, Ease.OutQuad));
            _submitSequence.Insert(0.03f, TweenScale(stamp, 1f, 0.17f, Ease.OutCubic));
        }
    }

    private IEnumerator ExecuteMainActionAfterFeedback(int index)
    {
        yield return new WaitForSecondsRealtime(SubmitFeedbackDuration);
        if (_controller == null)
        {
            EndSubmit(index);
            yield break;
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
            case 3:
                EndSubmit(index);
                BeginReceiptSwap(ReceiptPage.Settings);
                break;
            case 4:
                _controller.QuitGame();
                EndSubmit(index);
                break;
        }
    }

    private void EndSubmit(int index)
    {
        if (index >= 0 && index < _mainRows.Count)
            ResetRowInteractionVisuals(_mainRows[index]);
        _submitSequence?.Kill();
        _submitSequence = null;
        _submitting = false;
    }

    private void ActivateSetting(int index, bool pulse)
    {
        if (_transitioning || _activePage != ReceiptPage.Settings || index < 0 || index >= _settingsRows.Count)
            return;
        SelectRow(ReceiptPage.Settings, index);
        ResetIdleTimer();
        _pressedRow = null;
        _pointerScaleTween?.Kill();
        _pointerScaleTween = null;
        if (pulse)
            PlaySettingPulse(_settingsRows[index]);
        switch (index)
        {
            case 0:
                AdjustSetting(0, 1, true);
                break;
            case 1:
                AdjustSetting(1, 1, true);
                break;
            case 2:
                if (_controller != null)
                    _controller.SetInvertCamera(!_controller.InvertCamera);
                RefreshSettings(true);
                break;
            case 3:
                BeginReceiptSwap(ReceiptPage.Main);
                break;
        }
    }

    private void AdjustSelectedSetting(int direction)
    {
        if (_settingsSelectedIndex <= 1)
            AdjustSetting(_settingsSelectedIndex, direction, false);
        else if (_settingsSelectedIndex == 2)
        {
            if (_controller != null)
                _controller.SetInvertCamera(!_controller.InvertCamera);
            RefreshSettings(true);
        }
    }

    private void AdjustSetting(int settingIndex, int direction, bool wrap)
    {
        if (_controller == null || (settingIndex != 0 && settingIndex != 1))
            return;
        float normalized = settingIndex == 0
            ? Mathf.InverseLerp(SensitivityMinimum, SensitivityMaximum, _controller.Sensitivity)
            : Mathf.Clamp01(_controller.Volume);
        int step = Mathf.RoundToInt(normalized * SettingStepCount) + direction;
        if (wrap)
            step = (step + SettingStepCount + 1) % (SettingStepCount + 1);
        else
            step = Mathf.Clamp(step, 0, SettingStepCount);
        float steppedValue = step / (float)SettingStepCount;
        if (settingIndex == 0)
            _controller.SetSensitivity(Mathf.Lerp(SensitivityMinimum, SensitivityMaximum, steppedValue));
        else
            _controller.SetVolume(steppedValue);
        RefreshSettings(true);
    }

    private void PlaySettingPulse(VisualElement row)
    {
        DOTween.Kill(row);
        Sequence pulse = DOTween.Sequence().SetUpdate(true).SetId(row);
        pulse.Append(TweenScale(row, 0.97f, 0.07f, Ease.OutQuad));
        pulse.Append(TweenScale(row, 1f, 0.1f, Ease.OutCubic));
    }

    private void RefreshSettings(bool animateCheck)
    {
        float sensitivity = _controller != null ? _controller.Sensitivity : 1f;
        float volume = _controller != null ? _controller.Volume : 0.5f;
        bool invert = _controller != null && _controller.InvertCamera;
        if (_sensitivityValueText != null)
            _sensitivityValueText.text = sensitivity.ToString("F2");
        if (_volumeValueText != null)
            _volumeValueText.text = Mathf.RoundToInt(volume * 100f) + "%";
        if (_invertValueText != null)
            _invertValueText.text = invert ? "ON" : "OFF";
        SetNotchFill(_sensitivityNotches, Mathf.RoundToInt(Mathf.InverseLerp(SensitivityMinimum, SensitivityMaximum, sensitivity) * SettingStepCount));
        SetNotchFill(_volumeNotches, Mathf.RoundToInt(Mathf.Clamp01(volume) * SettingStepCount));
        if (_invertCheck == null)
            return;
        DOTween.Kill(_invertCheck);
        if (animateCheck)
        {
            TweenScale(_invertCheck, invert ? 1f : 0f, invert ? 0.16f : 0.1f, Ease.OutCubic)
                .SetUpdate(true).SetId(_invertCheck);
        }
        else
        {
            _invertCheck.style.scale = new Scale(invert ? Vector3.one : Vector3.zero);
        }
    }

    private static void SetNotchFill(List<VisualElement> notches, int filledStep)
    {
        int filledCount = Mathf.Clamp(filledStep, 0, notches.Count);
        for (int i = 0; i < notches.Count; i++)
            notches[i].EnableInClassList("is-filled", i < filledCount);
    }

    private static void ResetRowInteractionVisuals(Button row)
    {
        row.RemoveFromClassList("is-submitting");
        row.style.scale = new Scale(Vector3.one);
        row.style.translate = new StyleTranslate(StyleKeyword.Null);
        VisualElement stamp = row.Q<VisualElement>(className: "row-stamp");
        if (stamp == null)
            return;
        stamp.style.opacity = 0f;
        stamp.style.scale = new Scale(Vector3.one * 1.25f);
    }

    private static void SetRowSelectionVisuals(Button row, bool selected, bool animate)
    {
        VisualElement highlighter = row.Q<VisualElement>(className: "row-highlighter");
        VisualElement caret = row.Q<VisualElement>(className: "row-caret");
        float targetScale = selected ? 1f : 0f;
        float targetOpacity = selected ? 1f : 0f;
        if (highlighter != null)
        {
            DOTween.Kill(highlighter);
            if (animate)
                TweenScaleX(highlighter, targetScale, selected ? 0.16f : 0.11f, Ease.OutQuad).SetUpdate(true).SetId(highlighter);
            else
                highlighter.style.scale = new Scale(new Vector3(targetScale, 1f, 1f));
        }
        if (caret == null)
            return;
        DOTween.Kill(caret);
        if (animate)
            TweenOpacity(caret, targetOpacity, 0.09f, Ease.Linear).SetUpdate(true).SetId(caret);
        else
            caret.style.opacity = targetOpacity;
    }

    private void RefreshBestRun()
    {
        bool hasCompletedRun = false;
        if (_gameSaveManager != null)
        {
            IReadOnlyList<LevelTimeEntry> entries = _gameSaveManager.LevelTimeEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsCompleted)
                {
                    hasCompletedRun = true;
                    break;
                }
            }
        }
        string bestRun = hasCompletedRun ? FormatElapsedTime(_gameSaveManager.TotalTime) : "--:--.--";
        if (_bestRunTagTimeText != null)
            _bestRunTagTimeText.text = bestRun;
        // The current save schema stores one aggregate run, not lifetime history.
        if (_playthroughCountText != null)
            _playthroughCountText.text = hasCompletedRun ? "1" : "0";
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

    private bool ReadInput(out int verticalDirection, out int horizontalDirection, out bool submitInput, out bool cancelInput)
    {
        verticalDirection = 0;
        horizontalDirection = 0;
        submitInput = false;
        cancelInput = false;
        bool anyInput = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            anyInput |= keyboard.anyKey.wasPressedThisFrame;
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                verticalDirection = -1;
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                verticalDirection = 1;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                horizontalDirection = -1;
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                horizontalDirection = 1;
            submitInput = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
            cancelInput = keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame;
        }
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 pointerPosition = mouse.position.ReadValue();
            bool pointerMoved = (pointerPosition - _lastPointerPosition).sqrMagnitude > 0.01f;
            _lastPointerPosition = pointerPosition;
            anyInput |= pointerMoved || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0.01f;
        }
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 leftStick = gamepad.leftStick.ReadValue();
            anyInput |= leftStick.sqrMagnitude > 0.12f || gamepad.rightStick.ReadValue().sqrMagnitude > 0.12f;
            for (int i = 0; i < gamepad.allControls.Count; i++)
            {
                if (gamepad.allControls[i] is ButtonControl button && button.wasPressedThisFrame)
                {
                    anyInput = true;
                    break;
                }
            }
            if (gamepad.dpad.up.wasPressedThisFrame)
                verticalDirection = -1;
            else if (gamepad.dpad.down.wasPressedThisFrame)
                verticalDirection = 1;
            if (gamepad.dpad.left.wasPressedThisFrame)
                horizontalDirection = -1;
            else if (gamepad.dpad.right.wasPressedThisFrame)
                horizontalDirection = 1;
            if (Mathf.Abs(leftStick.y) > 0.55f && Mathf.Abs(leftStick.y) >= Mathf.Abs(leftStick.x))
            {
                if (!_verticalStickActive || Time.unscaledTime >= _nextVerticalStickTime)
                {
                    verticalDirection = leftStick.y > 0f ? -1 : 1;
                    _nextVerticalStickTime = Time.unscaledTime + (_verticalStickActive ? 0.12f : 0.42f);
                }
                _verticalStickActive = true;
            }
            else
            {
                _verticalStickActive = false;
            }
            if (Mathf.Abs(leftStick.x) > 0.55f && Mathf.Abs(leftStick.x) > Mathf.Abs(leftStick.y))
            {
                if (!_horizontalStickActive || Time.unscaledTime >= _nextHorizontalStickTime)
                {
                    horizontalDirection = leftStick.x > 0f ? 1 : -1;
                    _nextHorizontalStickTime = Time.unscaledTime + (_horizontalStickActive ? 0.12f : 0.42f);
                }
                _horizontalStickActive = true;
            }
            else
            {
                _horizontalStickActive = false;
            }
            submitInput |= gamepad.buttonSouth.wasPressedThisFrame;
            cancelInput |= gamepad.buttonEast.wasPressedThisFrame;
        }
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            anyInput = true;
        return anyInput;
    }

    private void SetPrinterFeeding(bool feeding)
    {
        _printerStatusLight?.EnableInClassList("is-feeding", feeding);
        _printerStatusHalo?.EnableInClassList("is-feeding", feeding);
    }

    private void ParkPaper(VisualElement paper) => SetPaperTransform(paper, 0f, -ReceiptFeedDistance, 0f, 1f);

    private void SetPaperTransform(VisualElement paper, float x, float y, float rotation, float opacity)
    {
        if (paper == null)
            return;
        paper.style.translate = new Translate(x, y);
        paper.style.rotate = new Rotate(new Angle(rotation, AngleUnit.Degree));
        paper.style.opacity = opacity;
        VisualElement shadow = GetPaperShadow(paper);
        if (shadow == null)
            return;
        shadow.style.translate = new Translate(x, y);
        shadow.style.rotate = new Rotate(new Angle(rotation, AngleUnit.Degree));
        shadow.style.opacity = opacity;
    }

    private VisualElement GetPaperShadow(VisualElement paper) => paper == _mainPaper ? _mainPaperShadow : _settingsPaperShadow;

    private void MovePaperToFallLayer(VisualElement paper)
    {
        if (paper == null || _fallLayer == null)
            return;
        VisualElement shadow = GetPaperShadow(paper);
        if (shadow != null)
            _fallLayer.Add(shadow);
        _fallLayer.Add(paper);
    }

    private void RestorePaperToFeedMask(VisualElement paper)
    {
        if (paper == null || _feedMask == null || paper.parent == _feedMask)
            return;
        VisualElement shadow = GetPaperShadow(paper);
        if (shadow != null)
            _feedMask.Add(shadow);
        _feedMask.Add(paper);
    }

    private Tween TweenPaperOpacity(VisualElement paper, float target, float duration, Ease ease)
    {
        float start = paper.style.opacity.value;
        return DOVirtual.Float(start, target, duration, opacity =>
        {
            paper.style.opacity = opacity;
            VisualElement shadow = GetPaperShadow(paper);
            if (shadow != null)
                shadow.style.opacity = opacity;
        }).SetEase(ease);
    }

    private void KillAllTweens()
    {
        _pointerScaleTween?.Kill();
        _pointerScaleTween = null;
        _submitSequence?.Kill();
        _submitSequence = null;
        KillReceiptFeedTween();
        KillTearSequence();
        for (int i = 0; i < _mainRows.Count; i++)
        {
            DOTween.Kill(_mainRows[i]);
            DOTween.Kill(_mainRows[i].Q<VisualElement>(className: "row-highlighter"));
            DOTween.Kill(_mainRows[i].Q<VisualElement>(className: "row-caret"));
        }
        for (int i = 0; i < _settingsRows.Count; i++)
        {
            DOTween.Kill(_settingsRows[i]);
            DOTween.Kill(_settingsRows[i].Q<VisualElement>(className: "row-highlighter"));
            DOTween.Kill(_settingsRows[i].Q<VisualElement>(className: "row-caret"));
        }
        DOTween.Kill(_invertCheck);
        SetPrinterFeeding(false);
    }

    private void KillReceiptFeedTween()
    {
        _receiptFeedTween?.Kill();
        _receiptFeedTween = null;
    }

    private void KillTearSequence()
    {
        _tearSequence?.Kill();
        _tearSequence = null;
    }

    private static Tween TweenScale(VisualElement element, float target, float duration, Ease ease)
    {
        float start = element.style.scale.value.value.x;
        return DOVirtual.Float(start, target, duration, value =>
        {
            element.style.scale = new Scale(new Vector3(value, value, 1f));
        }).SetEase(ease);
    }

    private static Tween TweenPressPulse(VisualElement element)
    {
        return DOVirtual.Float(0f, 1f, 0.24f, progress =>
        {
            float scale;
            if (progress <= 1f / 3f)
            {
                float downProgress = progress * 3f;
                float eased = 1f - (1f - downProgress) * (1f - downProgress);
                scale = Mathf.Lerp(1f, 0.94f, eased);
            }
            else
            {
                float upProgress = (progress - 1f / 3f) * 1.5f;
                float eased = 1f - Mathf.Pow(1f - upProgress, 3f);
                scale = Mathf.Lerp(0.94f, 1f, eased);
            }
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }).SetEase(Ease.Linear);
    }

    private static Tween TweenOpacity(VisualElement element, float target, float duration, Ease ease)
    {
        float start = element.style.opacity.value;
        return DOVirtual.Float(start, target, duration, value => element.style.opacity = value).SetEase(ease);
    }

    private static Tween TweenScaleX(VisualElement element, float target, float duration, Ease ease)
    {
        float start = element.style.scale.value.value.x;
        return DOVirtual.Float(start, target, duration, value =>
        {
            element.style.scale = new Scale(new Vector3(value, 1f, 1f));
        }).SetEase(ease);
    }

    private static Vector2 ReadPointerPosition() => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
}
