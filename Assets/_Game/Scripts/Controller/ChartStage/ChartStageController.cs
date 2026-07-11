using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartStageController
// 설명: ChartStageDataSO를 읽어 차트 노트의 월드 위치와 판정용 런타임 캐시를 생성하는 컨트롤러
//==============================================================================
// 세부 구현 사항
// - ChartRunController가 제공하는 시간->X 위치 변환을 사용합니다.
// - ChartNoteData.Value는 Y 위치 계산 기준으로 사용합니다.
// - 입력, 판정, 점수, 노트 프리팹 표시는 각각 다른 컨트롤러/뷰의 책임으로 남깁니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartStageController : MonoBehaviour, IChartStageService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // Value를 월드 Y로 변환할 때 너무 작은 배율이 되는 것을 막기 위한 최소값입니다.
    private const float MIN_VALUE_TO_WORLD_Y_SCALE = 0.01f;

    // 가장 가까운 노트 탐색에서 음수 시간창을 막기 위한 최소값입니다.
    private const float MIN_FIND_TIME_WINDOW = 0.0f;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Stage")]
    [Tooltip("초기화 시 자동으로 배치 캐시를 만들 기본 스테이지 데이터입니다.")]
    [SerializeField]
    private ChartStageDataSO _defaultStageData;

    [TabGroup("Placement")]
    [Tooltip("note.Value를 월드 Y 위치로 변환할 때 곱할 배율입니다.")]
    [MinValue(MIN_VALUE_TO_WORLD_Y_SCALE)]
    [SerializeField]
    private float _valueToWorldYScale = 1.0f;

    [TabGroup("Placement")]
    [Tooltip("note.Value로 계산한 월드 Y 위치에 더할 오프셋입니다.")]
    [SerializeField]
    private float _worldYOffset = 0.0f;

    [TabGroup("Placement")]
    [Tooltip("배치 노트에 사용할 고정 월드 Z 위치입니다.")]
    [SerializeField]
    private float _noteWorldZ = 0.0f;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // GameRoot를 통해 초기화되었는지 여부입니다.
    private bool _isInitialized;

    // 현재 배치 캐시가 만들어진 스테이지 데이터입니다.
    private ChartStageDataSO _currentStageData;

    // 시간->X 위치 계산을 제공하는 차트 러닝 위치 서비스입니다.
    private IChartPositionProvider _chartPositionProvider;

    // JudgementController와 View가 사용할 런타임 배치 노트 캐시입니다.
    private readonly List<ChartPlacedNote> _placedNotes = new List<ChartPlacedNote>();

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("초기화 여부")]
    public bool IsInitialized => _isInitialized;

    /// <summary>현재 스테이지 데이터가 배치 캐시로 준비되었는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("스테이지 준비")]
    public bool HasStage => _currentStageData != null;

    /// <summary>현재 배치된 원본 스테이지 데이터입니다.</summary>
    public ChartStageDataSO CurrentStageData => _currentStageData;

    /// <summary>현재 스테이지 ID입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("스테이지 ID")]
    public string StageId
    {
        get
        {
            if (_currentStageData == null)
            {
                return string.Empty;
            }

            return _currentStageData.StageId;
        }
    }

    /// <summary>현재 화면 표시용 스테이지 이름입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("표시 이름")]
    public string DisplayName
    {
        get
        {
            if (_currentStageData == null)
            {
                return string.Empty;
            }

            return _currentStageData.DisplayName;
        }
    }

    /// <summary>런타임에 계산된 배치 노트 목록입니다.</summary>
    public IReadOnlyList<ChartPlacedNote> PlacedNotes => _placedNotes;

    /// <summary>런타임에 계산된 배치 노트 수입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("배치 노트 수")]
    public int PlacedNoteCount => _placedNotes.Count;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

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
    /// <param name="chartPositionProvider">시간->X 위치 계산을 제공하는 차트 러닝 위치 서비스입니다.</param>
    public void Init(IChartPositionProvider chartPositionProvider)
    {
        _chartPositionProvider = chartPositionProvider;
        ClampSettings();

        _isInitialized = true;

        if (_defaultStageData != null)
        {
            TryBuildStage(_defaultStageData);
        }
    }

    //==============================================================================
    // 공개 API (IChartStageService)
    //==============================================================================

    /// <summary>
    /// 스테이지 데이터를 읽어 월드 위치가 포함된 런타임 노트 캐시를 생성합니다.
    /// </summary>
    /// <param name="stageData">배치할 스테이지 원본 데이터입니다.</param>
    /// <returns>캐시 생성에 성공하면 true입니다.</returns>
    public bool TryBuildStage(ChartStageDataSO stageData)
    {
        if (_isInitialized == false)
        {
            LogWarning("초기화 전에는 스테이지 캐시를 생성할 수 없습니다.");
            return false;
        }

        if (_chartPositionProvider == null)
        {
            LogWarning("ChartRun 위치 서비스가 없어 스테이지 캐시를 생성할 수 없습니다.");
            return false;
        }

        if (stageData == null)
        {
            LogWarning("스테이지 데이터가 null입니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(stageData.StageId))
        {
            LogWarning("스테이지 ID가 비어 있습니다.");
            return false;
        }

        _currentStageData = stageData;
        RebuildPlacedNotes();
        return true;
    }

    /// <summary>현재 스테이지 데이터를 기준으로 런타임 노트 캐시를 다시 생성합니다.</summary>
    /// <returns>캐시 재생성에 성공하면 true입니다.</returns>
    public bool TryRebuildStage()
    {
        if (_currentStageData == null)
        {
            LogWarning("다시 생성할 현재 스테이지 데이터가 없습니다.");
            return false;
        }

        return TryBuildStage(_currentStageData);
    }

    /// <summary>현재 스테이지와 런타임 노트 캐시를 비웁니다.</summary>
    public void ClearStage()
    {
        _currentStageData = null;
        _placedNotes.Clear();
    }

    /// <summary>모든 배치 노트의 판정 상태를 초기화합니다.</summary>
    public void ResetJudgementState()
    {
        for (int i = 0; i < _placedNotes.Count; i++)
        {
            ChartPlacedNote placedNote = _placedNotes[i];

            if (placedNote == null)
            {
                continue;
            }

            placedNote.ResetJudgement();
        }
    }

    /// <summary>
    /// 인덱스로 배치 노트를 조회합니다.
    /// </summary>
    /// <param name="index">조회할 배치 노트 인덱스입니다.</param>
    /// <param name="placedNote">조회된 배치 노트입니다.</param>
    /// <returns>배치 노트를 찾으면 true입니다.</returns>
    public bool TryGetPlacedNote(int index, out ChartPlacedNote placedNote)
    {
        placedNote = null;

        if (index < 0 || index >= _placedNotes.Count)
        {
            return false;
        }

        placedNote = _placedNotes[index];
        return placedNote != null;
    }

    /// <summary>
    /// 지정 시간에 가장 가까운 미판정 필수 노트를 찾습니다.
    /// </summary>
    /// <param name="time">탐색 기준 스테이지 진행 시간입니다.</param>
    /// <param name="maxTimeWindow">허용할 최대 시간 차이입니다.</param>
    /// <param name="placedNote">가장 가까운 미판정 필수 노트입니다.</param>
    /// <returns>노트를 찾으면 true입니다.</returns>
    public bool TryFindNearestUnjudgedNote(float time, float maxTimeWindow, out ChartPlacedNote placedNote)
    {
        placedNote = null;

        if (maxTimeWindow < MIN_FIND_TIME_WINDOW)
        {
            return false;
        }

        float closestDifference = float.MaxValue;

        for (int i = 0; i < _placedNotes.Count; i++)
        {
            ChartPlacedNote candidate = _placedNotes[i];

            if (CanUseCandidateForJudgement(candidate) == false)
            {
                continue;
            }

            float difference = Mathf.Abs(candidate.Time - time);

            if (difference > maxTimeWindow)
            {
                continue;
            }

            if (difference >= closestDifference)
            {
                continue;
            }

            closestDifference = difference;
            placedNote = candidate;
        }

        return placedNote != null;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>설정값을 안전한 범위로 보정합니다.</summary>
    private void ClampSettings()
    {
        _valueToWorldYScale = GetSafePositiveValue(
            _valueToWorldYScale,
            1.0f,
            MIN_VALUE_TO_WORLD_Y_SCALE);
    }

    /// <summary>현재 스테이지 데이터의 노트 배열을 월드 배치 캐시로 변환합니다.</summary>
    private void RebuildPlacedNotes()
    {
        _placedNotes.Clear();

        if (_currentStageData == null)
        {
            return;
        }

        ChartNoteData[] notes = _currentStageData.Notes;

        if (notes == null || notes.Length <= 0)
        {
            LogWarning("스테이지에 배치할 노트가 없습니다.");
            return;
        }

        for (int i = 0; i < notes.Length; i++)
        {
            ChartNoteData noteData = notes[i];

            if (noteData == null)
            {
                LogWarning($"노트 {i}번 데이터가 null이라 건너뜁니다.");
                continue;
            }

            if (IsValidNoteTime(noteData) == false)
            {
                LogWarning($"노트 {i}번 시간이 스테이지 길이를 벗어나 건너뜁니다.");
                continue;
            }

            Vector3 worldPosition = CalculateWorldPosition(noteData);
            ChartPlacedNote placedNote = new ChartPlacedNote(noteData, worldPosition, i);

            ApplyCustomCandleShapeIfNeeded(noteData, placedNote);
            _placedNotes.Add(placedNote);
        }
    }

    /// <summary>
    /// 원본 노트의 시간과 차트 높이 값으로 월드 위치를 계산합니다.
    /// </summary>
    /// <param name="noteData">월드 위치를 계산할 노트 데이터입니다.</param>
    /// <returns>계산된 월드 위치입니다.</returns>
    private Vector3 CalculateWorldPosition(ChartNoteData noteData)
    {
        float x = _chartPositionProvider.GetXAtTime(noteData.Time);
        float y = ConvertValueToWorldY(noteData.CloseValue);

        return new Vector3(x, y, _noteWorldZ);
    }

    /// <summary>
    /// 노트에 커스텀 OHLC 캔들 값이 있으면 런타임 배치 노트에 월드 Y 형태 정보를 설정합니다.
    /// </summary>
    /// <param name="noteData">캔들 형태 값을 가진 원본 노트 데이터입니다.</param>
    /// <param name="placedNote">월드 형태 정보를 받을 런타임 배치 노트입니다.</param>
    private void ApplyCustomCandleShapeIfNeeded(ChartNoteData noteData, ChartPlacedNote placedNote)
    {
        if (noteData == null)
        {
            return;
        }

        if (placedNote == null)
        {
            return;
        }

        if (noteData.UseCustomCandleShape == false)
        {
            return;
        }

        float closeValue = GetSafeFiniteValue(noteData.CloseValue, noteData.Value);
        float openValue = GetSafeFiniteValue(noteData.OpenValue, closeValue);
        float bodyTopValue = Mathf.Max(openValue, closeValue);
        float bodyBottomValue = Mathf.Min(openValue, closeValue);
        float highValue = Mathf.Max(GetSafeFiniteValue(noteData.HighValue, bodyTopValue), bodyTopValue);
        float lowValue = Mathf.Min(GetSafeFiniteValue(noteData.LowValue, bodyBottomValue), bodyBottomValue);

        placedNote.SetCustomCandleWorldShape(
            ConvertValueToWorldY(openValue),
            ConvertValueToWorldY(highValue),
            ConvertValueToWorldY(lowValue),
            ConvertValueToWorldY(closeValue));
    }

    /// <summary>
    /// note.Value를 월드 Y 위치로 변환합니다.
    /// </summary>
    /// <param name="value">원본 차트 높이 값입니다.</param>
    /// <returns>월드 Y 위치입니다.</returns>
    private float ConvertValueToWorldY(float value)
    {
        return _worldYOffset + value * _valueToWorldYScale;
    }

    /// <summary>
    /// 노트 시간이 현재 스테이지 길이 안에 있는지 확인합니다.
    /// </summary>
    /// <param name="noteData">검증할 노트 데이터입니다.</param>
    /// <returns>유효한 시간이면 true입니다.</returns>
    private bool IsValidNoteTime(ChartNoteData noteData)
    {
        if (noteData.Time < 0.0f)
        {
            return false;
        }

        if (_currentStageData == null)
        {
            return true;
        }

        return noteData.Time <= _currentStageData.StageDuration;
    }

    /// <summary>
    /// JudgementController가 판정 후보로 사용할 수 있는 노트인지 확인합니다.
    /// </summary>
    /// <param name="placedNote">검증할 배치 노트입니다.</param>
    /// <returns>판정 후보로 사용할 수 있으면 true입니다.</returns>
    private static bool CanUseCandidateForJudgement(ChartPlacedNote placedNote)
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
    /// 양수여야 하는 설정값을 안전하게 보정합니다.
    /// </summary>
    /// <param name="value">검증할 원본 값입니다.</param>
    /// <param name="fallback">잘못된 값일 때 사용할 대체 값입니다.</param>
    /// <param name="minimum">허용할 최소 값입니다.</param>
    /// <returns>보정된 양수 값입니다.</returns>
    private static float GetSafePositiveValue(float value, float fallback, float minimum)
    {
        if (float.IsNaN(value))
        {
            return fallback;
        }

        if (float.IsInfinity(value))
        {
            return fallback;
        }

        return Mathf.Max(value, minimum);
    }

    /// <summary>
    /// 유한한 실수 값만 통과시키고 잘못된 값이면 대체 값을 반환합니다.
    /// </summary>
    /// <param name="value">검증할 원본 값입니다.</param>
    /// <param name="fallback">잘못된 값일 때 사용할 대체 값입니다.</param>
    /// <returns>유한한 원본 값 또는 대체 값입니다.</returns>
    private static float GetSafeFiniteValue(float value, float fallback)
    {
        if (float.IsNaN(value))
        {
            return fallback;
        }

        if (float.IsInfinity(value))
        {
            return fallback;
        }

        return value;
    }

    /// <summary>
    /// 에디터와 개발 빌드에서만 출력되는 경고 로그입니다.
    /// </summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartStageController] {message}");
    }
}
