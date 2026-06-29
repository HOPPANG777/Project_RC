//==============================================================================
// 기능: struct::ChartJudgementResult
// 설명: 노트 1개에 대한 판정 등급, 점수, 시간 차이, 입력/노트 참조를 담는 런타임 결과 데이터
//==============================================================================
// 세부 구현 사항
// - JudgementController가 ChartInputEvent와 ChartPlacedNote를 비교한 뒤 생성합니다.
// - ScoreController는 Grade, Score, Multiplier, IsHit 값을 사용해 PROFIT, COMBO, Accuracy를 계산합니다.
// - MISS는 입력 실패, 미입력으로 지나간 노트, BUY/SELL 방향 불일치를 모두 표현할 수 있습니다.
//==============================================================================

/// <summary>
/// 차트 노트 1개에 대한 판정 결과 데이터입니다.
/// </summary>
public readonly struct ChartJudgementResult
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // PERFECT / GREAT / GOOD / MISS 중 하나로 확정된 판정 등급입니다.
    private readonly ChartJudgementGrade _grade;

    // 이 판정으로 PROFIT에 더해질 점수입니다.
    private readonly int _score;

    // Accuracy 계산에 사용할 판정 배율입니다.
    private readonly float _multiplier;

    // 노트 기준 시간과 입력 시간의 절댓값 차이입니다.
    private readonly float _timeDifference;

    // 판정에 사용된 BUY / SELL 입력 이벤트입니다. 미입력 MISS라면 기본값일 수 있습니다.
    private readonly ChartInputEvent _inputEvent;

    // 판정 대상이 된 런타임 배치 노트입니다.
    private readonly ChartPlacedNote _placedNote;

    // 콤보가 이어지는 성공 판정인지 여부입니다.
    private readonly bool _isHit;

    // 기본값 결과가 실제 판정처럼 소비되는 것을 막기 위한 유효성 플래그입니다.
    private readonly bool _isValid;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>PERFECT / GREAT / GOOD / MISS 중 하나로 확정된 판정 등급입니다.</summary>
    public ChartJudgementGrade Grade => _grade;

    /// <summary>이 판정으로 PROFIT에 더해질 점수입니다.</summary>
    public int Score => _score;

    /// <summary>Accuracy 계산에 사용할 판정 배율입니다.</summary>
    public float Multiplier => _multiplier;

    /// <summary>노트 기준 시간과 입력 시간의 절댓값 차이입니다.</summary>
    public float TimeDifference => _timeDifference;

    /// <summary>판정에 사용된 BUY / SELL 입력 이벤트입니다. 미입력 MISS라면 기본값일 수 있습니다.</summary>
    public ChartInputEvent InputEvent => _inputEvent;

    /// <summary>판정 대상이 된 런타임 배치 노트입니다.</summary>
    public ChartPlacedNote PlacedNote => _placedNote;

    /// <summary>콤보가 이어지는 성공 판정인지 여부입니다.</summary>
    public bool IsHit => _isHit;

    /// <summary>실제 판정에서 생성된 결과인지 여부입니다.</summary>
    public bool IsValid => _isValid;

    /// <summary>판정에 실제 입력 이벤트가 포함되어 있는지 여부입니다.</summary>
    public bool HasInput => _inputEvent.IsValid;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// 차트 노트 1개에 대한 판정 결과를 생성합니다.
    /// </summary>
    /// <param name="grade">확정된 판정 등급입니다.</param>
    /// <param name="score">PROFIT에 더해질 점수입니다.</param>
    /// <param name="multiplier">Accuracy 계산에 사용할 판정 배율입니다.</param>
    /// <param name="timeDifference">노트 기준 시간과 입력 시간의 절댓값 차이입니다.</param>
    /// <param name="inputEvent">판정에 사용된 BUY / SELL 입력 이벤트입니다.</param>
    /// <param name="placedNote">판정 대상이 된 런타임 배치 노트입니다.</param>
    /// <param name="isHit">콤보가 이어지는 성공 판정이면 true입니다.</param>
    public ChartJudgementResult(
        ChartJudgementGrade grade,
        int score,
        float multiplier,
        float timeDifference,
        ChartInputEvent inputEvent,
        ChartPlacedNote placedNote,
        bool isHit)
    {
        _grade = grade;
        _score = score;
        _multiplier = multiplier;
        _timeDifference = timeDifference;
        _inputEvent = inputEvent;
        _placedNote = placedNote;
        _isHit = isHit;
        _isValid = true;
    }
}
