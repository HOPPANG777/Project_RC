//==============================================================================
// 기능: enum::ChartJudgementGrade
// 설명: 입력 타이밍과 BUY/SELL 방향 확인 결과를 점수와 피드백으로 변환하기 위한 판정 등급
//==============================================================================

/// <summary>
/// 차트 노트 1개에 대한 입력 판정 등급입니다.
/// </summary>
public enum ChartJudgementGrade
{
    /// <summary>
    /// 노트 기준 시간과 입력 시간이 40ms 이내로 가장 정확하게 맞은 최상위 성공 판정입니다.
    /// </summary>
    JUDGEMENT_GRADE_PERFECT = 0,

    /// <summary>
    /// PERFECT 범위는 벗어났지만 60ms 이내로 충분히 정확하게 맞은 상위 성공 판정입니다.
    /// </summary>
    JUDGEMENT_GRADE_GREAT = 1,

    /// <summary>
    /// GREAT 범위는 벗어났지만 100ms 이내로 허용 가능한 기본 성공 판정입니다.
    /// </summary>
    JUDGEMENT_GRADE_GOOD = 2,

    /// <summary>
    /// 입력 시간이 100ms를 초과했거나, 입력이 누락되었거나, BUY/SELL 방향이 일치하지 않은 실패 판정입니다.
    /// </summary>
    JUDGEMENT_GRADE_MISS = 3
}
