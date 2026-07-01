//==============================================================================
// 기능: interface::IChartPlayService
// 설명: GameRoot가 외부에 공개하는 한 판 시작 흐름 계약
//==============================================================================

/// <summary>
/// 스테이지 준비 확인부터 차트 러닝 시작까지의 한 판 시작 흐름을 제공하는 서비스 계약입니다.
/// </summary>
public interface IChartPlayService
{
    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    bool IsInitialized { get; }

    /// <summary>현재 한 판을 시작할 수 있는 준비 상태인지 여부입니다.</summary>
    bool CanStartPlay { get; }

    /// <summary>스테이지와 판정 상태를 준비한 뒤 차트 러닝을 시작합니다.</summary>
    /// <returns>한 판 시작에 성공하면 true입니다.</returns>
    bool TryStartPlay();
}
