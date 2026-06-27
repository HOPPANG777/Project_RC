using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

//==============================================================================
// StockRhythmStageView
//==============================================================================
// Creates and updates generated world objects, chart visuals, and HUD controls.
//==============================================================================
internal sealed class StockRhythmStageView
{
    // Root dependency injected by StockRhythmController.
    private GameRoot _gameRoot;

    // Runtime-generated assets used by every generated view object.
    private StockRhythmRuntimeAssets _assets;

    // Pause button callback owned by the controller.
    private UnityAction _onPause;

    // BUY action callback owned by the controller.
    private UnityAction _onBuy;

    // SELL action callback owned by the controller.
    private UnityAction _onSell;

    // Restart callback owned by the controller.
    private UnityAction _onRestart;

    // Main camera used for the prototype scene.
    private Camera _mainCamera;

    // Runtime HUD canvas.
    private Canvas _canvas;

    // Root object for generated world visuals.
    private GameObject _worldRoot;

    // Root object for generated scrolling chart visuals.
    private GameObject _chartRoot;

    // Root object for the player timing marker.
    private GameObject _markerRoot;

    // Ring renderer on the timing marker.
    private SpriteRenderer _markerRing;

    // Main judgement beam renderer.
    private LineRenderer _judgeBeam;

    // Wide glow renderer behind the judgement beam.
    private LineRenderer _judgeGlow;

    // World-space judgement text near the timing line.
    private TextMesh _judgeTextWorld;

    // Score HUD text.
    private Text _scoreText;

    // Combo HUD text.
    private Text _comboText;

    // Accuracy HUD text.
    private Text _accuracyText;

    // Timer HUD text.
    private Text _timerText;

    // Central judgement HUD text.
    private Text _judgeText;

    // Fever label text.
    private Text _feverText;

    // Fever fill image.
    private Image _feverFill;

    // Result panel root object.
    private GameObject _resultPanel;

    // Result panel title text.
    private Text _resultTitleText;

    // Result panel body text.
    private Text _resultBodyText;

    // Pause button text.
    private Text _pauseButtonText;

    // Injects dependencies and controller-owned callbacks.
    public void Construct(
        GameRoot gameRoot,
        StockRhythmRuntimeAssets assets,
        UnityAction onPause,
        UnityAction onBuy,
        UnityAction onSell,
        UnityAction onRestart)
    {
        _gameRoot = gameRoot;
        _assets = assets;
        _onPause = onPause;
        _onBuy = onBuy;
        _onSell = onSell;
        _onRestart = onRestart;
    }

    // Creates generated camera, world visuals, and UI objects.
    public void Initialize(System.Random random)
    {
        ConfigureCamera();
        CreateWorld(random);
        CreateUi();
    }

    // Creates note, candle, and optional segment visuals for one note model.
    public void CreateNoteVisuals(StockRhythmNote note, System.Random random)
    {
        note.NoteView = CreateNoteView(note);
        note.CandleView = CreateCandle(note, random);
        if (note.Previous != null)
        {
            note.Segment = CreateChartSegment(note);
        }
    }

    // Updates positions and alpha values for notes, candles, and chart segments.
    public void UpdateNotes(IReadOnlyList<StockRhythmNote> notes, float gameTime)
    {
        for (int i = 0; i < notes.Count; i++)
        {
            StockRhythmNote note = notes[i];
            float x = NoteX(note, gameTime);
            Vector3 position = new Vector3(x, note.Y, 0f);

            if (note.NoteView != null && note.NoteView.Root != null)
            {
                note.NoteView.Root.transform.position = position;
                float alpha = note.Judged ? 0.38f : Mathf.Clamp01((x + 6.4f) / 1.0f);
                SetNoteAlpha(note.NoteView, alpha);
            }

            if (note.CandleView != null && note.CandleView.Root != null)
            {
                note.CandleView.Root.transform.position = new Vector3(x, note.Y - 0.92f, 0f);
                note.CandleView.Wick.SetPosition(0, new Vector3(x, note.Y - 1.28f, 0f));
                note.CandleView.Wick.SetPosition(1, new Vector3(x, note.Y - 0.56f, 0f));
                note.CandleView.Body.transform.position = new Vector3(x, note.Y - 0.92f, 0f);
            }

            if (note.Segment != null && note.Previous != null)
            {
                note.Segment.SetPosition(0, new Vector3(NoteX(note.Previous, gameTime), note.Previous.Y, 0f));
                note.Segment.SetPosition(1, position);
            }
        }
    }

