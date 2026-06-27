using UnityEngine;

//==============================================================================
// StockRhythmTypes
//==============================================================================
// Shared constants, enums, and lightweight data bundles for Stock Rhythm.
//==============================================================================

// Trade action enum values are named with screaming snake case by convention.
internal enum StockRhythmTradeAction
{
    BUY,
    SELL
}

// Play state enum values are named with screaming snake case by convention.
internal enum StockRhythmPlayState
{
    READY,
    PLAYING,
    PAUSED,
    FINISHED
}

// Shared tuning constants for the Stock Rhythm prototype.
internal static class StockRhythmConstants
{
    // Total prototype stage duration in seconds.
    public const float STAGE_DURATION = 60f;

    // Initial virtual seed money used in the result screen.
    public const float INITIAL_SEED_MONEY = 1000000f;

    // X position of the timing judgement line.
    public const float JUDGE_X = -4.55f;

    // Horizontal note scroll speed in world units per second.
    public const float NOTE_SPEED = 4.65f;

    // Seconds ahead of the judgement line where notes are generated.
    public const float SPAWN_LEAD_TIME = 3.2f;

    // Seconds after judgement before old visuals are removed.
    public const float CLEANUP_BEHIND_TIME = 1.2f;

    // Perfect judgement timing window.
    public const float PERFECT_WINDOW = 0.075f;

    // Great judgement timing window.
    public const float GREAT_WINDOW = 0.135f;

    // Good judgement timing window.
    public const float GOOD_WINDOW = 0.22f;

    // Base metronome beat length for generated click sounds.
    public const float BASE_BEAT_SECONDS = 60f / 128f;

    // Active fever duration in seconds.
    public const float FEVER_SECONDS = 6f;
}

// Runtime note model used for judgement and chart drawing.
internal sealed class StockRhythmNote
{
    // Target hit time in stage seconds.
    public float HitTime { get; set; }

    // Vertical chart lane position.
    public float Y { get; set; }

    // Required BUY or SELL action for this note.
    public StockRhythmTradeAction Action { get; set; }

    // Whether the note has already been judged.
    public bool Judged { get; set; }

    // Previous note used for chart segment drawing.
    public StockRhythmNote Previous { get; set; }

    // Visual bundle for the button-like note marker.
    public StockRhythmNoteView NoteView { get; set; }

    // Visual bundle for the candle under the note.
    public StockRhythmCandleView CandleView { get; set; }

    // Line segment connecting this note to the previous note.
    public LineRenderer Segment { get; set; }
}

// Visual references for one BUY/SELL note.
internal sealed class StockRhythmNoteView
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
internal sealed class StockRhythmCandleView
{
    // Root GameObject for this candle visual.
    public GameObject Root { get; set; }

    // Candle body renderer.
    public SpriteRenderer Body { get; set; }

    // Candle wick line renderer.
    public LineRenderer Wick { get; set; }
}

// Read-only result values displayed at the end of the stage.
internal readonly struct StockRhythmResultSnapshot
{
    // Final score value.
    public int Score { get; }

    // Accuracy value from 0 to 1.
    public float Accuracy { get; }

    // Highest combo count reached this stage.
    public int MaxCombo { get; }

    // Perfect judgement count.
    public int Perfects { get; }

    // Great judgement count.
    public int Greats { get; }

    // Good judgement count.
    public int Goods { get; }

    // Virtual stock return ratio.
    public float ReturnPercent { get; }

    // Final virtual seed money.
    public float SeedMoney { get; }

    // Creates an immutable result snapshot.
    public StockRhythmResultSnapshot(
        int score,
        float accuracy,
        int maxCombo,
        int perfects,
        int greats,
        int goods,
        float returnPercent,
        float seedMoney)
    {
        Score = score;
        Accuracy = accuracy;
        MaxCombo = maxCombo;
        Perfects = perfects;
        Greats = greats;
        Goods = goods;
        ReturnPercent = returnPercent;
        SeedMoney = seedMoney;
    }
}
