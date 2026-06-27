using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//==============================================================================
// StockRhythmController
//==============================================================================
// Coordinates stage flow, player input, note judgement, and result transitions.
//==============================================================================
public sealed class StockRhythmController
{
    // Active chart notes for the current stage.
    private readonly List<StockRhythmNote> _notes = new List<StockRhythmNote>();

    // Root dependency injected by GameRoot.
    private GameRoot _gameRoot;

    // Runtime-generated assets and audio playback.
    private StockRhythmRuntimeAssets _runtimeAssets;

    // Stage presentation layer for world objects and UI.
    private StockRhythmStageView _stageView;

    // Mutable gameplay state for the current stage.
    private StockRhythmStageState _stageState;

    // Deterministic random generator for reproducible sample charts.
    private System.Random _random;

    // Injects GameRoot dependency.
    public void Construct(GameRoot gameRoot)
    {
        _gameRoot = gameRoot;
    }

    // Creates generated assets, presentation objects, and the first stage.
    public void Initialize()
    {
        _random = new System.Random(314159);
        _stageState = new StockRhythmStageState();

        _runtimeAssets = new StockRhythmRuntimeAssets();
        _runtimeAssets.Construct(_gameRoot);
        _runtimeAssets.Initialize();

        _stageView = new StockRhythmStageView();
        _stageView.Construct(
            _gameRoot,
            _runtimeAssets,
            TogglePause,
            () => HandleTradeInput(StockRhythmTradeAction.BUY),
            () => HandleTradeInput(StockRhythmTradeAction.SELL),
            ResetStage);
        _stageView.Initialize(_random);

        ResetStage();
    }

    // Runs one controller frame from GameRoot.Update.
    public void Tick(float deltaTime)
    {
        PollKeyboard();

        if (_stageState.PlayState != StockRhythmPlayState.PLAYING)
        {
            return;
        }

        _stageState.AdvanceTime(deltaTime);
        GenerateNotesUntil(_stageState.GameTime + StockRhythmConstants.SPAWN_LEAD_TIME + 1.5f);
        _stageState.TickFever(deltaTime);
        UpdateBeat();
        _stageView.UpdateNotes(_notes, _stageState.GameTime);
        UpdateMarker(false);
        CheckMisses();
        CleanupOldNotes();
        _stageView.UpdateHud(_stageState);

        if (_stageState.GameTime >= StockRhythmConstants.STAGE_DURATION)
        {
            EndStage();
        }
    }

    // Releases transient runtime resources created by the controller.
    public void Dispose()
    {
        Time.timeScale = 1f;
        _runtimeAssets?.Dispose();
    }

    // Resets score, notes, timers, and chart generation state.
    private void ResetStage()
    {
        Time.timeScale = 1f;

        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            _stageView.DestroyNoteVisuals(_notes[i]);
        }

        _notes.Clear();
        _random = new System.Random(314159);
        _stageState.Reset();
        _stageView.HideResult();
        _stageView.SetPauseButtonLabel("II");

