using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartStageDataSO
// 설명: StockRhythm 스테이지의 원본 메타 정보와 캔들 노트 패턴을 보관하는 데이터
//==============================================================================
// 세부 구현 사항
// - 첫 MVP 스테이지인 SAMSU 같은 차트 리듬 스테이지를 ScriptableObject로 작성합니다.
// - 기획자가 나중에 패턴표를 옮겨 넣을 수 있도록 노트 배열을 인스펙터에 노출합니다.
// - 런타임 월드 위치 계산과 판정 상태는 ChartStageController / ChartPlacedNote 책임입니다.
//==============================================================================

[CreateAssetMenu(fileName = "ChartStageData", menuName = "StockRhythm/Chart Stage Data")]
public class ChartStageDataSO : ScriptableObject
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 스테이지 길이가 0이 되는 상황을 막기 위한 최소 진행 시간입니다.
    private const float MIN_STAGE_DURATION = 0.01f;

    // BPM이 0이 되는 상황을 막기 위한 최소 음악 기준 BPM입니다.
    private const float MIN_BPM = 1.0f;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    // 스테이지를 내부에서 식별하기 위한 고유 ID입니다.
    [TabGroup("Stage")]
    [Tooltip("스테이지를 내부에서 식별하기 위한 고유 ID입니다. 예: SAMSU")]
    [SerializeField]
    private string _stageId = "SAMSU";

    // 화면에 표시할 스테이지 이름입니다.
    [TabGroup("Stage")]
    [Tooltip("화면에 표시할 스테이지 이름입니다.")]
    [SerializeField]
    private string _displayName = "SAMSU";

    // 스테이지 전체 진행 시간입니다.
    [TabGroup("Stage")]
    [Tooltip("스테이지 전체 진행 시간(초)입니다.")]
    [MinValue(MIN_STAGE_DURATION)]
    [SerializeField]
    private float _stageDuration = 30.0f;

    // 음악/채보 기준 BPM입니다.
    [TabGroup("Music")]
    [Tooltip("음악 또는 채보 기준 BPM입니다. BMS/CSV 임포트 시 시간 계산 기준으로 사용할 수 있습니다.")]
    [MinValue(MIN_BPM)]
    [SerializeField]
    private float _bpm = 120.0f;

    // 이 스테이지에서 재생할 음악 클립입니다.
    [TabGroup("Music")]
    [Tooltip("이 스테이지에서 재생할 음악 클립입니다. 없으면 DSP 시간만으로 테스트할 수 있습니다.")]
    [SerializeField]
    private AudioClip _musicClip;

    // 기획자가 입력할 캔들 노트 패턴 배열입니다.
    [TabGroup("Notes")]
    [Tooltip("판정 또는 장식에 사용할 캔들 노트 배열입니다. 모든 0.1초 노드에 노트가 있을 필요는 없습니다.")]
    [SerializeField]
    private ChartNoteData[] _notes = new ChartNoteData[0];

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>스테이지를 내부에서 식별하기 위한 고유 ID입니다.</summary>
    public string StageId => _stageId;

    /// <summary>화면에 표시할 스테이지 이름입니다.</summary>
    public string DisplayName => _displayName;

    /// <summary>스테이지 전체 진행 시간(초)입니다.</summary>
    public float StageDuration => _stageDuration;

    /// <summary>음악 또는 채보 기준 BPM입니다.</summary>
    public float Bpm => _bpm;

    /// <summary>이 스테이지에서 재생할 음악 클립입니다.</summary>
    public AudioClip MusicClip => _musicClip;

    /// <summary>판정 또는 장식에 사용할 캔들 노트 배열입니다.</summary>
    public ChartNoteData[] Notes => _notes;

    /// <summary>등록된 캔들 노트 수입니다.</summary>
    public int NoteCount
    {
        get
        {
            if (_notes == null)
            {
                return 0;
            }

            return _notes.Length;
        }
    }

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>인스펙터 값이 바뀔 때 잘못된 설정값을 보정합니다.</summary>
    private void OnValidate()
    {
        ClampSettings();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 인덱스로 캔들 노트 데이터를 조회합니다.
    /// </summary>
    /// <param name="index">조회할 노트 인덱스입니다.</param>
    /// <param name="noteData">조회된 노트 데이터입니다.</param>
    /// <returns>노트 데이터를 찾으면 true입니다.</returns>
    public bool TryGetNote(int index, out ChartNoteData noteData)
    {
        noteData = null;

        if (_notes == null)
        {
            return false;
        }

        if (index < 0 || index >= _notes.Length)
        {
            return false;
        }

        noteData = _notes[index];
        return noteData != null;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>설정값을 안전한 범위로 보정합니다.</summary>
    private void ClampSettings()
    {
        _stageDuration = GetSafePositiveValue(_stageDuration, 30.0f, MIN_STAGE_DURATION);
        _bpm = GetSafePositiveValue(_bpm, 120.0f, MIN_BPM);

        if (_notes == null)
        {
            _notes = new ChartNoteData[0];
        }
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
}
