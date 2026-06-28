using UnityEngine;

//==============================================================================
// 기능: class::ChartPlacedNote
// 설명: 원본 캔들 노트 데이터와 월드 배치 정보, 판정 여부를 담는 런타임 데이터
//==============================================================================
// 세부 구현 사항
// - ChartStageDataSO의 ChartNoteData를 런타임 판정/표시용으로 감싼 객체입니다.
// - 화면 프리팹이 없어도 JudgementController가 먼저 판정 로직을 만들 수 있게 합니다.
// - IsJudged는 입력 판정 또는 MISS 처리 이후 중복 판정을 막기 위한 상태입니다.
//==============================================================================

public class ChartPlacedNote
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 원본 캔들 노트 데이터입니다.
    private readonly ChartNoteData _data;

    // 스테이지 노트 배열 안에서의 원본 인덱스입니다.
    private readonly int _index;

    // ChartRun 시간축과 note.Value를 기준으로 계산한 월드 위치입니다.
    private Vector3 _worldPosition;

    // 이 노트가 이미 판정되었는지 여부입니다.
    private bool _isJudged;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>원본 캔들 노트 데이터입니다.</summary>
    public ChartNoteData Data => _data;

    /// <summary>ChartRun 시간축과 note.Value를 기준으로 계산한 월드 위치입니다.</summary>
    public Vector3 WorldPosition => _worldPosition;

    /// <summary>이 노트가 이미 판정되었는지 여부입니다.</summary>
    public bool IsJudged => _isJudged;

    /// <summary>스테이지 노트 배열 안에서의 원본 인덱스입니다.</summary>
    public int Index => _index;

    /// <summary>노트가 판정선에 닿아야 하는 스테이지 진행 시간(초)입니다.</summary>
    public float Time
    {
        get
        {
            if (_data == null)
            {
                return 0.0f;
            }

            return _data.Time;
        }
    }

    /// <summary>이 노트가 요구하는 BUY 또는 SELL 입력 방향입니다.</summary>
    public ChartNoteSide Side
    {
        get
        {
            if (_data == null)
            {
                return ChartNoteSide.NOTE_SIDE_BUY;
            }

            return _data.Side;
        }
    }

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 런타임 배치 노트 데이터를 생성합니다.
    /// </summary>
    /// <param name="data">원본 캔들 노트 데이터입니다.</param>
    /// <param name="worldPosition">계산된 월드 위치입니다.</param>
    /// <param name="index">스테이지 노트 배열 안에서의 원본 인덱스입니다.</param>
    public ChartPlacedNote(ChartNoteData data, Vector3 worldPosition, int index)
    {
        _data = data;
        _worldPosition = worldPosition;
        _index = index;
        _isJudged = false;
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 계산된 월드 위치를 갱신합니다.
    /// </summary>
    /// <param name="worldPosition">새 월드 위치입니다.</param>
    public void SetWorldPosition(Vector3 worldPosition)
    {
        _worldPosition = worldPosition;
    }

    /// <summary>이 노트를 판정 완료 상태로 표시합니다.</summary>
    public void MarkJudged()
    {
        _isJudged = true;
    }

    /// <summary>이 노트의 판정 상태를 초기화합니다.</summary>
    public void ResetJudgement()
    {
        _isJudged = false;
    }
}
