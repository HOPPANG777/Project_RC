using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// Feature: class::ChartGameplayHudPresenter
// Description: Displays required gameplay HUD state for SAMSU, Profit, Combo, BUY/SELL, and judgement text.
//==============================================================================
// Implementation notes
// - HUD reads score state through IChartScoreService and does not recalculate judgement.
// - Missing UI references can be auto-created so the MVP scene shows the required HUD immediately.
// - Timing uses Update and a small timer instead of coroutines.
//==============================================================================

[DisallowMultipleComponent]
public class ChartGameplayHudPresenter : MonoBehaviour
{
    //==============================================================================
    // Constants
    //==============================================================================

    // Default stage name shown when the stage service has no display name.
    private const string DEFAULT_STAGE_NAME = "SAMSU";

    // Default sorting order used by the generated overlay canvas.
    private const int DEFAULT_CANVAS_SORT_ORDER = 100;

    // Minimum amount of time a judgement label can stay visible.
    private const float MIN_JUDGEMENT_DISPLAY_SECONDS = 0.05f;

    // Width used by generated left and right status labels.
    private const float SIDE_LABEL_WIDTH = 360.0f;

    // Height used by generated top status labels.
    private const float TOP_LABEL_HEIGHT = 48.0f;

    // Width used by generated action labels.
    private const float ACTION_LABEL_WIDTH = 180.0f;

    // Height used by generated action labels.
    private const float ACTION_LABEL_HEIGHT = 48.0f;

    // Width used by generated judgement feedback label.
    private const float JUDGEMENT_LABEL_WIDTH = 640.0f;

    // Height used by generated judgement feedback label.
    private const float JUDGEMENT_LABEL_HEIGHT = 96.0f;

    //==============================================================================
    // Serialized Fields
    //==============================================================================

    // Root canvas that contains gameplay HUD text.
    [TabGroup("References")]
    [Tooltip("Canvas used to display the gameplay HUD overlay.")]
    [SerializeField]
    private Canvas _canvasRoot;

    // Stage name text, normally showing SAMSU for the MVP stage.
    [TabGroup("References")]
    [Tooltip("Text that displays the current stage or symbol name.")]
    [SerializeField]
    private TMP_Text _stageText;

    // Profit text fed by ChartScoreController.
    [TabGroup("References")]
    [Tooltip("Text that displays current Profit.")]
    [SerializeField]
    private TMP_Text _profitText;

    // Combo text fed by ChartScoreController.
    [TabGroup("References")]
    [Tooltip("Text that displays current Combo.")]
    [SerializeField]
    private TMP_Text _comboText;

    // BUY side indicator text.
    [TabGroup("References")]
    [Tooltip("Text that labels the BUY input side.")]
    [SerializeField]
    private TMP_Text _buyText;

    // SELL side indicator text.
    [TabGroup("References")]
    [Tooltip("Text that labels the SELL input side.")]
    [SerializeField]
    private TMP_Text _sellText;

    // Latest judgement feedback text.
    [TabGroup("References")]
    [Tooltip("Text that displays the latest PERFECT/GREAT/GOOD/MISS judgement.")]
    [SerializeField]
    private TMP_Text _judgementText;

    // Whether this presenter should create missing HUD UI at runtime.
    [TabGroup("Settings")]
    [Tooltip("Creates a default TMP/UGUI HUD when scene references are missing.")]
    [SerializeField]
    private bool _autoCreateMissingHud = true;

    // Sorting order used when the presenter creates a canvas.
    [TabGroup("Settings")]
    [Tooltip("Sorting order used by the generated overlay canvas.")]
    [SerializeField]
    private int _canvasSortOrder = DEFAULT_CANVAS_SORT_ORDER;

    // Number of seconds the latest judgement text remains visible.
    [TabGroup("Settings")]
    [Tooltip("Seconds to keep latest judgement feedback visible.")]
    [MinValue(MIN_JUDGEMENT_DISPLAY_SECONDS)]
    [SerializeField]
    private float _judgementDisplaySeconds = 0.55f;

    // Neutral HUD text color.
    [TabGroup("Color")]
    [Tooltip("Default HUD text color.")]
    [SerializeField]
    private Color _primaryTextColor = new Color(0.86f, 0.92f, 1.0f, 1.0f);

    // Muted secondary text color.
    [TabGroup("Color")]
    [Tooltip("Muted HUD text color.")]
    [SerializeField]
    private Color _secondaryTextColor = new Color(0.62f, 0.70f, 0.82f, 1.0f);

    // BUY indicator color.
    [TabGroup("Color")]
    [Tooltip("BUY indicator color.")]
    [SerializeField]
    private Color _buyColor = new Color(0.22f, 0.86f, 0.45f, 1.0f);