    // Plays the visual metronome pulse on the judgement line.
    public void UpdateBeat(float gameTime, bool isFeverActive)
    {
        float pulse = 1f + Mathf.PingPong(gameTime * 8f, 0.35f);
        _judgeBeam.widthMultiplier = 0.05f * pulse;
        _judgeGlow.widthMultiplier = isFeverActive ? 0.36f : 0.24f;
    }

    // Moves the timing marker toward the last successfully hit note.
    public void UpdateMarker(float markerY, ref float markerVelocity, bool isFeverActive, bool instant)
    {
        if (instant)
        {
            _markerRoot.transform.position = new Vector3(StockRhythmConstants.JUDGE_X, markerY, 0f);
            return;
        }

        float current = _markerRoot.transform.position.y;
        float smooth = Mathf.SmoothDamp(current, markerY, ref markerVelocity, 0.08f, 20f);
        _markerRoot.transform.position = new Vector3(StockRhythmConstants.JUDGE_X, smooth, 0f);
        _markerRing.color = isFeverActive ? new Color(1f, 0.82f, 0.08f, 1f) : new Color(0.38f, 1f, 1f, 1f);
    }

    // Refreshes score, combo, judgement, timer, and fever HUD elements.
    public void UpdateHud(StockRhythmStageState state)
    {
        _scoreText.text = "SCORE\n" + state.Score.ToString("N0");
        _comboText.text = state.Combo > 0 ? state.Combo.ToString() + " COMBO" : "0 COMBO";
        _accuracyText.text = "ACC " + state.AccuracyPercent.ToString("0.0") + "%  HIT " + state.Hits + " / MISS " + state.HudMisses;
        _timerText.text = Mathf.CeilToInt(Mathf.Max(0f, StockRhythmConstants.STAGE_DURATION - state.GameTime)).ToString("00") + "s";
        _judgeText.text = state.LastJudge;
        _judgeTextWorld.text = state.LastJudge;
        _judgeText.color = state.IsFeverActive ? new Color(1f, 0.85f, 0.1f, 1f) : new Color(1f, 0.34f, 0.78f, 1f);
        _feverText.text = state.IsFeverActive ? "FEVER x2" : "FEVER";

        RectTransform fillRect = _feverFill.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(504f * Mathf.Clamp01(state.Fever), -16f);
    }

    // Updates the pause button label.
    public void SetPauseButtonLabel(string label)
    {
        _pauseButtonText.text = label;
    }

    // Hides the end-of-stage result panel.
    public void HideResult()
    {
        _resultPanel.SetActive(false);
    }

    // Displays the end-of-stage result panel.
    public void ShowResult(StockRhythmResultSnapshot result)
    {
        _resultTitleText.text = "STAGE CLEAR";
        _resultBodyText.text =
            "Final Score  " + result.Score.ToString("N0") + "\n" +
            "Accuracy     " + (result.Accuracy * 100f).ToString("0.0") + "%\n" +
            "Max Combo    " + result.MaxCombo.ToString("N0") + "\n" +
            "Perfect / Great / Good  " + result.Perfects + " / " + result.Greats + " / " + result.Goods + "\n" +
            "Virtual Return  " + (result.ReturnPercent * 100f).ToString("+0.0;-0.0;0.0") + "%\n" +
            "Seed Money   " + result.SeedMoney.ToString("N0") + "\n\n" +
            "VIRTUAL GAME RESULT - NOT INVESTMENT ADVICE";
        _resultPanel.SetActive(true);
    }

