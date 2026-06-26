using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

//==============================================================================
// StockRhythmController
//==============================================================================
// Controls the BUY/SELL rhythm prototype, including chart notes, UI, and scoring.
//==============================================================================
public sealed class StockRhythmController
{
    // Trade action enum values are named with screaming snake case by convention.
    private enum TradeAction
    {
        BUY,
        SELL
    }

    // Play state enum values are named with screaming snake case by convention.
    private enum PlayState
    {
        READY,
        PLAYING,
        PAUSED,
        FINISHED
    }

    // Runtime note model used for judgement and chart drawing.
    private sealed class ChartNote
    {
        // Target hit time in stage seconds.
        public float HitTime { get; set; }

        // Vertical chart lane position.
        public float Y { get; set; }

        // Required BUY or SELL action for this note.
        public TradeAction Action { get; set; }

        // Whether the note has already been judged.
        public bool Judged { get; set; }

        // Previous note used for chart segment drawing.
        public ChartNote Previous { get; set; }

        // Visual bundle for the button-like note marker.
        public NoteView NoteView { get; set; }

        // Visual bundle for the candle under the note.
        public CandleView CandleView { get; set; }

        // Line segment connecting this note to the previous note.
        public LineRenderer Segment { get; set; }
    }

    // Visual references for one BUY/SELL note.
    private sealed class NoteView
    {
        // Root GameObject for this note visual.
        public GameObject Root { get; set; }

        // Colored note panel renderer.
        public SpriteRenderer Panel { get; set; }

        // White note border renderer.
        public SpriteRenderer Border { get; set; }

        // Direction icon renderer.
        public SpriteRenderer Icon { get; set; }

        // BUY/SELL label mesh.
        public TextMesh Label { get; set; }
    }

    // Visual references for one chart candle.
    private sealed class CandleView
    {
        // Root GameObject for this candle visual.
        public GameObject Root { get; set; }

        // Candle body renderer.
        public SpriteRenderer Body { get; set; }

        // Candle wick line renderer.
        public LineRenderer Wick { get; set; }
    }

    // Total prototype stage duration in seconds.
    private const float STAGE_DURATION = 60f;

    // Initial virtual seed money used in the result screen.
    private const float INITIAL_SEED_MONEY = 1000000f;

    // X position of the timing judgement line.
    private const float JUDGE_X = -4.55f;

    // Horizontal note scroll speed in world units per second.
    private const float NOTE_SPEED = 4.65f;

    // Seconds ahead of the judgement line where notes are generated.
    private const float SPAWN_LEAD_TIME = 3.2f;

    // Seconds after judgement before old visuals are removed.
    private const float CLEANUP_BEHIND_TIME = 1.2f;

    // Perfect judgement timing window.
    private const float PERFECT_WINDOW = 0.075f;

    // Great judgement timing window.
    private const float GREAT_WINDOW = 0.135f;

    // Good judgement timing window.
    private const float GOOD_WINDOW = 0.22f;

    // Base metronome beat length for generated click sounds.
    private const float BASE_BEAT_SECONDS = 60f / 128f;

    // Active chart notes for the current stage.
    private readonly List<ChartNote> _notes = new List<ChartNote>();

    // Root dependency injected by GameRoot.
    private GameRoot _gameRoot;

    // Main camera used for the prototype scene.
    private Camera _mainCamera;

    // Runtime HUD canvas.
    private Canvas _canvas;

    // Dynamic UI font for Korean and English labels.
    private Font _uiFont;

    // Shared line material for generated LineRenderers.
    private Material _lineMaterial;

    // Solid rectangle sprite used by generated UI and world objects.
    private Sprite _rectSprite;

    // Filled circle sprite used by the timing marker.
    private Sprite _circleSprite;

    // Ring sprite used by the timing marker.
    private Sprite _ringSprite;

    // Soft circle sprite used for glows.
    private Sprite _softCircleSprite;

    // Up arrow sprite used for BUY notes and buttons.
    private Sprite _upTriangleSprite;

    // Down arrow sprite used for SELL notes and buttons.
    private Sprite _downTriangleSprite;

    // Root object for generated world visuals.
    private GameObject _worldRoot;

    // Root object for generated scrolling chart visuals.
    private GameObject _chartRoot;

    // Root object for the player timing marker.
    private GameObject _markerRoot;

