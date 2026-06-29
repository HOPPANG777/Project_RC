//==============================================================================
// 기능: interface::IChartJudgementService
// 설명: GameRoot가 외부에 공개하는 차트 노트 판정 시스템 계약
//==============================================================================

/// <summary>
/// BUY / SELL 입력과 차트 노트를 비교해 생성된 판정 결과를 외부에 제공하는 서비스 계약입니다.
/// </summary>
public interface IChartJudgementService
{
    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    bool IsInitialized { get; }

    /// <summary>현재 입력을 판정 결과로 처리할 수 있는지 여부입니다.</summary>
    bool IsJudgementEnabled { get; }

    /// <summary>가장 최근에 생성된 판정 결과가 있는지 여부입니다.</summary>
    bool HasLastResult { get; }

    /// <summary>HUD 판정 텍스트 표시 등에 사용할 가장 최근 판정 결과입니다.</summary>
    ChartJudgementResult LastResult { get; }

    /// <summary>ScoreController 또는 HUD가 아직 소비하지 않은 판정 결과 수입니다.</summary>
    int BufferedResultCount { get; }

    /// <summary>
    /// 입력 판정 활성화 여부를 설정합니다.
    /// </summary>
    /// <param name="isEnabled">입력을 판정 결과로 처리할 수 있으면 true입니다.</param>
    void SetJudgementEnabled(bool isEnabled);

    /// <summary>현재 스테이지의 판정 상태와 내부 판정 결과 버퍼를 초기화합니다.</summary>
    void ResetJudgementState();

    /// <summary>아직 소비하지 않은 판정 결과 버퍼를 모두 비웁니다.</summary>
    void ClearBufferedResults();

    /// <summary>
    /// 가장 오래된 판정 결과를 하나 꺼냅니다.
    /// </summary>
    /// <param name="judgementResult">소비할 판정 결과입니다.</param>
    /// <returns>소비할 판정 결과가 있으면 true입니다.</returns>
    bool TryConsumeResult(out ChartJudgementResult judgementResult);
}