    // Destroys every generated object owned by one note.
    public void DestroyNoteVisuals(StockRhythmNote note)
    {
        if (note.NoteView != null && note.NoteView.Root != null)
        {
            Object.Destroy(note.NoteView.Root);
        }

        if (note.CandleView != null && note.CandleView.Root != null)
        {
            Object.Destroy(note.CandleView.Root);
        }

        if (note.Segment != null)
        {
            Object.Destroy(note.Segment.gameObject);
        }
    }

    // Applies a simple visual fade after judging a note.
    public void FadeNote(StockRhythmNote note, Color flashColor)
    {
        if (note.NoteView == null)
        {
            return;
        }

        note.NoteView.Panel.color = Color.Lerp(note.NoteView.Panel.color, flashColor, 0.5f);
        note.NoteView.Border.color = new Color(1f, 1f, 1f, 0.35f);
    }

    // Configures or creates the main camera.
    private void ConfigureCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            _mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        _mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        _mainCamera.orthographic = true;
        _mainCamera.orthographicSize = 5.2f;
        _mainCamera.clearFlags = CameraClearFlags.SolidColor;
        _mainCamera.backgroundColor = new Color(0.018f, 0.017f, 0.041f, 1f);
    }

    // Creates world roots and static stage visuals.
    private void CreateWorld(System.Random random)
    {
        _worldRoot = new GameObject("World");
        _worldRoot.transform.SetParent(_gameRoot.transform, false);

        _chartRoot = new GameObject("Scrolling Chart");
        _chartRoot.transform.SetParent(_worldRoot.transform, false);

        CreateBackground(random);
        CreateJudgeLine();
        CreateMarker();
        CreateWorldLabels();
    }

    // Creates grid, skyline, and neon background objects.
    private void CreateBackground(System.Random random)
    {
        for (int i = 0; i <= 18; i++)
        {
            float x = -8.5f + i * 0.95f;
            LineRenderer line = CreateLine("Grid V", new Color(0.09f, 0.28f, 0.55f, 0.17f), 0.012f, -30);
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(x, -2.5f, 0f));
            line.SetPosition(1, new Vector3(x, 3.5f, 0f));
        }

        for (int i = 0; i <= 8; i++)
        {
            float y = -2.4f + i * 0.75f;
            LineRenderer line = CreateLine("Grid H", new Color(0.09f, 0.28f, 0.55f, 0.22f), 0.012f, -30);
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(-8.7f, y, 0f));
            line.SetPosition(1, new Vector3(8.7f, y, 0f));
        }

        for (int i = 0; i < 28; i++)
        {
            float x = -8.8f + i * 0.68f;
            float height = 0.25f + (float)random.NextDouble() * 1.0f;
            SpriteRenderer block = CreateSpriteRenderer("City Block", _assets.RectSprite, new Color(0.018f, 0.045f, 0.085f, 0.95f), -40);
            block.transform.position = new Vector3(x, -3.08f + height * 0.5f, 0f);
            block.transform.localScale = new Vector3(0.42f, height, 1f);
        }

        for (int i = 0; i < 36; i++)
        {
            Color color = i % 2 == 0
                ? new Color(0f, 0.9f, 0.65f, 0.55f)
                : new Color(1f, 0.1f, 0.72f, 0.5f);
            SpriteRenderer light = CreateSpriteRenderer("Neon Pixel", _assets.RectSprite, color, -20);
            light.transform.position = new Vector3(-8.3f + (float)random.NextDouble() * 16.6f, -2.7f + (float)random.NextDouble() * 6.4f, 0f);
            light.transform.localScale = new Vector3(0.045f, 0.16f, 1f);
        }
    }

    // Creates the vertical timing judgement line.
    private void CreateJudgeLine()
    {
        _judgeGlow = CreateLine("Judge Glow", new Color(1f, 0.02f, 0.82f, 0.4f), 0.24f, 10);
        _judgeGlow.positionCount = 2;
        _judgeGlow.SetPosition(0, new Vector3(StockRhythmConstants.JUDGE_X, -2.42f, 0f));
        _judgeGlow.SetPosition(1, new Vector3(StockRhythmConstants.JUDGE_X, 3.52f, 0f));

        _judgeBeam = CreateLine("Judge Beam", new Color(1f, 0.7f, 1f, 0.95f), 0.055f, 11);
        _judgeBeam.positionCount = 2;
        _judgeBeam.SetPosition(0, new Vector3(StockRhythmConstants.JUDGE_X, -2.42f, 0f));
        _judgeBeam.SetPosition(1, new Vector3(StockRhythmConstants.JUDGE_X, 3.52f, 0f));
    }

    // Creates the player timing marker.
    private void CreateMarker()
    {
        _markerRoot = new GameObject("Player Timing Marker");
        _markerRoot.transform.SetParent(_worldRoot.transform, false);

        SpriteRenderer glow = CreateSpriteRenderer("Marker Glow", _assets.SoftCircleSprite, new Color(0f, 0.9f, 1f, 0.28f), 20);
        glow.transform.SetParent(_markerRoot.transform, false);
        glow.transform.localScale = Vector3.one * 1.35f;

        _markerRing = CreateSpriteRenderer("Marker Ring", _assets.RingSprite, new Color(0.38f, 1f, 1f, 1f), 21);
        _markerRing.transform.SetParent(_markerRoot.transform, false);
        _markerRing.transform.localScale = Vector3.one * 0.72f;

        SpriteRenderer markerCore = CreateSpriteRenderer("Marker Core", _assets.CircleSprite, new Color(0.02f, 0.015f, 0.05f, 1f), 22);
        markerCore.transform.SetParent(_markerRoot.transform, false);
        markerCore.transform.localScale = Vector3.one * 0.34f;
    }

    // Creates world-space stage title and judgement labels.
    private void CreateWorldLabels()
    {
        TextMesh centerTitle = CreateWorldText("SOMSOMG", new Vector3(0f, 4.08f, 0f), 0.16f, new Color(1f, 0.88f, 1f, 1f), 35);
        centerTitle.fontStyle = FontStyle.Bold;

        TextMesh subtitle = CreateWorldText("VIRTUAL STOCK RHYTHM", new Vector3(0f, 3.72f, 0f), 0.055f, new Color(0.5f, 1f, 0.95f, 0.72f), 35);
        subtitle.fontStyle = FontStyle.Bold;

        _judgeTextWorld = CreateWorldText("READY", new Vector3(StockRhythmConstants.JUDGE_X + 0.72f, 2.86f, 0f), 0.105f, new Color(1f, 0.12f, 0.72f, 1f), 36);
        _judgeTextWorld.fontStyle = FontStyle.Bold;
    }

    // Creates the screen-space HUD canvas and all UI controls.
    private void CreateUi()
    {
        _canvas = new GameObject("HUD Canvas").AddComponent<Canvas>();
        _canvas.transform.SetParent(_gameRoot.transform, false);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        CanvasScaler scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _canvas.gameObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        _scoreText = CreateUiText("Score", _canvas.transform, new Vector2(28f, -24f), new Vector2(0f, 1f), new Vector2(0f, 1f), 46, TextAnchor.UpperLeft, new Color(0.72f, 0.94f, 1f, 1f));
        _comboText = CreateUiText("Combo", _canvas.transform, new Vector2(28f, -88f), new Vector2(0f, 1f), new Vector2(0f, 1f), 42, TextAnchor.UpperLeft, new Color(1f, 0.2f, 0.76f, 1f));
        _accuracyText = CreateUiText("Accuracy", _canvas.transform, new Vector2(28f, -142f), new Vector2(0f, 1f), new Vector2(0f, 1f), 26, TextAnchor.UpperLeft, new Color(0.64f, 1f, 0.86f, 0.86f));
        _timerText = CreateUiText("Timer", _canvas.transform, new Vector2(-32f, -28f), new Vector2(1f, 1f), new Vector2(1f, 1f), 34, TextAnchor.UpperRight, new Color(0.9f, 0.95f, 1f, 0.95f));
        _judgeText = CreateUiText("Judge", _canvas.transform, new Vector2(0f, -148f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 46, TextAnchor.UpperCenter, new Color(1f, 0.34f, 0.78f, 1f));
        _judgeText.fontStyle = FontStyle.Bold;

        Button pauseButton = CreateTopButton("II", new Vector2(-40f, -36f), _onPause);
        _pauseButtonText = pauseButton.GetComponentInChildren<Text>();

        CreateActionButton("BUY", "L SHIFT", StockRhythmTradeAction.BUY, new Vector2(168f, 92f), new Color(0.06f, 0.82f, 0.24f, 1f), _assets.UpTriangleSprite);
        CreateActionButton("SELL", "R SHIFT", StockRhythmTradeAction.SELL, new Vector2(-168f, 92f), new Color(0.95f, 0.1f, 0.12f, 1f), _assets.DownTriangleSprite);
        CreateFeverBar();
        CreateResultPanel();
    }

    // Ensures UI buttons work with the new Input System.
    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        InputSystemUIInputModule module = eventSystem.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    // Creates one large BUY or SELL action button.
    private void CreateActionButton(string label, string keyHint, StockRhythmTradeAction action, Vector2 anchoredPosition, Color color, Sprite icon)
    {
        GameObject root = new GameObject(label + " Button");
        root.transform.SetParent(_canvas.transform, false);
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        rectTransform.anchorMin = action == StockRhythmTradeAction.BUY ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.pivot = action == StockRhythmTradeAction.BUY ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(260f, 92f);

        Image image = root.AddComponent<Image>();
        image.sprite = _assets.RectSprite;
        image.color = color;
        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(action == StockRhythmTradeAction.BUY ? _onBuy : _onSell);

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(46f, 0f);
        iconRect.sizeDelta = new Vector2(52f, 52f);
        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.color = Color.white;

        Text labelText = CreateUiText(label, root.transform, new Vector2(34f, 10f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 44, TextAnchor.MiddleCenter, Color.white);
        labelText.fontStyle = FontStyle.Bold;
        labelText.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 58f);

        Text hintText = CreateUiText(keyHint, root.transform, new Vector2(34f, -30f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 20, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.82f));
        hintText.fontStyle = FontStyle.Bold;
        hintText.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 32f);
    }

    // Creates the pause button in the top-right corner.
    private Button CreateTopButton(string label, Vector2 anchoredPosition, UnityAction onClick)
    {
        GameObject root = new GameObject("Top Button");
        root.transform.SetParent(_canvas.transform, false);
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(64f, 64f);

        Image image = root.AddComponent<Image>();
        image.sprite = _assets.RectSprite;
        image.color = new Color(0.04f, 0.05f, 0.11f, 0.9f);
        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.7f, 0.95f, 1f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = CreateUiText(label, root.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 30, TextAnchor.MiddleCenter, Color.white);
        text.fontStyle = FontStyle.Bold;
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
        return button;
    }

    // Creates the fever gauge at the bottom of the screen.
    private void CreateFeverBar()
    {
        GameObject root = new GameObject("Fever Bar");
        root.transform.SetParent(_canvas.transform, false);
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 50f);
        rectTransform.sizeDelta = new Vector2(520f, 52f);

        Image background = root.AddComponent<Image>();
        background.sprite = _assets.RectSprite;
        background.color = new Color(0.1f, 0.02f, 0.16f, 0.92f);
        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0f, 0.9f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(root.transform, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(8f, 0f);
        fillRect.sizeDelta = new Vector2(0f, -16f);
        _feverFill = fillObject.AddComponent<Image>();
        _feverFill.sprite = _assets.RectSprite;
        _feverFill.color = new Color(1f, 0.77f, 0.08f, 1f);

        _feverText = CreateUiText("FEVER", root.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 28, TextAnchor.MiddleCenter, Color.white);
        _feverText.fontStyle = FontStyle.Bold;
        _feverText.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 52f);
    }

    // Creates the end-of-stage result panel.
    private void CreateResultPanel()
    {
        _resultPanel = new GameObject("Result Panel");
        _resultPanel.transform.SetParent(_canvas.transform, false);
        RectTransform rectTransform = _resultPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(700f, 470f);

        Image image = _resultPanel.AddComponent<Image>();
        image.sprite = _assets.RectSprite;
        image.color = new Color(0.025f, 0.021f, 0.065f, 0.96f);
        Outline outline = _resultPanel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.1f, 0.75f, 0.95f);
        outline.effectDistance = new Vector2(4f, -4f);

        _resultTitleText = CreateUiText("STAGE CLEAR", _resultPanel.transform, new Vector2(0f, -36f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 50, TextAnchor.UpperCenter, new Color(1f, 0.86f, 1f, 1f));
        _resultTitleText.fontStyle = FontStyle.Bold;
        _resultTitleText.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 70f);

        _resultBodyText = CreateUiText("", _resultPanel.transform, new Vector2(0f, -116f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 30, TextAnchor.UpperCenter, new Color(0.78f, 0.96f, 1f, 1f));
        _resultBodyText.lineSpacing = 1.15f;
        _resultBodyText.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 230f);

        Button restartButton = CreatePanelButton(_resultPanel.transform, "RESTART", new Vector2(0f, 56f), new Color(0.12f, 0.88f, 0.45f, 1f));
        restartButton.onClick.AddListener(_onRestart);
        _resultPanel.SetActive(false);
    }

    // Creates one button inside the result panel.
    private Button CreatePanelButton(Transform parent, string label, Vector2 anchoredPosition, Color color)
    {
        GameObject root = new GameObject(label + " Button");
        root.transform.SetParent(parent, false);
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(260f, 72f);

        Image image = root.AddComponent<Image>();
        image.sprite = _assets.RectSprite;
        image.color = color;
        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateUiText(label, root.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 34, TextAnchor.MiddleCenter, Color.white);
        text.fontStyle = FontStyle.Bold;
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 72f);
        return button;
    }

    // Creates the visible note marker for a chart note.
    private StockRhythmNoteView CreateNoteView(StockRhythmNote note)
    {
        GameObject root = new GameObject(note.Action + " Note");
        root.transform.SetParent(_chartRoot.transform, false);

        Color main = note.Action == StockRhythmTradeAction.BUY
            ? new Color(0.18f, 0.95f, 0.33f, 1f)
            : new Color(1f, 0.19f, 0.2f, 1f);

        SpriteRenderer border = CreateSpriteRenderer("Border", _assets.RectSprite, Color.white, 25);
        border.transform.SetParent(root.transform, false);
        border.transform.localScale = new Vector3(0.74f, 0.74f, 1f);

        SpriteRenderer panel = CreateSpriteRenderer("Panel", _assets.RectSprite, main, 26);
        panel.transform.SetParent(root.transform, false);
        panel.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

        Sprite iconSprite = note.Action == StockRhythmTradeAction.BUY ? _assets.UpTriangleSprite : _assets.DownTriangleSprite;
        SpriteRenderer icon = CreateSpriteRenderer("Icon", iconSprite, Color.white, 27);
        icon.transform.SetParent(root.transform, false);
        icon.transform.localPosition = new Vector3(0f, 0.085f, 0f);
        icon.transform.localScale = Vector3.one * 0.32f;

        TextMesh label = CreateWorldText(note.Action == StockRhythmTradeAction.BUY ? "BUY" : "SELL", Vector3.zero, 0.035f, Color.white, 28);
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        label.fontStyle = FontStyle.Bold;

        return new StockRhythmNoteView
        {
            Root = root,
            Panel = panel,
            Border = border,
            Icon = icon,
            Label = label
        };
    }

    // Creates a small candle visual under a chart note.
    private StockRhythmCandleView CreateCandle(StockRhythmNote note, System.Random random)
    {
        GameObject root = new GameObject("Candle");
        root.transform.SetParent(_chartRoot.transform, false);
        Color color = note.Action == StockRhythmTradeAction.BUY
            ? new Color(0.26f, 1f, 0.66f, 0.86f)
            : new Color(1f, 0.24f, 0.28f, 0.86f);

        SpriteRenderer body = CreateSpriteRenderer("Body", _assets.RectSprite, color, 5);
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.07f, 0.32f + (float)random.NextDouble() * 0.2f, 1f);

        LineRenderer wick = CreateLine("Wick", color, 0.018f, 4);
        wick.transform.SetParent(root.transform, false);
        wick.positionCount = 2;

        return new StockRhythmCandleView
        {
            Root = root,
            Body = body,
            Wick = wick
        };
    }

    // Creates a colored line segment between two chart notes.
    private LineRenderer CreateChartSegment(StockRhythmNote note)
    {
        Color color = note.Action == StockRhythmTradeAction.BUY
            ? new Color(0.18f, 1f, 0.55f, 0.92f)
            : new Color(1f, 0.17f, 0.22f, 0.92f);
        LineRenderer segment = CreateLine("Chart Segment", color, 0.06f, 6);
        segment.transform.SetParent(_chartRoot.transform, false);
        segment.positionCount = 2;
        return segment;
    }

    // Creates configured UI text under a parent RectTransform.
    private Text CreateUiText(string name, Transform parent, Vector2 anchoredPosition, Vector2 anchor, Vector2 pivot, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(520f, 80f);

        Text text = root.AddComponent<Text>();
        text.font = _assets.UiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    // Creates configured world-space TextMesh.
    private TextMesh CreateWorldText(string text, Vector3 position, float characterSize, Color color, int sortingOrder)
    {
        GameObject root = new GameObject(text + " Text");
        root.transform.SetParent(_worldRoot != null ? _worldRoot.transform : null, false);
        root.transform.position = position;

        TextMesh textMesh = root.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.font = _assets.UiFont;
        textMesh.fontSize = 64;
        textMesh.characterSize = characterSize;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = color;

        MeshRenderer renderer = root.GetComponent<MeshRenderer>();
        renderer.sortingOrder = sortingOrder;
        return textMesh;
    }

    // Creates a SpriteRenderer and parents it under the world root by default.
    private SpriteRenderer CreateSpriteRenderer(string name, Sprite sprite, Color color, int sortingOrder)
    {
        GameObject root = new GameObject(name);
        if (_worldRoot != null)
        {
            root.transform.SetParent(_worldRoot.transform, false);
        }

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    // Creates a configured LineRenderer.
    private LineRenderer CreateLine(string name, Color color, float width, int sortingOrder)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(_worldRoot != null ? _worldRoot.transform : null, false);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.material = _assets.LineMaterial;
        line.startColor = color;
        line.endColor = color;
        line.widthMultiplier = width;
        line.positionCount = 0;
        line.useWorldSpace = true;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.sortingOrder = sortingOrder;
        return line;
    }

    // Applies alpha to every renderer in a note view.
    private static void SetNoteAlpha(StockRhythmNoteView noteView, float alpha)
    {
        Color panel = noteView.Panel.color;
        panel.a = alpha;
        noteView.Panel.color = panel;

        Color border = noteView.Border.color;
        border.a = alpha;
        noteView.Border.color = border;

        Color icon = noteView.Icon.color;
        icon.a = alpha;
        noteView.Icon.color = icon;

        Color label = noteView.Label.color;
        label.a = alpha;
        noteView.Label.color = label;
    }

    // Converts a note hit time into its current world X position.
    private static float NoteX(StockRhythmNote note, float gameTime)
    {
        return StockRhythmConstants.JUDGE_X + (note.HitTime - gameTime) * StockRhythmConstants.NOTE_SPEED;
    }
}