    // Ring renderer on the timing marker.
    private SpriteRenderer _markerRing;

    // Core renderer on the timing marker.
    private SpriteRenderer _markerCore;

    // Main judgement beam renderer.
    private LineRenderer _judgeBeam;

    // Wide glow renderer behind the judgement beam.
    private LineRenderer _judgeGlow;

    // World-space judgement text near the timing line.
    private TextMesh _judgeTextWorld;

    // Runtime audio source for generated click and hit sounds.
    private AudioSource _audioSource;

    // Generated beat click sound.
    private AudioClip _beatClip;

    // Generated successful hit sound.
    private AudioClip _hitClip;

    // Generated miss sound.
    private AudioClip _missClip;

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

    // Deterministic random generator for reproducible sample charts.
    private System.Random _random;

    // Current stage time in seconds.
    private float _gameTime;

    // Next note generation hit time.
    private float _nextNoteTime;

    // Next beat tick time for generated metronome sounds.
    private float _nextBeatTick;

    // Previous generated note Y position.
    private float _lastY;

    // Current generated market trend direction.
    private int _trend;

    // Current score.
    private int _score;

    // Current combo count.
    private int _combo;

    // Highest combo count reached this stage.
    private int _maxCombo;

    // Successful hit count.
    private int _hits;

    // Perfect judgement count.
    private int _perfects;

    // Great judgement count.
    private int _greats;

    // Good judgement count.
    private int _goods;

    // Missed note count.
    private int _misses;

    // Wrong-side input count.
    private int _wrongs;

    // Extra input count outside the timing window.
    private int _extraInputs;

    // Fever gauge amount from 0 to 1.
    private float _fever;

    // Remaining active fever seconds.
    private float _feverTimer;

    // Target Y position for the timing marker.
    private float _markerY;

    // SmoothDamp velocity for the timing marker.
    private float _markerVelocity;

    // Current state of the stage.
    private PlayState _playState;

    // Last judgement label shown in the HUD.
    private string _lastJudge = "READY";

    // Whether fever score multiplier is currently active.
    private bool IsFeverActive => _playState == PlayState.PLAYING && _feverTimer > 0f;

    // Injects GameRoot dependency.
    public void Construct(GameRoot gameRoot)
    {
        _gameRoot = gameRoot;
    }

    // Creates generated assets, world visuals, UI, and the first stage.
    public void Initialize()
    {
        _random = new System.Random(314159);
        PrepareAssets();
        ConfigureCamera();
        CreateAudio();
        CreateWorld();
        CreateUi();
        ResetStage();
    }

    // Runs one controller frame from GameRoot.Update.
    public void Tick(float deltaTime)
    {
        PollKeyboard();

        if (_playState != PlayState.PLAYING)
        {
            return;
        }

        _gameTime += deltaTime;
        GenerateNotesUntil(_gameTime + SPAWN_LEAD_TIME + 1.5f);
        UpdateFever(deltaTime);
        UpdateBeat();
        UpdateNotes();
        UpdateMarker(false);
        CheckMisses();
        CleanupOldNotes();
        UpdateHud();

        if (_gameTime >= STAGE_DURATION)
        {
            EndStage();
        }
    }

    // Releases transient runtime resources created by the controller.
    public void Dispose()
    {
        Time.timeScale = 1f;

        if (_lineMaterial != null)
        {
            UnityEngine.Object.Destroy(_lineMaterial);
        }
    }

