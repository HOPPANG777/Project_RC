using UnityEngine;

//==============================================================================
// StockRhythmStageState
//==============================================================================
// Owns mutable stage timers, score counters, judgement state, and result math.
//==============================================================================
internal sealed class StockRhythmStageState
{
    // Current stage time in seconds.
    public float GameTime { get; private set; }

    // Next note generation hit time.
    public float NextNoteTime { get; private set; }

    // Next beat tick time for generated metronome sounds.
    public float NextBeatTick { get; set; }

    // Previous generated note Y position.
    public float LastY { get; private set; }

    // Current generated market trend direction.
    public int Trend { get; private set; }

    // Current score.
    public int Score { get; private set; }

    // Current combo count.
    public int Combo { get; private set; }

    // Highest combo count reached this stage.
    public int MaxCombo { get; private set; }

    // Successful hit count.
    public int Hits { get; private set; }

    // Perfect judgement count.
    public int Perfects { get; private set; }

    // Great judgement count.
    public int Greats { get; private set; }

    // Good judgement count.
    public int Goods { get; private set; }

    // Missed note count.
    public int Misses { get; private set; }

    // Wrong-side input count.
    public int Wrongs { get; private set; }

    // Extra input count outside the timing window.
    public int ExtraInputs { get; private set; }

    // Fever gauge amount from 0 to 1.
    public float Fever { get; private set; }

    // Remaining active fever seconds.
    public float FeverTimer { get; private set; }

    // Target Y position for the timing marker.
    public float MarkerY { get; private set; }

    // SmoothDamp velocity for the timing marker.
    public float MarkerVelocity { get; set; }

    // Current state of the stage.
    public StockRhythmPlayState PlayState { get; private set; }

    // Last judgement label shown in the HUD.
    public string LastJudge { get; private set; } = "READY";

    // Whether fever score multiplier is currently active.
    public bool IsFeverActive => PlayState == StockRhythmPlayState.PLAYING && FeverTimer > 0f;

    // Total count of hit, miss, wrong-side, and extra-input attempts.
    public int Attempts => Hits + Misses + Wrongs + ExtraInputs;

    // Miss-like attempts used by the compact HUD.
    public int HudMisses => Misses + Wrongs;

    // Accuracy value from 0 to 100 for HUD display.
    public float AccuracyPercent => Attempts > 0 ? (float)Hits / Attempts * 100f : 100f;

    // Accuracy value from 0 to 1 for result display.
    public float AccuracyRatio => Attempts > 0 ? (float)Hits / Attempts : 1f;

    // Resets every mutable field to the initial playable stage state.
    public void Reset()
    {
        GameTime = 0f;
        NextNoteTime = 1.4f;
        NextBeatTick = 0f;
        LastY = -0.9f;
        Trend = 1;
        Score = 0;
        Combo = 0;
        MaxCombo = 0;
        Hits = 0;
        Perfects = 0;
        Greats = 0;
        Goods = 0;
        Misses = 0;
        Wrongs = 0;
        ExtraInputs = 0;
        Fever = 0f;
        FeverTimer = 0f;
        MarkerY = LastY;
        MarkerVelocity = 0f;
        LastJudge = "READY";
        PlayState = StockRhythmPlayState.PLAYING;
    }

    // Advances the stage clock.
    public void AdvanceTime(float deltaTime)
    {
        GameTime += deltaTime;
    }

    // Stores generated note state and schedules the next note.
    public void CommitGeneratedNote(float nextY, int trend, float interval)
    {
        LastY = nextY;
        Trend = trend;
        NextNoteTime += interval;
    }

    // Applies an extra input outside the timing window.
    public void RegisterExtraInput(StockRhythmTradeAction action)
    {
        ExtraInputs++;
        Combo = 0;
        Score = Mathf.Max(0, Score - 100);
        LastJudge = action == StockRhythmTradeAction.BUY ? "TOO EARLY BUY" : "TOO EARLY SELL";
    }

    // Applies a wrong-side input against an active note.
    public void RegisterWrongInput()
    {
        Wrongs++;
        Combo = 0;
        Score = Mathf.Max(0, Score - 150);
        LastJudge = "WRONG SIDE";
    }

    // Applies a missed note.
    public void RegisterMiss()
    {
        Misses++;
        Combo = 0;
        LastJudge = "MISS";
    }

    // Applies scoring, combo, marker movement, and fever gain after a valid hit.
    public void RegisterSuccessfulHit(float markerY, float error)
    {
        bool feverWasActive = IsFeverActive;
        Hits++;
        Combo++;
        MaxCombo = Mathf.Max(MaxCombo, Combo);
        MarkerY = markerY;
        MarkerVelocity = 0f;

        int baseScore;
        float feverGain;
        if (error <= StockRhythmConstants.PERFECT_WINDOW)
        {
            Perfects++;
            baseScore = 1000;
            feverGain = 0.085f;
            LastJudge = "PERFECT";
        }
        else if (error <= StockRhythmConstants.GREAT_WINDOW)
        {
            Greats++;
            baseScore = 650;
            feverGain = 0.055f;
            LastJudge = "GREAT";
        }
        else
        {
            Goods++;
            baseScore = 350;
            feverGain = 0.035f;
            LastJudge = "GOOD";
        }

        float multiplier = feverWasActive ? 2f : 1f;
        Score += Mathf.RoundToInt((baseScore + Combo * 12) * multiplier);
        if (!feverWasActive)
        {
            Fever = Mathf.Clamp01(Fever + feverGain);
            if (Fever >= 1f)
            {
                FeverTimer = StockRhythmConstants.FEVER_SECONDS;
                LastJudge = "FEVER";
            }
        }
    }

    // Ticks the fever timer and clears the gauge when fever ends.
    public void TickFever(float deltaTime)
    {
        if (!IsFeverActive)
        {
            return;
        }

        FeverTimer -= deltaTime;
        Fever = Mathf.Clamp01(FeverTimer / StockRhythmConstants.FEVER_SECONDS);
        if (FeverTimer <= 0f)
        {
            FeverTimer = 0f;
            Fever = 0f;
        }
    }

    // Moves the state machine into paused state.
    public void Pause()
    {
        PlayState = StockRhythmPlayState.PAUSED;
        LastJudge = "PAUSED";
    }

    // Moves the state machine back into playing state.
    public void Resume()
    {
        PlayState = StockRhythmPlayState.PLAYING;
        LastJudge = "READY";
    }

    // Moves the state machine into finished state.
    public void Finish()
    {
        PlayState = StockRhythmPlayState.FINISHED;
    }

    // Calculates result values for the end-of-stage panel.
    public StockRhythmResultSnapshot CreateResult()
    {
        float judgementBonus = Perfects * 1.2f + Greats * 0.75f + Goods * 0.35f;
        float riskPenalty = Misses * 0.7f + Wrongs * 1.1f + ExtraInputs * 0.3f;
        float returnPercent = Mathf.Clamp((Score / 90000f) + judgementBonus * 0.018f - riskPenalty * 0.026f, -0.18f, 0.42f);
        float seedMoney = StockRhythmConstants.INITIAL_SEED_MONEY * (1f + returnPercent);

        return new StockRhythmResultSnapshot(
            Score,
            AccuracyRatio,
            MaxCombo,
            Perfects,
            Greats,
            Goods,
            returnPercent,
            seedMoney);
    }
}