    // SELL indicator color.
    [TabGroup("Color")]
    [Tooltip("SELL indicator color.")]
    [SerializeField]
    private Color _sellColor = new Color(1.0f, 0.27f, 0.32f, 1.0f);

    // PERFECT judgement color.
    [TabGroup("Color")]
    [Tooltip("PERFECT judgement feedback color.")]
    [SerializeField]
    private Color _perfectColor = new Color(1.0f, 0.91f, 0.34f, 1.0f);

    // GREAT judgement color.
    [TabGroup("Color")]
    [Tooltip("GREAT judgement feedback color.")]
    [SerializeField]
    private Color _greatColor = new Color(0.42f, 0.82f, 1.0f, 1.0f);

    // GOOD judgement color.
    [TabGroup("Color")]
    [Tooltip("GOOD judgement feedback color.")]
    [SerializeField]
    private Color _goodColor = new Color(0.56f, 0.95f, 0.62f, 1.0f);

    // MISS judgement color.
    [TabGroup("Color")]
    [Tooltip("MISS judgement feedback color.")]
    [SerializeField]
    private Color _missColor = new Color(1.0f, 0.34f, 0.42f, 1.0f);

    //==============================================================================
    // Internal State
    //==============================================================================

    // Whether Init has supplied HUD dependencies.
    private bool _isInitialized;

    // Stage service used for the displayed stage name.
    private IChartStageService _chartStageService;

    // Score service that feeds Profit, Combo, and judgement feedback.
    private IChartScoreService _chartScoreService;

    // Remaining visible time for the latest judgement text.
    private float _judgementDisplayTimeRemaining;

    //==============================================================================
    // Properties
    //==============================================================================

    /// <summary>Whether Init has supplied HUD dependencies.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Initialized")]
    public bool IsInitialized => _isInitialized;

    //==============================================================================
    // Unity Lifecycle
    //==============================================================================

    /// <summary>Updates judgement feedback visibility without using a coroutine.</summary>
    private void Update()
    {
        UpdateJudgementFeedbackTimer();
    }

    /// <summary>Removes score service subscriptions before the presenter is destroyed.</summary>
    private void OnDestroy()
    {
        UnsubscribeScoreService();
    }

    /// <summary>Clamps inspector settings when edited in the Unity Inspector.</summary>
    private void OnValidate()
    {
        ClampSettings();
    }

    //==============================================================================
    // Initialization
    //==============================================================================

    /// <summary>
    /// Initializes the gameplay HUD with stage and score services.
    /// </summary>
    /// <param name="chartStageService">Stage service used for the stage name.</param>
    /// <param name="chartScoreService">Score service used for Profit, Combo, and judgement feedback.</param>
    public void Init(
        IChartStageService chartStageService,
        IChartScoreService chartScoreService)
    {
        UnsubscribeScoreService();

        _chartStageService = chartStageService;
        _chartScoreService = chartScoreService;

        ClampSettings();

        if (_autoCreateMissingHud)
        {
            EnsureHudReferences();
        }

        SubscribeScoreService();
        RefreshAllTexts();
        HideJudgementText();

        _isInitialized = true;
        LogMissingDependencies();
    }

    //==============================================================================
    // Internal Logic
    //==============================================================================

    /// <summary>Subscribes to score events if the score service is available.</summary>
    private void SubscribeScoreService()
    {
        if (_chartScoreService == null)
        {
            return;
        }

        _chartScoreService.ScoreStateChanged += HandleScoreStateChanged;
        _chartScoreService.JudgementResultRecorded += HandleJudgementResultRecorded;
    }

    /// <summary>Unsubscribes from score events if the score service is available.</summary>
    private void UnsubscribeScoreService()
    {
        if (_chartScoreService == null)
        {
            return;
        }

        _chartScoreService.ScoreStateChanged -= HandleScoreStateChanged;
        _chartScoreService.JudgementResultRecorded -= HandleJudgementResultRecorded;
    }

