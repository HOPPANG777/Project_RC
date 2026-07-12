using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// Feature: class::ChartScoreController
// Description: Converts judgement results into gameplay Profit, Combo, and Accuracy state.
//==============================================================================
// Implementation notes
// - JudgementController remains the source of truth for judgement grade and score.
// - This controller consumes buffered judgement results and publishes score events for HUD/result views.
// - Combo increases on hit judgements and resets on MISS.
//==============================================================================

[DisallowMultipleComponent]
public class ChartScoreController : MonoBehaviour, IChartScoreService
{
    //==============================================================================
    // Constants
    //==============================================================================

    // Converts a normalized multiplier sum into a readable percentage value.
    private const float PERCENT_MULTIPLIER = 100.0f;

    //==============================================================================
    // Internal State
    //==============================================================================

    // Whether Init has supplied the required score dependencies.
    private bool _isInitialized;

    // Current play session profit.
    private int _profit;

    // Current uninterrupted hit combo.
    private int _combo;

    // Highest combo reached in the current play session.
    private int _maxCombo;

    // Number of required notes that have produced a judgement result.
    private int _judgedNoteCount;

    // Number of required notes in the current stage.
    private int _totalNoteCount;

    // Sum of judgement multipliers used for accuracy calculation.
    private float _multiplierSum;

    // Most recent valid judgement result recorded by this controller.
    private ChartJudgementResult _lastJudgementResult;

    // Whether a valid judgement has been recorded since the latest reset.
    private bool _hasLastJudgementResult;

    // Judgement service that buffers note judgement results.
    private IChartJudgementService _chartJudgementService;

    // Stage service used to count required notes for the current stage.
    private IChartStageService _chartStageService;

    //==============================================================================
    // Properties
    //==============================================================================

    /// <summary>Raised whenever profit, combo, accuracy, or judgement counters change.</summary>
    public event Action ScoreStateChanged;

    /// <summary>Raised after a valid judgement result has been recorded into the score state.</summary>
    public event Action<ChartJudgementResult> JudgementResultRecorded;

    /// <summary>Whether Init has supplied the required score dependencies.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Initialized")]
    public bool IsInitialized => _isInitialized;

    /// <summary>Current play session profit.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Profit")]
    public int Profit => _profit;

    /// <summary>Current uninterrupted hit combo.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Combo")]
    public int Combo => _combo;

    /// <summary>Highest combo reached in the current play session.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Max Combo")]
    public int MaxCombo => _maxCombo;

    /// <summary>Number of required notes that have produced a judgement result.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Judged Notes")]
    public int JudgedNoteCount => _judgedNoteCount;

    /// <summary>Number of required notes in the current stage.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Total Notes")]
    public int TotalNoteCount => _totalNoteCount;

    /// <summary>Current accuracy percentage calculated from judgement multipliers.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Accuracy")]
    public float Accuracy
    {
        get
        {
            if (_totalNoteCount <= 0)
            {
                return 0.0f;
            }

            return (_multiplierSum / _totalNoteCount) * PERCENT_MULTIPLIER;
        }
    }

    /// <summary>Whether a valid judgement has been recorded since the latest reset.</summary>
    public bool HasLastJudgementResult => _hasLastJudgementResult;

    /// <summary>Most recent valid judgement result recorded by this controller.</summary>
    public ChartJudgementResult LastJudgementResult => _lastJudgementResult;

    //==============================================================================
    // Unity Lifecycle
    //==============================================================================

    /// <summary>Consumes pending judgement results every frame after initialization.</summary>
    private void Update()
    {
        if (CanConsumeJudgementResults() == false)
        {
            return;
        }

        ConsumeBufferedJudgementResults();
    }

    //==============================================================================
    // Initialization
    //==============================================================================

    /// <summary>
    /// Initializes score accumulation from judgement and stage services.
    /// </summary>
    /// <param name="chartJudgementService">Service that emits judgement results.</param>
    /// <param name="chartStageService">Service that provides current stage note data.</param>
    public void Init(
        IChartJudgementService chartJudgementService,
        IChartStageService chartStageService)
    {
        _chartJudgementService = chartJudgementService;
        _chartStageService = chartStageService;

        ResetScoreState();

        _isInitialized = true;
        LogMissingDependencies();
    }