        GenerateNotesUntil(StockRhythmConstants.SPAWN_LEAD_TIME + 4f);
        _stageView.UpdateNotes(_notes, _stageState.GameTime);
        UpdateMarker(true);
        _stageView.UpdateHud(_stageState);
    }

    // Generates notes until the requested future time is covered.
    private void GenerateNotesUntil(float aheadTime)
    {
        while (_stageState.NextNoteTime < aheadTime &&
               _stageState.NextNoteTime <= StockRhythmConstants.STAGE_DURATION + 1f)
        {
            AddNote();
        }
    }

    // Creates one rule-based BUY or SELL note from the current virtual market trend.
    private void AddNote()
    {
        float difficulty = Mathf.Clamp01(_stageState.NextNoteTime / StockRhythmConstants.STAGE_DURATION);
        int trend = _stageState.Trend;
        if (_random.NextDouble() < 0.18 + difficulty * 0.07)
        {
            trend *= -1;
        }

        if (_stageState.LastY > 2.55f)
        {
            trend = -1;
        }
        else if (_stageState.LastY < -1.85f)
        {
            trend = 1;
        }

        float volatility = 0.38f + (float)_random.NextDouble() * (0.35f + difficulty * 0.25f);
        float nextY = Mathf.Clamp(_stageState.LastY + trend * volatility, -2.05f, 2.75f);
        StockRhythmTradeAction action = trend > 0 ? StockRhythmTradeAction.BUY : StockRhythmTradeAction.SELL;
        StockRhythmNote previous = _notes.Count > 0 ? _notes[_notes.Count - 1] : null;

        StockRhythmNote note = new StockRhythmNote
        {
            HitTime = _stageState.NextNoteTime,
            Y = nextY,
            Action = action,
            Previous = previous
        };

        _stageView.CreateNoteVisuals(note, _random);
        _notes.Add(note);

        float interval = Mathf.Lerp(0.78f, 0.50f, difficulty);
        if (_random.NextDouble() < 0.17)
        {
            interval += StockRhythmConstants.BASE_BEAT_SECONDS * 0.5f;
        }

        _stageState.CommitGeneratedNote(nextY, trend, interval);
    }

    // Reads keyboard controls and dispatches gameplay commands.
    private void PollKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame && _stageState.PlayState == StockRhythmPlayState.FINISHED)
        {
            ResetStage();
        }

        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        if (_stageState.PlayState != StockRhythmPlayState.PLAYING)
        {
            return;
        }

        if (keyboard.leftShiftKey.wasPressedThisFrame)
        {
            HandleTradeInput(StockRhythmTradeAction.BUY);
        }

        if (keyboard.rightShiftKey.wasPressedThisFrame)
        {
            HandleTradeInput(StockRhythmTradeAction.SELL);
        }
    }

    // Judges one BUY or SELL input against the nearest active note.
    private void HandleTradeInput(StockRhythmTradeAction action)
    {
        if (_stageState.PlayState != StockRhythmPlayState.PLAYING)
        {
            return;
        }

        StockRhythmNote target = FindJudgeTarget();
        if (target == null)
        {
            _stageState.RegisterExtraInput(action);
            _runtimeAssets.PlayClip(_runtimeAssets.MissClip, 0.75f);
            _stageView.UpdateHud(_stageState);
            return;
        }

        float error = Mathf.Abs(target.HitTime - _stageState.GameTime);
        if (target.Action != action)
        {
            target.Judged = true;
            _stageState.RegisterWrongInput();
            _stageView.FadeNote(target, new Color(0.45f, 0.45f, 0.45f, 0.35f));
            _runtimeAssets.PlayClip(_runtimeAssets.MissClip, 0.9f);
            _stageView.UpdateHud(_stageState);
            return;
        }

        ApplySuccessfulHit(target, error);
    }

    // Applies score, combo, marker movement, and fever gain after a valid hit.
    private void ApplySuccessfulHit(StockRhythmNote target, float error)
    {
        target.Judged = true;
        _stageState.RegisterSuccessfulHit(target.Y, error);
        _stageView.FadeNote(target, Color.white);
        _runtimeAssets.PlayClip(_runtimeAssets.HitClip, _stageState.IsFeverActive ? 1.15f : 0.9f);
        _stageView.UpdateHud(_stageState);
    }

    // Finds the nearest unjudged note inside the Good timing window.
    private StockRhythmNote FindJudgeTarget()
    {
        StockRhythmNote best = null;
        float bestError = StockRhythmConstants.GOOD_WINDOW;
        for (int i = 0; i < _notes.Count; i++)
        {
            StockRhythmNote note = _notes[i];
            if (note.Judged)
            {
                continue;
            }

            float error = Mathf.Abs(note.HitTime - _stageState.GameTime);
            if (error <= bestError)
            {
                best = note;
                bestError = error;
            }
        }

        return best;
    }

    // Plays metronome ticks and pulses the judgement line.
    private void UpdateBeat()
    {
        while (_stageState.GameTime >= _stageState.NextBeatTick)
        {
            _runtimeAssets.PlayClip(_runtimeAssets.BeatClip, _stageState.IsFeverActive ? 0.9f : 0.55f);
            _stageState.NextBeatTick += StockRhythmConstants.BASE_BEAT_SECONDS;
        }

        _stageView.UpdateBeat(_stageState.GameTime, _stageState.IsFeverActive);
    }

    // Moves the timing marker toward the last successfully hit note.
    private void UpdateMarker(bool instant)
    {
        float markerVelocity = _stageState.MarkerVelocity;
        _stageView.UpdateMarker(_stageState.MarkerY, ref markerVelocity, _stageState.IsFeverActive, instant);
        _stageState.MarkerVelocity = markerVelocity;
    }

    // Converts overdue unjudged notes into misses.
    private void CheckMisses()
    {
        for (int i = 0; i < _notes.Count; i++)
        {
            StockRhythmNote note = _notes[i];
            if (note.Judged)
            {
                continue;
            }

            if (_stageState.GameTime - note.HitTime > StockRhythmConstants.GOOD_WINDOW)
            {
                note.Judged = true;
                _stageState.RegisterMiss();
                _stageView.FadeNote(note, new Color(0.55f, 0.55f, 0.55f, 0.25f));
                _runtimeAssets.PlayClip(_runtimeAssets.MissClip, 0.6f);
            }
        }
    }

    // Removes old note visuals after they scroll behind the player.
    private void CleanupOldNotes()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            StockRhythmNote note = _notes[i];
            if (_stageState.GameTime - note.HitTime > StockRhythmConstants.CLEANUP_BEHIND_TIME)
            {
                _stageView.DestroyNoteVisuals(note);
                _notes.RemoveAt(i);
            }
        }
    }

    // Toggles between playing and paused states.
    private void TogglePause()
    {
        if (_stageState.PlayState == StockRhythmPlayState.FINISHED)
        {
            return;
        }

        if (_stageState.PlayState == StockRhythmPlayState.PAUSED)
        {
            _stageState.Resume();
            Time.timeScale = 1f;
            _stageView.SetPauseButtonLabel("II");
        }
        else if (_stageState.PlayState == StockRhythmPlayState.PLAYING)
        {
            _stageState.Pause();
            Time.timeScale = 0f;
            _stageView.SetPauseButtonLabel(">");
        }

        _stageView.UpdateHud(_stageState);
    }

    // Calculates and displays the final result screen.
    private void EndStage()
    {
        _stageState.Finish();
        Time.timeScale = 1f;
        _stageView.ShowResult(_stageState.CreateResult());
    }
}
