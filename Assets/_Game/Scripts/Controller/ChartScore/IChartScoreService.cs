using System;

//==============================================================================
// Feature: interface::IChartScoreService
// Description: Public score contract exposed by GameRoot for gameplay HUD and result flow.
//==============================================================================

/// <summary>
/// Provides the gameplay score state generated from chart judgement results.
/// </summary>
public interface IChartScoreService
{
    /// <summary>Raised whenever profit, combo, accuracy, or judgement counters change.</summary>
    event Action ScoreStateChanged;

    /// <summary>Raised after a valid judgement result has been recorded into the score state.</summary>
    event Action<ChartJudgementResult> JudgementResultRecorded;

    /// <summary>Whether the service has received its GameRoot dependencies.</summary>
    bool IsInitialized { get; }

    /// <summary>Total accumulated profit for the current play session.</summary>
    int Profit { get; }

    /// <summary>Current uninterrupted hit combo.</summary>
    int Combo { get; }

    /// <summary>Highest combo reached during the current play session.</summary>
    int MaxCombo { get; }

    /// <summary>Number of required notes that have produced a judgement result.</summary>
    int JudgedNoteCount { get; }

    /// <summary>Number of required notes in the current stage.</summary>
    int TotalNoteCount { get; }

    /// <summary>Current accuracy percentage calculated from judgement multipliers.</summary>
    float Accuracy { get; }

    /// <summary>Whether a valid judgement result has been recorded after the latest reset.</summary>
    bool HasLastJudgementResult { get; }

    /// <summary>Most recent valid judgement result recorded after the latest reset.</summary>
    ChartJudgementResult LastJudgementResult { get; }

    /// <summary>Clears score counters for the current stage before gameplay starts.</summary>
    void ResetScoreState();
}