    //==============================================================================
    // Public API
    //==============================================================================

    /// <summary>Clears score counters for the current stage before gameplay starts.</summary>
    public void ResetScoreState()
    {
        _profit = 0;
        _combo = 0;
        _maxCombo = 0;
        _judgedNoteCount = 0;
        _totalNoteCount = CalculateTotalRequiredNoteCount();
        _multiplierSum = 0.0f;
        _lastJudgementResult = default;
        _hasLastJudgementResult = false;

        ScoreStateChanged?.Invoke();
    }

    //==============================================================================
    // Internal Logic
    //==============================================================================

    /// <summary>Checks whether buffered judgement results can be consumed.</summary>
    /// <returns>True when the judgement service is ready to provide results.</returns>
    private bool CanConsumeJudgementResults()
    {
        if (_isInitialized == false)
        {
            return false;
        }

        if (_chartJudgementService == null)
        {
            return false;
        }

        return _chartJudgementService.IsInitialized;
    }

    /// <summary>Consumes every pending judgement result from the judgement service buffer.</summary>
    private void ConsumeBufferedJudgementResults()
    {
        while (_chartJudgementService.TryConsumeResult(out ChartJudgementResult judgementResult))
        {
            RecordJudgementResult(judgementResult);
        }
    }

    /// <summary>
    /// Applies a valid judgement result to Profit, Combo, and Accuracy state.
    /// </summary>
    /// <param name="judgementResult">Judgement result emitted by ChartJudgementController.</param>
    private void RecordJudgementResult(ChartJudgementResult judgementResult)
    {
        if (judgementResult.IsValid == false)
        {
            return;
        }

        _profit += judgementResult.Score;
        _combo = GetNextCombo(judgementResult);
        _maxCombo = Mathf.Max(_maxCombo, _combo);
        _judgedNoteCount++;
        _multiplierSum += Mathf.Clamp01(judgementResult.Multiplier);
        _lastJudgementResult = judgementResult;
        _hasLastJudgementResult = true;

        JudgementResultRecorded?.Invoke(judgementResult);
        ScoreStateChanged?.Invoke();
    }

    /// <summary>
    /// Calculates the next combo value from the latest judgement result.
    /// </summary>
    /// <param name="judgementResult">Latest judgement result.</param>
    /// <returns>Next combo value after applying the judgement.</returns>
    private int GetNextCombo(ChartJudgementResult judgementResult)
    {
        if (judgementResult.IsHit == false)
        {
            return 0;
        }

        return _combo + 1;
    }

    /// <summary>
    /// Counts required notes in the current stage for accuracy and progress display.
    /// </summary>
    /// <returns>Number of required notes in the current stage.</returns>
    private int CalculateTotalRequiredNoteCount()
    {
        if (_chartStageService == null)
        {
            return 0;
        }

        IReadOnlyList<ChartPlacedNote> placedNotes = _chartStageService.PlacedNotes;

        if (placedNotes == null)
        {
            return 0;
        }

        int requiredNoteCount = 0;

        for (int i = 0; i < placedNotes.Count; i++)
        {
            if (IsRequiredScoreNote(placedNotes[i]) == false)
            {
                continue;
            }

            requiredNoteCount++;
        }

        return requiredNoteCount;
    }

    /// <summary>
    /// Checks whether a placed note should contribute to score totals.
    /// </summary>
    /// <param name="placedNote">Placed note to inspect.</param>
    /// <returns>True when the note is required for judgement and scoring.</returns>
    private static bool IsRequiredScoreNote(ChartPlacedNote placedNote)
    {
        if (placedNote == null)
        {
            return false;
        }

        if (placedNote.Data == null)
        {
            return false;
        }

        return placedNote.Data.IsRequired;
    }

    /// <summary>Logs missing dependencies during editor and development builds.</summary>
    private void LogMissingDependencies()
    {
        if (_chartJudgementService == null)
        {
            LogWarning("ChartJudgement service is not connected. Score cannot update from judgements.");
        }

        if (_chartStageService == null)
        {
            LogWarning("ChartStage service is not connected. Total note count cannot be calculated.");
        }
    }

    /// <summary>Logs a score controller warning in editor and development builds.</summary>
    /// <param name="message">Warning message to write.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartScoreController] {message}");
    }
}
