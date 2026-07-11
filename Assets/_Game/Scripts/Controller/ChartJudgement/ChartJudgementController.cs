using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartJudgementController
// 설명: BUY / SELL 입력과 차트 노트를 비교해 PERFECT / GREAT / GOOD / MISS 결과를 생성하는 컨트롤러
//==============================================================================
// 세부 구현 사항
// - 입력 시간은 AudioSettings.dspTime을 ChartRun 기준 스테이지 시간으로 변환해 사용합니다.
// - 40ms / 60ms / 100ms 판정 폭과 점수 배율은 SAMSU MVP 문서 기준을 따릅니다.
// - ScoreController가 결과를 순서대로 소비할 수 있도록 ChartJudgementResult 버퍼를 제공합니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartJudgementController : MonoBehaviour, IChartJudgementService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // SAMSU MVP 기준 PERFECT 판정 폭입니다.
    private const float DEFAULT_PERFECT_TIME_WINDOW = 0.04f;

    // SAMSU MVP 기준 GREAT 판정 폭입니다.
    private const float DEFAULT_GREAT_TIME_WINDOW = 0.06f;

    // SAMSU MVP 기준 GOOD 판정 폭이자 MISS 처리 기준입니다.
    private const float DEFAULT_GOOD_TIME_WINDOW = 0.10f;

    // PERFECT 판정 점수입니다.
    private const int SCORE_PERFECT = 100;

    // GREAT 판정 점수입니다.
    private const int SCORE_GREAT = 70;

    // GOOD 판정 점수입니다.
    private const int SCORE_GOOD = 50;

    // MISS 판정 점수입니다.
    private const int SCORE_MISS = 0;

    // PERFECT 판정 배율입니다.
    private const float MULTIPLIER_PERFECT = 1.0f;

    // GREAT 판정 배율입니다.
    private const float MULTIPLIER_GREAT = 0.7f;

    // GOOD 판정 배율입니다.
    private const float MULTIPLIER_GOOD = 0.5f;

    // MISS 판정 배율입니다.
    private const float MULTIPLIER_MISS = 0.0f;

    // 판정 결과 버퍼가 비정상적으로 0 이하가 되는 상황을 막기 위한 최소값입니다.
    private const int MIN_BUFFERED_RESULT_COUNT = 1;

    // ScoreController 소비가 잠시 늦어져도 결과를 잃지 않기 위한 기본 버퍼 크기입니다.
    private const int DEFAULT_BUFFERED_RESULT_COUNT = 64;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Settings")]
    [Tooltip("GameRoot 초기화 직후 입력 판정을 수행할지 여부입니다.")]
    [SerializeField]
    private bool _judgementEnabledOnStart = true;

    [TabGroup("Settings")]
    [Tooltip("PERFECT로 인정할 최대 시간 차이(초)입니다.")]
    [MinValue(0.0f)]
    [SerializeField]
    private float _perfectTimeWindow = DEFAULT_PERFECT_TIME_WINDOW;

    [TabGroup("Settings")]
    [Tooltip("GREAT로 인정할 최대 시간 차이(초)입니다.")]
    [MinValue(0.0f)]
    [SerializeField]
    private float _greatTimeWindow = DEFAULT_GREAT_TIME_WINDOW;

    [TabGroup("Settings")]
    [Tooltip("GOOD로 인정할 최대 시간 차이(초)입니다. 이 값을 초과한 미판정 노트는 MISS 처리됩니다.")]
    [MinValue(0.0f)]
    [SerializeField]
    private float _goodTimeWindow = DEFAULT_GOOD_TIME_WINDOW;

    [TabGroup("Settings")]
    [Tooltip("ScoreController 또는 HUD가 소비하기 전까지 보관할 최대 판정 결과 수입니다.")]
    [MinValue(MIN_BUFFERED_RESULT_COUNT)]
    [SerializeField]
    private int _maxBufferedResultCount = DEFAULT_BUFFERED_RESULT_COUNT;

    [TabGroup("Settings")]
    [Tooltip("판정을 비활성화할 때 남아 있는 판정 결과 버퍼를 함께 비울지 여부입니다.")]
    [SerializeField]
    private bool _clearResultsOnDisable = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // GameRoot를 통해 초기화되었는지 여부입니다.
    private bool _isInitialized;

    // 현재 입력을 판정 결과로 처리할 수 있는지 여부입니다.
    private bool _isJudgementEnabled;

    // 가장 최근에 생성된 판정 결과가 있는지 여부입니다.
    private bool _hasLastResult;

    // HUD 판정 텍스트 표시 등에 사용할 가장 최근 판정 결과입니다.
    private ChartJudgementResult _lastResult;

    // BUY / SELL 입력 이벤트를 제공하는 입력 서비스입니다.
    private IChartInputService _chartInputService;

    // 판정 대상 노트 목록과 탐색 기능을 제공하는 스테이지 서비스입니다.
    private IChartStageService _chartStageService;

    // DSP 입력 시간을 스테이지 진행 시간으로 변환하는 ChartRun 위치 서비스입니다.
    private IChartPositionProvider _chartPositionProvider;

    // ScoreController 또는 HUD가 순서대로 소비할 판정 결과 버퍼입니다.
    private readonly Queue<ChartJudgementResult> _resultBuffer = new Queue<ChartJudgementResult>();

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("초기화 여부")]
    public bool IsInitialized => _isInitialized;

    /// <summary>현재 입력을 판정 결과로 처리할 수 있는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("판정 활성화")]
    public bool IsJudgementEnabled => _isJudgementEnabled;

    /// <summary>가장 최근에 생성된 판정 결과가 있는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("최근 판정 있음")]
    public bool HasLastResult => _hasLastResult;

    /// <summary>HUD 판정 텍스트 표시 등에 사용할 가장 최근 판정 결과입니다.</summary>
    public ChartJudgementResult LastResult => _lastResult;

    /// <summary>ScoreController 또는 HUD가 아직 소비하지 않은 판정 결과 수입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("버퍼 결과 수")]
    public int BufferedResultCount => _resultBuffer.Count;

    /// <summary>새 판정 결과가 생성될 때 결과 소비 없이 발행되는 이벤트입니다.</summary>
    public event Action<ChartJudgementResult> JudgementResultCreated;

    /// <summary>현재 ChartRun 기준 스테이지 진행 시간입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("현재 판정 시간")]
    private float CurrentJudgementTime => GetCurrentJudgementTime();

    /// <summary>가장 최근 판정 등급입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("최근 판정 등급")]
    private ChartJudgementGrade LastGrade
    {
        get
        {
            if (_hasLastResult == false)
            {
                return ChartJudgementGrade.JUDGEMENT_GRADE_MISS;
            }

            return _lastResult.Grade;
        }
    }

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>입력 이벤트와 지나간 노트를 판정 결과로 변환합니다.</summary>
    private void Update()
    {
        if (CanProcessJudgement() == false)
        {
            return;
        }

        ProcessBufferedInputs();
        ProcessMissedNotes();
    }

    /// <summary>인스펙터 값이 바뀔 때 잘못된 설정값을 보정합니다.</summary>
    private void OnValidate()
    {
        ClampSettings();
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="chartInputService">BUY / SELL 입력 이벤트를 제공하는 입력 서비스입니다.</param>
    /// <param name="chartStageService">판정 대상 노트 목록과 탐색 기능을 제공하는 스테이지 서비스입니다.</param>
    /// <param name="chartPositionProvider">DSP 입력 시간을 스테이지 진행 시간으로 변환하는 ChartRun 위치 서비스입니다.</param>
    public void Init(
        IChartInputService chartInputService,
        IChartStageService chartStageService,
        IChartPositionProvider chartPositionProvider)
    {
        _chartInputService = chartInputService;
        _chartStageService = chartStageService;
        _chartPositionProvider = chartPositionProvider;

        ClampSettings();
        ResetInternalResultState();

        _isJudgementEnabled = _judgementEnabledOnStart;
        _isInitialized = true;

        LogMissingDependencies();
    }

    //==============================================================================
    // 공개 API (IChartJudgementService)
    //==============================================================================

    /// <summary>
    /// 입력 판정 활성화 여부를 설정합니다.
    /// </summary>
    /// <param name="isEnabled">입력을 판정 결과로 처리할 수 있으면 true입니다.</param>
    public void SetJudgementEnabled(bool isEnabled)
    {
        if (_isJudgementEnabled == isEnabled)
        {
            return;
        }

        _isJudgementEnabled = isEnabled;

        if (_isJudgementEnabled == false && _clearResultsOnDisable)
        {
            ClearBufferedResults();
        }
    }

    /// <summary>현재 스테이지의 판정 상태와 내부 판정 결과 버퍼를 초기화합니다.</summary>
    public void ResetJudgementState()
    {
        ResetInternalResultState();

        if (_chartStageService != null)
        {
            _chartStageService.ResetJudgementState();
        }

        if (_chartInputService != null)
        {
            _chartInputService.ClearBufferedInputs();
        }
    }

    /// <summary>아직 소비하지 않은 판정 결과 버퍼를 모두 비웁니다.</summary>
    public void ClearBufferedResults()
    {
        _resultBuffer.Clear();
    }

    /// <summary>
    /// 가장 오래된 판정 결과를 하나 꺼냅니다.
    /// </summary>
    /// <param name="judgementResult">소비할 판정 결과입니다.</param>
    /// <returns>소비할 판정 결과가 있으면 true입니다.</returns>
    public bool TryConsumeResult(out ChartJudgementResult judgementResult)
    {
        judgementResult = default;

        if (_resultBuffer.Count <= 0)
        {
            return false;
        }

        judgementResult = _resultBuffer.Dequeue();
        return judgementResult.IsValid;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>현재 판정 처리를 수행해도 되는 상태인지 확인합니다.</summary>
    /// <returns>판정 처리를 수행할 수 있으면 true입니다.</returns>
    private bool CanProcessJudgement()
    {
        if (_isInitialized == false)
        {
            return false;
        }

        if (_isJudgementEnabled == false)
        {
            return false;
        }

        if (_chartInputService == null || _chartStageService == null || _chartPositionProvider == null)
        {
            return false;
        }

        return _chartStageService.HasStage;
    }

    /// <summary>입력 버퍼에 쌓인 BUY / SELL 입력을 모두 판정합니다.</summary>
    private void ProcessBufferedInputs()
    {
        while (_chartInputService.TryConsumeInput(out ChartInputEvent inputEvent))
        {
            TryJudgeInput(inputEvent);
        }
    }

    /// <summary>
    /// 입력 이벤트와 가장 가까운 노트를 비교해 판정 결과를 생성합니다.
    /// </summary>
    /// <param name="inputEvent">판정할 BUY / SELL 입력 이벤트입니다.</param>
    /// <returns>판정 결과를 생성했으면 true입니다.</returns>
    private bool TryJudgeInput(ChartInputEvent inputEvent)
    {
        if (inputEvent.IsValid == false)
        {
            return false;
        }

        float inputRunTime = _chartPositionProvider.GetRunTimeAtDspTime(inputEvent.InputDspTime);

        if (_chartStageService.TryFindNearestUnjudgedNote(
            inputRunTime,
            _goodTimeWindow,
            out ChartPlacedNote placedNote) == false)
        {
            return false;
        }

        float timeDifference = Mathf.Abs(placedNote.Time - inputRunTime);
        ChartJudgementResult result = CreateInputJudgementResult(inputEvent, placedNote, timeDifference);

        placedNote.MarkJudged();
        EnqueueResult(result);
        return true;
    }

    /// <summary>GOOD 판정 폭을 지나친 미판정 필수 노트를 MISS 처리합니다.</summary>
    private void ProcessMissedNotes()
    {
        IReadOnlyList<ChartPlacedNote> placedNotes = _chartStageService.PlacedNotes;

        if (placedNotes == null || placedNotes.Count <= 0)
        {
            return;
        }

        float currentRunTime = _chartPositionProvider.GetCurrentRunTime();

        for (int i = 0; i < placedNotes.Count; i++)
        {
            ChartPlacedNote placedNote = placedNotes[i];

            if (CanProcessMissCandidate(placedNote) == false)
            {
                continue;
            }

            float timeDifference = currentRunTime - placedNote.Time;

            if (timeDifference <= _goodTimeWindow)
            {
                continue;
            }

            ChartJudgementResult result = CreateMissResult(default, placedNote, timeDifference);

            placedNote.MarkJudged();
            EnqueueResult(result);
        }
    }

    /// <summary>
    /// 입력 판정 결과를 생성합니다.
    /// </summary>
    /// <param name="inputEvent">판정에 사용된 BUY / SELL 입력 이벤트입니다.</param>
    /// <param name="placedNote">판정 대상 노트입니다.</param>
    /// <param name="timeDifference">노트 기준 시간과 입력 시간의 절댓값 차이입니다.</param>
    /// <returns>생성된 판정 결과입니다.</returns>
    private ChartJudgementResult CreateInputJudgementResult(
        ChartInputEvent inputEvent,
        ChartPlacedNote placedNote,
        float timeDifference)
    {
        if (inputEvent.Side != placedNote.Side)
        {
            return CreateMissResult(inputEvent, placedNote, timeDifference);
        }

        ChartJudgementGrade grade = CalculateGrade(timeDifference);
        return CreateResult(grade, inputEvent, placedNote, timeDifference);
    }

    /// <summary>
    /// MISS 판정 결과를 생성합니다.
    /// </summary>
    /// <param name="inputEvent">MISS를 유발한 입력 이벤트입니다. 미입력 MISS라면 기본값입니다.</param>
    /// <param name="placedNote">MISS 처리할 판정 대상 노트입니다.</param>
    /// <param name="timeDifference">노트 기준 시간과 판정 기준 시간의 차이입니다.</param>
    /// <returns>생성된 MISS 판정 결과입니다.</returns>
    private ChartJudgementResult CreateMissResult(
        ChartInputEvent inputEvent,
        ChartPlacedNote placedNote,
        float timeDifference)
    {
        return CreateResult(
            ChartJudgementGrade.JUDGEMENT_GRADE_MISS,
            inputEvent,
            placedNote,
            timeDifference);
    }

    /// <summary>
    /// 확정된 판정 등급으로 결과 데이터를 생성합니다.
    /// </summary>
    /// <param name="grade">확정된 판정 등급입니다.</param>
    /// <param name="inputEvent">판정에 사용된 BUY / SELL 입력 이벤트입니다.</param>
    /// <param name="placedNote">판정 대상 노트입니다.</param>
    /// <param name="timeDifference">노트 기준 시간과 판정 기준 시간의 차이입니다.</param>
    /// <returns>생성된 판정 결과입니다.</returns>
    private static ChartJudgementResult CreateResult(
        ChartJudgementGrade grade,
        ChartInputEvent inputEvent,
        ChartPlacedNote placedNote,
        float timeDifference)
    {
        int score = GetScore(grade);
        float multiplier = GetMultiplier(grade);
        bool isHit = IsHitGrade(grade);

        return new ChartJudgementResult(
            grade,
            score,
            multiplier,
            Mathf.Abs(timeDifference),
            inputEvent,
            placedNote,
            isHit);
    }

    /// <summary>
    /// 시간 차이에 맞는 판정 등급을 계산합니다.
    /// </summary>
    /// <param name="timeDifference">노트 기준 시간과 입력 시간의 절댓값 차이입니다.</param>
    /// <returns>계산된 판정 등급입니다.</returns>
    private ChartJudgementGrade CalculateGrade(float timeDifference)
    {
        if (timeDifference <= _perfectTimeWindow)
        {
            return ChartJudgementGrade.JUDGEMENT_GRADE_PERFECT;
        }

        if (timeDifference <= _greatTimeWindow)
        {
            return ChartJudgementGrade.JUDGEMENT_GRADE_GREAT;
        }

        if (timeDifference <= _goodTimeWindow)
        {
            return ChartJudgementGrade.JUDGEMENT_GRADE_GOOD;
        }

        return ChartJudgementGrade.JUDGEMENT_GRADE_MISS;
    }

    /// <summary>
    /// 판정 결과를 버퍼에 추가하고, 가장 최근 결과를 갱신합니다.
    /// </summary>
    /// <param name="judgementResult">추가할 판정 결과입니다.</param>
    private void EnqueueResult(ChartJudgementResult judgementResult)
    {
        if (judgementResult.IsValid == false)
        {
            return;
        }

        while (_resultBuffer.Count >= _maxBufferedResultCount)
        {
            _resultBuffer.Dequeue();
            LogWarning("판정 결과 버퍼가 가득 차 가장 오래된 결과를 제거했습니다.");
        }

        _lastResult = judgementResult;
        _hasLastResult = true;
        _resultBuffer.Enqueue(judgementResult);
        JudgementResultCreated?.Invoke(judgementResult);
    }

    /// <summary>내부 판정 결과 상태만 초기화합니다.</summary>
    private void ResetInternalResultState()
    {
        _hasLastResult = false;
        _lastResult = default;
        ClearBufferedResults();
    }

    /// <summary>설정값을 안전한 범위와 올바른 판정 순서로 보정합니다.</summary>
    private void ClampSettings()
    {
        _perfectTimeWindow = GetSafeNonNegativeValue(_perfectTimeWindow, DEFAULT_PERFECT_TIME_WINDOW);
        _greatTimeWindow = GetSafeNonNegativeValue(_greatTimeWindow, DEFAULT_GREAT_TIME_WINDOW);
        _goodTimeWindow = GetSafeNonNegativeValue(_goodTimeWindow, DEFAULT_GOOD_TIME_WINDOW);

        if (_greatTimeWindow < _perfectTimeWindow)
        {
            _greatTimeWindow = _perfectTimeWindow;
        }

        if (_goodTimeWindow < _greatTimeWindow)
        {
            _goodTimeWindow = _greatTimeWindow;
        }

        if (_maxBufferedResultCount < MIN_BUFFERED_RESULT_COUNT)
        {
            _maxBufferedResultCount = DEFAULT_BUFFERED_RESULT_COUNT;
        }
    }

    /// <summary>현재 ChartRun 기준 스테이지 진행 시간을 가져옵니다.</summary>
    /// <returns>현재 스테이지 진행 시간입니다.</returns>
    private float GetCurrentJudgementTime()
    {
        if (_chartPositionProvider == null)
        {
            return 0.0f;
        }

        return _chartPositionProvider.GetCurrentRunTime();
    }

    /// <summary>
    /// 미입력 MISS 후보로 처리할 수 있는 노트인지 확인합니다.
    /// </summary>
    /// <param name="placedNote">검증할 배치 노트입니다.</param>
    /// <returns>MISS 후보로 처리할 수 있으면 true입니다.</returns>
    private static bool CanProcessMissCandidate(ChartPlacedNote placedNote)
    {
        if (placedNote == null)
        {
            return false;
        }

        if (placedNote.IsJudged)
        {
            return false;
        }

        if (placedNote.Data == null)
        {
            return false;
        }

        return placedNote.Data.IsRequired;
    }

    /// <summary>
    /// 판정 등급에 해당하는 점수를 반환합니다.
    /// </summary>
    /// <param name="grade">점수를 확인할 판정 등급입니다.</param>
    /// <returns>판정 등급에 해당하는 점수입니다.</returns>
    private static int GetScore(ChartJudgementGrade grade)
    {
        switch (grade)
        {
            case ChartJudgementGrade.JUDGEMENT_GRADE_PERFECT:
                return SCORE_PERFECT;
            case ChartJudgementGrade.JUDGEMENT_GRADE_GREAT:
                return SCORE_GREAT;
            case ChartJudgementGrade.JUDGEMENT_GRADE_GOOD:
                return SCORE_GOOD;
            case ChartJudgementGrade.JUDGEMENT_GRADE_MISS:
                return SCORE_MISS;
            default:
                return SCORE_MISS;
        }
    }

    /// <summary>
    /// 판정 등급에 해당하는 Accuracy 배율을 반환합니다.
    /// </summary>
    /// <param name="grade">배율을 확인할 판정 등급입니다.</param>
    /// <returns>판정 등급에 해당하는 Accuracy 배율입니다.</returns>
    private static float GetMultiplier(ChartJudgementGrade grade)
    {
        switch (grade)
        {
            case ChartJudgementGrade.JUDGEMENT_GRADE_PERFECT:
                return MULTIPLIER_PERFECT;
            case ChartJudgementGrade.JUDGEMENT_GRADE_GREAT:
                return MULTIPLIER_GREAT;
            case ChartJudgementGrade.JUDGEMENT_GRADE_GOOD:
                return MULTIPLIER_GOOD;
            case ChartJudgementGrade.JUDGEMENT_GRADE_MISS:
                return MULTIPLIER_MISS;
            default:
                return MULTIPLIER_MISS;
        }
    }

    /// <summary>
    /// 콤보를 이어가는 성공 판정인지 확인합니다.
    /// </summary>
    /// <param name="grade">확인할 판정 등급입니다.</param>
    /// <returns>PERFECT, GREAT, GOOD이면 true입니다.</returns>
    private static bool IsHitGrade(ChartJudgementGrade grade)
    {
        return grade != ChartJudgementGrade.JUDGEMENT_GRADE_MISS;
    }

    /// <summary>
    /// 0 이상이어야 하는 설정값을 안전하게 보정합니다.
    /// </summary>
    /// <param name="value">검증할 원본 값입니다.</param>
    /// <param name="fallback">잘못된 값일 때 사용할 대체 값입니다.</param>
    /// <returns>보정된 0 이상의 값입니다.</returns>
    private static float GetSafeNonNegativeValue(float value, float fallback)
    {
        if (float.IsNaN(value))
        {
            return fallback;
        }

        if (float.IsInfinity(value))
        {
            return fallback;
        }

        return Mathf.Max(value, 0.0f);
    }

    /// <summary>초기화 시 누락된 의존성이 있는지 확인해 경고를 출력합니다.</summary>
    private void LogMissingDependencies()
    {
        if (_chartInputService == null)
        {
            LogWarning("ChartInput 서비스가 없어 입력을 판정할 수 없습니다.");
        }

        if (_chartStageService == null)
        {
            LogWarning("ChartStage 서비스가 없어 판정 대상 노트를 찾을 수 없습니다.");
        }

        if (_chartPositionProvider == null)
        {
            LogWarning("ChartRun 위치 서비스가 없어 DSP 시간을 스테이지 시간으로 변환할 수 없습니다.");
        }
    }

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartJudgementController] {message}");
    }
}