    // Builds reusable generated sprites and materials.
    private void PrepareAssets()
    {
        _uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Segoe UI", "Arial" }, 32);
        _rectSprite = SpriteFromTexture(CreateSolidTexture(Color.white), 1f);
        _circleSprite = SpriteFromTexture(CreateCircleTexture(96, false, Color.white), 96f);
        _ringSprite = SpriteFromTexture(CreateRingTexture(128, Color.white), 128f);
        _softCircleSprite = SpriteFromTexture(CreateCircleTexture(128, true, Color.white), 128f);
        _upTriangleSprite = SpriteFromTexture(CreateTriangleTexture(96, true, Color.white), 96f);
        _downTriangleSprite = SpriteFromTexture(CreateTriangleTexture(96, false, Color.white), 96f);
        _lineMaterial = new Material(Shader.Find("Sprites/Default"));
        _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
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

    // Creates generated audio clips and a runtime audio source.
    private void CreateAudio()
    {
        _audioSource = _gameRoot.gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.volume = 0.55f;
        _beatClip = CreateToneClip(110f, 0.055f, 0.18f);
        _hitClip = CreateToneClip(880f, 0.08f, 0.25f);
        _missClip = CreateToneClip(92f, 0.12f, 0.35f);
    }

    // Creates world roots and static stage visuals.
    private void CreateWorld()
    {
        _worldRoot = new GameObject("World");
        _worldRoot.transform.SetParent(_gameRoot.transform, false);

        _chartRoot = new GameObject("Scrolling Chart");
        _chartRoot.transform.SetParent(_worldRoot.transform, false);

        CreateBackground();
        CreateJudgeLine();
        CreateMarker();
        CreateWorldLabels();
    }

    // Creates grid, skyline, and neon background objects.
    private void CreateBackground()
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
            float height = 0.25f + (float)_random.NextDouble() * 1.0f;
            SpriteRenderer block = CreateSpriteRenderer("City Block", _rectSprite, new Color(0.018f, 0.045f, 0.085f, 0.95f), -40);
            block.transform.position = new Vector3(x, -3.08f + height * 0.5f, 0f);
            block.transform.localScale = new Vector3(0.42f, height, 1f);
        }