    /// <summary>Ensures every HUD reference exists, creating default UI only when missing.</summary>
    private void EnsureHudReferences()
    {
        EnsureCanvasRoot();

        if (_canvasRoot == null)
        {
            return;
        }

        Transform canvasTransform = _canvasRoot.transform;

        if (_stageText == null)
        {
            _stageText = CreateHudText(
                "StageText",
                canvasTransform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(28.0f, -22.0f),
                new Vector2(SIDE_LABEL_WIDTH, TOP_LABEL_HEIGHT),
                30.0f,
                TextAlignmentOptions.Left,
                _primaryTextColor);
        }

        if (_profitText == null)
        {
            _profitText = CreateHudText(
                "ProfitText",
                canvasTransform,
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(28.0f, -66.0f),
                new Vector2(SIDE_LABEL_WIDTH, TOP_LABEL_HEIGHT),
                24.0f,
                TextAlignmentOptions.Left,
                _secondaryTextColor);
        }

        if (_comboText == null)
        {
            _comboText = CreateHudText(
                "ComboText",
                canvasTransform,
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(-28.0f, -24.0f),
                new Vector2(SIDE_LABEL_WIDTH, TOP_LABEL_HEIGHT),
                30.0f,
                TextAlignmentOptions.Right,
                _primaryTextColor);
        }

        if (_buyText == null)
        {
            _buyText = CreateHudText(
                "BuyText",
                canvasTransform,
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(48.0f, 36.0f),
                new Vector2(ACTION_LABEL_WIDTH, ACTION_LABEL_HEIGHT),
                28.0f,
                TextAlignmentOptions.Left,
                _buyColor);
        }

        if (_sellText == null)
        {
            _sellText = CreateHudText(
                "SellText",
                canvasTransform,
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(-48.0f, 36.0f),
                new Vector2(ACTION_LABEL_WIDTH, ACTION_LABEL_HEIGHT),
                28.0f,
                TextAlignmentOptions.Right,
                _sellColor);
        }

        if (_judgementText == null)
        {
            _judgementText = CreateHudText(
                "JudgementText",
                canvasTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.0f, -96.0f),
                new Vector2(JUDGEMENT_LABEL_WIDTH, JUDGEMENT_LABEL_HEIGHT),
                52.0f,
                TextAlignmentOptions.Center,
                _perfectColor);
        }
    }

    /// <summary>Ensures a Canvas exists for generated HUD text.</summary>
    private void EnsureCanvasRoot()
    {
        if (_canvasRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "ChartGameplayHudCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(transform, false);

        _canvasRoot = canvasObject.GetComponent<Canvas>();
        _canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvasRoot.sortingOrder = _canvasSortOrder;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        canvasScaler.matchWidthOrHeight = 0.5f;
    }

    /// <summary>
    /// Creates one TMP HUD text element with stable anchors and fixed dimensions.
    /// </summary>
    /// <param name="name">GameObject name for the text element.</param>
    /// <param name="parent">Parent transform under the HUD canvas.</param>
    /// <param name="anchorMin">Minimum anchor for the text RectTransform.</param>
    /// <param name="anchorMax">Maximum anchor for the text RectTransform.</param>
    /// <param name="pivot">Pivot for the text RectTransform.</param>
    /// <param name="anchoredPosition">Anchored position for the text RectTransform.</param>
    /// <param name="sizeDelta">Fixed text box size.</param>
    /// <param name="fontSize">Initial font size.</param>
    /// <param name="alignment">TMP text alignment.</param>
    /// <param name="color">Initial text color.</param>
    /// <returns>Created TMP text component.</returns>
    private static TMP_Text CreateHudText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        return text;
    }

    /// <summary>Refreshes every HUD text from the current service state.</summary>
    private void RefreshAllTexts()
    {
        RefreshStageText();
        RefreshScoreTexts();
        RefreshActionTexts();
    }

    /// <summary>Refreshes the displayed stage or symbol name.</summary>
    private void RefreshStageText()
    {
        if (_stageText == null)
        {
            return;
        }

        _stageText.text = GetStageDisplayName();
        _stageText.color = _primaryTextColor;
    }

    /// <summary>Refreshes Profit and Combo labels from the score service.</summary>
    private void RefreshScoreTexts()
    {
        if (_profitText != null)
        {
            int profit = _chartScoreService != null ? _chartScoreService.Profit : 0;
            _profitText.text = $"Profit {profit}";
            _profitText.color = _secondaryTextColor;
        }

        if (_comboText != null)
        {
            int combo = _chartScoreService != null ? _chartScoreService.Combo : 0;
            _comboText.text = $"Combo {combo}";
            _comboText.color = _primaryTextColor;
        }
    }

    /// <summary>Refreshes fixed BUY and SELL input labels.</summary>
    private void RefreshActionTexts()
    {
        if (_buyText != null)
        {
            _buyText.text = "BUY";
            _buyText.color = _buyColor;
        }

        if (_sellText != null)
        {
            _sellText.text = "SELL";
            _sellText.color = _sellColor;
        }
    }

    /// <summary>Handles score state changes emitted by ChartScoreController.</summary>
    private void HandleScoreStateChanged()
    {
        RefreshStageText();
        RefreshScoreTexts();
    }

    /// <summary>
    /// Shows latest judgement feedback from ChartScoreController.
    /// </summary>
    /// <param name="judgementResult">Latest recorded judgement result.</param>
    private void HandleJudgementResultRecorded(ChartJudgementResult judgementResult)
    {
        if (judgementResult.IsValid == false)
        {
            return;
        }

        if (_judgementText == null)
        {
            return;
        }

        _judgementText.text = GetJudgementDisplayText(judgementResult.Grade);
        _judgementText.color = GetJudgementDisplayColor(judgementResult.Grade);
        _judgementText.gameObject.SetActive(true);
        _judgementDisplayTimeRemaining = _judgementDisplaySeconds;
    }

    /// <summary>Hides judgement feedback after its display timer ends.</summary>
    private void UpdateJudgementFeedbackTimer()
    {
        if (_judgementText == null)
        {
            return;
        }

        if (_judgementText.gameObject.activeSelf == false)
        {
            return;
        }

        _judgementDisplayTimeRemaining -= Time.deltaTime;

        if (_judgementDisplayTimeRemaining > 0.0f)
        {
            return;
        }

        HideJudgementText();
    }

    /// <summary>Hides the judgement feedback text and clears its timer.</summary>
    private void HideJudgementText()
    {
        _judgementDisplayTimeRemaining = 0.0f;

        if (_judgementText == null)
        {
            return;
        }

        _judgementText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Gets a safe stage display name for the HUD.
    /// </summary>
    /// <returns>Stage display name or the MVP fallback symbol name.</returns>
    private string GetStageDisplayName()
    {
        if (_chartStageService == null)
        {
            return DEFAULT_STAGE_NAME;
        }

        if (string.IsNullOrWhiteSpace(_chartStageService.DisplayName))
        {
            return DEFAULT_STAGE_NAME;
        }

        return _chartStageService.DisplayName;
    }

    /// <summary>
    /// Converts a judgement grade to HUD text.
    /// </summary>
    /// <param name="grade">Judgement grade to display.</param>
    /// <returns>Readable judgement label.</returns>
    private static string GetJudgementDisplayText(ChartJudgementGrade grade)
    {
        switch (grade)
        {
            case ChartJudgementGrade.JUDGEMENT_GRADE_PERFECT:
                return "PERFECT";
            case ChartJudgementGrade.JUDGEMENT_GRADE_GREAT:
                return "GREAT";
            case ChartJudgementGrade.JUDGEMENT_GRADE_GOOD:
                return "GOOD";
            case ChartJudgementGrade.JUDGEMENT_GRADE_MISS:
                return "MISS";
            default:
                return "MISS";
        }
    }

    /// <summary>
    /// Gets the HUD color for a judgement grade.
    /// </summary>
    /// <param name="grade">Judgement grade to color.</param>
    /// <returns>Configured judgement color.</returns>
    private Color GetJudgementDisplayColor(ChartJudgementGrade grade)
    {
        switch (grade)
        {
            case ChartJudgementGrade.JUDGEMENT_GRADE_PERFECT:
                return _perfectColor;
            case ChartJudgementGrade.JUDGEMENT_GRADE_GREAT:
                return _greatColor;
            case ChartJudgementGrade.JUDGEMENT_GRADE_GOOD:
                return _goodColor;
            case ChartJudgementGrade.JUDGEMENT_GRADE_MISS:
                return _missColor;
            default:
                return _missColor;
        }
    }

    /// <summary>Clamps editable settings to safe runtime values.</summary>
    private void ClampSettings()
    {
        if (_canvasSortOrder < 0)
        {
            _canvasSortOrder = DEFAULT_CANVAS_SORT_ORDER;
        }

        if (_judgementDisplaySeconds < MIN_JUDGEMENT_DISPLAY_SECONDS)
        {
            _judgementDisplaySeconds = MIN_JUDGEMENT_DISPLAY_SECONDS;
        }
    }

    /// <summary>Logs missing dependencies during editor and development builds.</summary>
    private void LogMissingDependencies()
    {
        if (_chartStageService == null)
        {
            LogWarning("ChartStage service is not connected. HUD will use the default stage name.");
        }

        if (_chartScoreService == null)
        {
            LogWarning("ChartScore service is not connected. HUD score values will not update.");
        }

        if (_canvasRoot == null)
        {
            LogWarning("Canvas root is not connected. HUD text cannot be displayed.");
        }
    }

    /// <summary>Logs a HUD presenter warning in editor and development builds.</summary>
    /// <param name="message">Warning message to write.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartGameplayHudPresenter] {message}");
    }
}