        for (int i = 0; i < 36; i++)
        {
            Color color = i % 2 == 0
                ? new Color(0f, 0.9f, 0.65f, 0.55f)
                : new Color(1f, 0.1f, 0.72f, 0.5f);
            SpriteRenderer light = CreateSpriteRenderer("Neon Pixel", _rectSprite, color, -20);
            light.transform.position = new Vector3(-8.3f + (float)_random.NextDouble() * 16.6f, -2.7f + (float)_random.NextDouble() * 6.4f, 0f);
            light.transform.localScale = new Vector3(0.045f, 0.16f, 1f);
        }
    }

    // Creates the vertical timing judgement line.
    private void CreateJudgeLine()
    {
        _judgeGlow = CreateLine("Judge Glow", new Color(1f, 0.02f, 0.82f, 0.4f), 0.24f, 10);
        _judgeGlow.positionCount = 2;
        _judgeGlow.SetPosition(0, new Vector3(JUDGE_X, -2.42f, 0f));
        _judgeGlow.SetPosition(1, new Vector3(JUDGE_X, 3.52f, 0f));

        _judgeBeam = CreateLine("Judge Beam", new Color(1f, 0.7f, 1f, 0.95f), 0.055f, 11);
        _judgeBeam.positionCount = 2;
        _judgeBeam.SetPosition(0, new Vector3(JUDGE_X, -2.42f, 0f));
        _judgeBeam.SetPosition(1, new Vector3(JUDGE_X, 3.52f, 0f));
    }

    // Creates the player timing marker.
    private void CreateMarker()
    {
        _markerRoot = new GameObject("Player Timing Marker");
        _markerRoot.transform.SetParent(_worldRoot.transform, false);

        SpriteRenderer glow = CreateSpriteRenderer("Marker Glow", _softCircleSprite, new Color(0f, 0.9f, 1f, 0.28f), 20);
        glow.transform.SetParent(_markerRoot.transform, false);
        glow.transform.localScale = Vector3.one * 1.35f;

        _markerRing = CreateSpriteRenderer("Marker Ring", _ringSprite, new Color(0.38f, 1f, 1f, 1f), 21);
        _markerRing.transform.SetParent(_markerRoot.transform, false);
        _markerRing.transform.localScale = Vector3.one * 0.72f;

        _markerCore = CreateSpriteRenderer("Marker Core", _circleSprite, new Color(0.02f, 0.015f, 0.05f, 1f), 22);
        _markerCore.transform.SetParent(_markerRoot.transform, false);
        _markerCore.transform.localScale = Vector3.one * 0.34f;
    }

    // Creates world-space stage title and judgement labels.
    private void CreateWorldLabels()
    {
        TextMesh centerTitle = CreateWorldText("SOMSOMG", new Vector3(0f, 4.08f, 0f), 0.16f, new Color(1f, 0.88f, 1f, 1f), 35);
        centerTitle.fontStyle = FontStyle.Bold;

        TextMesh subtitle = CreateWorldText("VIRTUAL STOCK RHYTHM", new Vector3(0f, 3.72f, 0f), 0.055f, new Color(0.5f, 1f, 0.95f, 0.72f), 35);
        subtitle.fontStyle = FontStyle.Bold;

        _judgeTextWorld = CreateWorldText("READY", new Vector3(JUDGE_X + 0.72f, 2.86f, 0f), 0.105f, new Color(1f, 0.12f, 0.72f, 1f), 36);
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

        Button pauseButton = CreateTopButton("II", new Vector2(-40f, -36f), TogglePause);
        _pauseButtonText = pauseButton.GetComponentInChildren<Text>();

        CreateActionButton("BUY", "L SHIFT", TradeAction.BUY, new Vector2(168f, 92f), new Color(0.06f, 0.82f, 0.24f, 1f), _upTriangleSprite);
        CreateActionButton("SELL", "R SHIFT", TradeAction.SELL, new Vector2(-168f, 92f), new Color(0.95f, 0.1f, 0.12f, 1f), _downTriangleSprite);
        CreateFeverBar();
        CreateResultPanel();
    }

    // Ensures UI buttons work with the new Input System.
    private void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        InputSystemUIInputModule module = eventSystem.AddComponent<InputSystemUIInputModule>();

        InputAction point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
        InputAction click = new InputAction("Click", InputActionType.PassThrough, "<Pointer>/press");
        InputAction scroll = new InputAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll");
        InputAction move = new InputAction("Move", InputActionType.PassThrough);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        InputAction submit = new InputAction("Submit", InputActionType.Button, "<Keyboard>/enter");
        InputAction cancel = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");

        point.Enable();
        click.Enable();
        scroll.Enable();
        move.Enable();
        submit.Enable();
        cancel.Enable();

        module.point = InputActionReference.Create(point);
        module.leftClick = InputActionReference.Create(click);
        module.scrollWheel = InputActionReference.Create(scroll);
        module.move = InputActionReference.Create(move);
        module.submit = InputActionReference.Create(submit);
        module.cancel = InputActionReference.Create(cancel);
    }

    // Creates one large BUY or SELL action button.
    private void CreateActionButton(string label, string keyHint, TradeAction action, Vector2 anchoredPosition, Color color, Sprite icon)
    {
        GameObject root = new GameObject(label + " Button");
        root.transform.SetParent(_canvas.transform, false);
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        rectTransform.anchorMin = action == TradeAction.BUY ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.pivot = action == TradeAction.BUY ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(260f, 92f);

        Image image = root.AddComponent<Image>();
        image.sprite = _rectSprite;
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
        button.onClick.AddListener(() => HandleTradeInput(action));

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
    private Button CreateTopButton(string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
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
        image.sprite = _rectSprite;
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
        background.sprite = _rectSprite;
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
        _feverFill.sprite = _rectSprite;
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
        image.sprite = _rectSprite;
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
        restartButton.onClick.AddListener(ResetStage);
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
        image.sprite = _rectSprite;
        image.color = color;
        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateUiText(label, root.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 34, TextAnchor.MiddleCenter, Color.white);
        text.fontStyle = FontStyle.Bold;
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 72f);
        return button;
    }

    // Resets score, notes, timers, and chart generation state.
    private void ResetStage()
    {
        Time.timeScale = 1f;

        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            DestroyNoteVisuals(_notes[i]);
        }

        _notes.Clear();
        _random = new System.Random(314159);
        _gameTime = 0f;
        _nextNoteTime = 1.4f;
        _nextBeatTick = 0f;
        _lastY = -0.9f;
        _markerY = _lastY;
        _markerVelocity = 0f;
        _trend = 1;
        _score = 0;
        _combo = 0;
        _maxCombo = 0;
        _hits = 0;
        _perfects = 0;
        _greats = 0;
        _goods = 0;
        _misses = 0;
        _wrongs = 0;
        _extraInputs = 0;
        _fever = 0f;
        _feverTimer = 0f;
        _lastJudge = "READY";
        _playState = PlayState.PLAYING;

        _resultPanel.SetActive(false);
        _pauseButtonText.text = "II";

        GenerateNotesUntil(SPAWN_LEAD_TIME + 4f);
        UpdateNotes();
        UpdateMarker(true);
        UpdateHud();
    }

    // Generates notes until the requested future time is covered.
    private void GenerateNotesUntil(float aheadTime)
    {
        while (_nextNoteTime < aheadTime && _nextNoteTime <= STAGE_DURATION + 1f)
        {
            AddNote();
        }
    }

    // Creates one rule-based BUY or SELL note from the current virtual market trend.
    private void AddNote()
    {
        float difficulty = Mathf.Clamp01(_nextNoteTime / STAGE_DURATION);
        if (_random.NextDouble() < 0.18 + difficulty * 0.07)
        {
            _trend *= -1;
        }

        if (_lastY > 2.55f)
        {
            _trend = -1;
        }
        else if (_lastY < -1.85f)
        {
            _trend = 1;
        }

        float volatility = 0.38f + (float)_random.NextDouble() * (0.35f + difficulty * 0.25f);
        float nextY = Mathf.Clamp(_lastY + _trend * volatility, -2.05f, 2.75f);
        TradeAction action = _trend > 0 ? TradeAction.BUY : TradeAction.SELL;
        ChartNote previous = _notes.Count > 0 ? _notes[_notes.Count - 1] : null;

        ChartNote note = new ChartNote
        {
            HitTime = _nextNoteTime,
            Y = nextY,
            Action = action,
            Previous = previous
        };

        note.NoteView = CreateNoteView(note);
        note.CandleView = CreateCandle(note);
        if (previous != null)
        {
            note.Segment = CreateChartSegment(note);
        }

        _notes.Add(note);
        _lastY = nextY;

        float interval = Mathf.Lerp(0.78f, 0.50f, difficulty);
        if (_random.NextDouble() < 0.17)
        {
            interval += BASE_BEAT_SECONDS * 0.5f;
        }

        _nextNoteTime += interval;
    }

    // Creates the visible note marker for a chart note.
    private NoteView CreateNoteView(ChartNote note)
    {
        GameObject root = new GameObject(note.Action + " Note");
        root.transform.SetParent(_chartRoot.transform, false);

        Color main = note.Action == TradeAction.BUY
            ? new Color(0.18f, 0.95f, 0.33f, 1f)
            : new Color(1f, 0.19f, 0.2f, 1f);

        SpriteRenderer border = CreateSpriteRenderer("Border", _rectSprite, Color.white, 25);
        border.transform.SetParent(root.transform, false);
        border.transform.localScale = new Vector3(0.74f, 0.74f, 1f);

        SpriteRenderer panel = CreateSpriteRenderer("Panel", _rectSprite, main, 26);
        panel.transform.SetParent(root.transform, false);
        panel.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

        Sprite iconSprite = note.Action == TradeAction.BUY ? _upTriangleSprite : _downTriangleSprite;
        SpriteRenderer icon = CreateSpriteRenderer("Icon", iconSprite, Color.white, 27);
        icon.transform.SetParent(root.transform, false);
        icon.transform.localPosition = new Vector3(0f, 0.085f, 0f);
        icon.transform.localScale = Vector3.one * 0.32f;

        TextMesh label = CreateWorldText(note.Action == TradeAction.BUY ? "BUY" : "SELL", Vector3.zero, 0.035f, Color.white, 28);
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(0f, -0.18f, 0f);
        label.fontStyle = FontStyle.Bold;

        return new NoteView
        {
            Root = root,
            Panel = panel,
            Border = border,
            Icon = icon,
            Label = label
        };
    }

    // Creates a small candle visual under a chart note.
    private CandleView CreateCandle(ChartNote note)
    {
        GameObject root = new GameObject("Candle");
        root.transform.SetParent(_chartRoot.transform, false);
        Color color = note.Action == TradeAction.BUY
            ? new Color(0.26f, 1f, 0.66f, 0.86f)
            : new Color(1f, 0.24f, 0.28f, 0.86f);

        SpriteRenderer body = CreateSpriteRenderer("Body", _rectSprite, color, 5);
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.07f, 0.32f + (float)_random.NextDouble() * 0.2f, 1f);

        LineRenderer wick = CreateLine("Wick", color, 0.018f, 4);
        wick.transform.SetParent(root.transform, false);
        wick.positionCount = 2;

        return new CandleView
        {
            Root = root,
            Body = body,
            Wick = wick
        };
    }

    // Creates a colored line segment between two chart notes.
    private LineRenderer CreateChartSegment(ChartNote note)
    {
        Color color = note.Action == TradeAction.BUY
            ? new Color(0.18f, 1f, 0.55f, 0.92f)
            : new Color(1f, 0.17f, 0.22f, 0.92f);
        LineRenderer segment = CreateLine("Chart Segment", color, 0.06f, 6);
        segment.transform.SetParent(_chartRoot.transform, false);
        segment.positionCount = 2;
        return segment;
    }

    // Reads keyboard controls and dispatches gameplay commands.
    private void PollKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame && _playState == PlayState.FINISHED)
        {
            ResetStage();
        }

        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        if (_playState != PlayState.PLAYING)
        {
            return;
        }

        if (keyboard.leftShiftKey.wasPressedThisFrame)
        {
            HandleTradeInput(TradeAction.BUY);
        }

        if (keyboard.rightShiftKey.wasPressedThisFrame)
        {
            HandleTradeInput(TradeAction.SELL);
        }
    }

    // Judges one BUY or SELL input against the nearest active note.
    private void HandleTradeInput(TradeAction action)
    {
        if (_playState != PlayState.PLAYING)
        {
            return;
        }

        ChartNote target = FindJudgeTarget();
        if (target == null)
        {
            _extraInputs++;
            _combo = 0;
            _score = Mathf.Max(0, _score - 100);
            _lastJudge = action == TradeAction.BUY ? "TOO EARLY BUY" : "TOO EARLY SELL";
            PlayClip(_missClip, 0.75f);
            UpdateHud();
            return;
        }

        float error = Mathf.Abs(target.HitTime - _gameTime);
        if (target.Action != action)
        {
            target.Judged = true;
            _wrongs++;
            _combo = 0;
            _score = Mathf.Max(0, _score - 150);
            _lastJudge = "WRONG SIDE";
            FadeNote(target, new Color(0.45f, 0.45f, 0.45f, 0.35f));
            PlayClip(_missClip, 0.9f);
            UpdateHud();
            return;
        }

        ApplySuccessfulHit(target, error);
    }

    // Applies score, combo, marker movement, and fever gain after a valid hit.
    private void ApplySuccessfulHit(ChartNote target, float error)
    {
        target.Judged = true;
        _hits++;
        _combo++;
        _maxCombo = Mathf.Max(_maxCombo, _combo);
        _markerY = target.Y;
        _markerVelocity = 0f;

        int baseScore;
        float feverGain;
        if (error <= PERFECT_WINDOW)
        {
            _perfects++;
            baseScore = 1000;
            feverGain = 0.085f;
            _lastJudge = "PERFECT";
        }
        else if (error <= GREAT_WINDOW)
        {
            _greats++;
            baseScore = 650;
            feverGain = 0.055f;
            _lastJudge = "GREAT";
        }
        else
        {
            _goods++;
            baseScore = 350;
            feverGain = 0.035f;
            _lastJudge = "GOOD";
        }

        float multiplier = IsFeverActive ? 2f : 1f;
        _score += Mathf.RoundToInt((baseScore + _combo * 12) * multiplier);
        if (!IsFeverActive)
        {
            _fever = Mathf.Clamp01(_fever + feverGain);
            if (_fever >= 1f)
            {
                _feverTimer = 6f;
                _lastJudge = "FEVER";
            }
        }

        FadeNote(target, Color.white);
        PlayClip(_hitClip, IsFeverActive ? 1.15f : 0.9f);
        UpdateHud();
    }

    // Finds the nearest unjudged note inside the Good timing window.
    private ChartNote FindJudgeTarget()
    {
        ChartNote best = null;
        float bestError = GOOD_WINDOW;
        for (int i = 0; i < _notes.Count; i++)
        {
            ChartNote note = _notes[i];
            if (note.Judged)
            {
                continue;
            }

            float error = Mathf.Abs(note.HitTime - _gameTime);
            if (error <= bestError)
            {
                best = note;
                bestError = error;
            }
        }

        return best;
    }

    // Ticks the fever timer and clears the gauge when fever ends.
    private void UpdateFever(float deltaTime)
    {
        if (!IsFeverActive)
        {
            return;
        }

        _feverTimer -= deltaTime;
        _fever = Mathf.Clamp01(_feverTimer / 6f);
        if (_feverTimer <= 0f)
        {
            _feverTimer = 0f;
            _fever = 0f;
        }
    }

    // Plays metronome ticks and pulses the judgement line.
    private void UpdateBeat()
    {
        while (_gameTime >= _nextBeatTick)
        {
            PlayClip(_beatClip, IsFeverActive ? 0.9f : 0.55f);
            _nextBeatTick += BASE_BEAT_SECONDS;
        }

        float pulse = 1f + Mathf.PingPong(_gameTime * 8f, 0.35f);
        _judgeBeam.widthMultiplier = 0.05f * pulse;
        _judgeGlow.widthMultiplier = IsFeverActive ? 0.36f : 0.24f;
    }

    // Updates positions and alpha values for notes, candles, and chart segments.
    private void UpdateNotes()
    {
        for (int i = 0; i < _notes.Count; i++)
        {
            ChartNote note = _notes[i];
            float x = NoteX(note);
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
                note.Segment.SetPosition(0, new Vector3(NoteX(note.Previous), note.Previous.Y, 0f));
                note.Segment.SetPosition(1, position);
            }
        }
    }

    // Moves the timing marker toward the last successfully hit note.
    private void UpdateMarker(bool instant)
    {
        if (instant)
        {
            _markerRoot.transform.position = new Vector3(JUDGE_X, _markerY, 0f);
            return;
        }

        float current = _markerRoot.transform.position.y;
        float smooth = Mathf.SmoothDamp(current, _markerY, ref _markerVelocity, 0.08f, 20f);
        _markerRoot.transform.position = new Vector3(JUDGE_X, smooth, 0f);
        _markerRing.color = IsFeverActive ? new Color(1f, 0.82f, 0.08f, 1f) : new Color(0.38f, 1f, 1f, 1f);
    }

    // Converts overdue unjudged notes into misses.
    private void CheckMisses()
    {
        for (int i = 0; i < _notes.Count; i++)
        {
            ChartNote note = _notes[i];
            if (note.Judged)
            {
                continue;
            }

            if (_gameTime - note.HitTime > GOOD_WINDOW)
            {
                note.Judged = true;
                _misses++;
                _combo = 0;
                _lastJudge = "MISS";
                FadeNote(note, new Color(0.55f, 0.55f, 0.55f, 0.25f));
                PlayClip(_missClip, 0.6f);
            }
        }
    }

    // Removes old note visuals after they scroll behind the player.
    private void CleanupOldNotes()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            ChartNote note = _notes[i];
            if (_gameTime - note.HitTime > CLEANUP_BEHIND_TIME)
            {
                DestroyNoteVisuals(note);
                _notes.RemoveAt(i);
            }
        }
    }

    // Destroys every generated object owned by one note.
    private void DestroyNoteVisuals(ChartNote note)
    {
        if (note.NoteView != null && note.NoteView.Root != null)
        {
            UnityEngine.Object.Destroy(note.NoteView.Root);
        }

        if (note.CandleView != null && note.CandleView.Root != null)
        {
            UnityEngine.Object.Destroy(note.CandleView.Root);
        }

        if (note.Segment != null)
        {
            UnityEngine.Object.Destroy(note.Segment.gameObject);
        }
    }

    // Applies a simple visual fade after judging a note.
    private static void FadeNote(ChartNote note, Color flashColor)
    {
        if (note.NoteView == null)
        {
            return;
        }

        note.NoteView.Panel.color = Color.Lerp(note.NoteView.Panel.color, flashColor, 0.5f);
        note.NoteView.Border.color = new Color(1f, 1f, 1f, 0.35f);
    }

    // Applies alpha to every renderer in a note view.
    private static void SetNoteAlpha(NoteView noteView, float alpha)
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
    private float NoteX(ChartNote note)
    {
        return JUDGE_X + (note.HitTime - _gameTime) * NOTE_SPEED;
    }

    // Refreshes score, combo, judgement, timer, and fever HUD elements.
    private void UpdateHud()
    {
        int attempts = _hits + _misses + _wrongs + _extraInputs;
        float accuracy = attempts > 0 ? (float)_hits / attempts * 100f : 100f;
        _scoreText.text = "SCORE\n" + _score.ToString("N0");
        _comboText.text = _combo > 0 ? _combo.ToString() + " COMBO" : "0 COMBO";
        _accuracyText.text = "ACC " + accuracy.ToString("0.0") + "%  HIT " + _hits + " / MISS " + (_misses + _wrongs);
        _timerText.text = Mathf.CeilToInt(Mathf.Max(0f, STAGE_DURATION - _gameTime)).ToString("00") + "s";
        _judgeText.text = _lastJudge;
        _judgeTextWorld.text = _lastJudge;
        _judgeText.color = IsFeverActive ? new Color(1f, 0.85f, 0.1f, 1f) : new Color(1f, 0.34f, 0.78f, 1f);
        _feverText.text = IsFeverActive ? "FEVER x2" : "FEVER";

        RectTransform fillRect = _feverFill.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(504f * Mathf.Clamp01(_fever), -16f);
    }

    // Toggles between playing and paused states.
    private void TogglePause()
    {
        if (_playState == PlayState.FINISHED)
        {
            return;
        }

        if (_playState == PlayState.PAUSED)
        {
            _playState = PlayState.PLAYING;
            Time.timeScale = 1f;
            _lastJudge = "READY";
            _pauseButtonText.text = "II";
        }
        else if (_playState == PlayState.PLAYING)
        {
            _playState = PlayState.PAUSED;
            Time.timeScale = 0f;
            _lastJudge = "PAUSED";
            _pauseButtonText.text = ">";
        }

        UpdateHud();
    }

    // Calculates and displays the final result screen.
    private void EndStage()
    {
        _playState = PlayState.FINISHED;
        Time.timeScale = 1f;

        int attempts = _hits + _misses + _wrongs + _extraInputs;
        float accuracy = attempts > 0 ? (float)_hits / attempts : 1f;
        float judgementBonus = _perfects * 1.2f + _greats * 0.75f + _goods * 0.35f;
        float riskPenalty = _misses * 0.7f + _wrongs * 1.1f + _extraInputs * 0.3f;
        float returnPercent = Mathf.Clamp((_score / 90000f) + judgementBonus * 0.018f - riskPenalty * 0.026f, -0.18f, 0.42f);
        float seedMoney = INITIAL_SEED_MONEY * (1f + returnPercent);

        _resultTitleText.text = "STAGE CLEAR";
        _resultBodyText.text =
            "Final Score  " + _score.ToString("N0") + "\n" +
            "Accuracy     " + (accuracy * 100f).ToString("0.0") + "%\n" +
            "Max Combo    " + _maxCombo.ToString("N0") + "\n" +
            "Perfect / Great / Good  " + _perfects + " / " + _greats + " / " + _goods + "\n" +
            "Virtual Return  " + (returnPercent * 100f).ToString("+0.0;-0.0;0.0") + "%\n" +
            "Seed Money   " + seedMoney.ToString("N0") + "\n\n" +
            "VIRTUAL GAME RESULT - NOT INVESTMENT ADVICE";
        _resultPanel.SetActive(true);
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
        text.font = _uiFont;
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
        textMesh.font = _uiFont;
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
        line.material = _lineMaterial;
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

    // Plays a one-shot generated audio clip.
    private void PlayClip(AudioClip clip, float volumeScale)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip, volumeScale);
        }
    }

    // Creates a short generated sine-wave tone.
    private static AudioClip CreateToneClip(float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float envelope = 1f - (float)i / sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * volume * envelope;
        }

        AudioClip clip = AudioClip.Create("Tone " + frequency, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Creates a one-pixel solid texture.
    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Creates a filled or soft circle texture.
    private static Texture2D CreateCircleTexture(int size, bool soft, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = soft ? Mathf.Clamp01(1f - distance) : (distance <= 1f ? 1f : 0f);
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Creates a ring texture for the timing marker.
    private static Texture2D CreateRingTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = distance > 0.62f && distance < 0.96f ? 1f : 0f;
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Creates an up or down triangle texture for note icons.
    private static Texture2D CreateTriangleTexture(int size, bool up, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedX = Mathf.Abs((x + 0.5f) / size - 0.5f) * 2f;
                float normalizedY = (y + 0.5f) / size;
                float edge = up ? normalizedY : 1f - normalizedY;
                bool inside = edge > 0.16f && normalizedX < edge * 0.95f;
                texture.SetPixel(x, y, inside ? color : Color.clear);
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Wraps a generated texture in a sprite.
    private static Sprite SpriteFromTexture(Texture2D texture, float pixelsPerUnit)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
