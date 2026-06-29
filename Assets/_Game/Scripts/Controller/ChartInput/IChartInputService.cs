//==============================================================================
// 기능: interface::IChartInputService
// 설명: GameRoot가 외부에 공개하는 BUY / SELL 입력 시스템 계약
//==============================================================================

public interface IChartInputService
{
    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    bool IsInitialized { get; }

    /// <summary>현재 BUY / SELL 입력을 받을 수 있는지 여부입니다.</summary>
    bool IsInputEnabled { get; }

    /// <summary>아직 판정 쪽에서 소비하지 않은 입력 수입니다.</summary>
    int BufferedInputCount { get; }

    /// <summary>
    /// BUY / SELL 입력 활성화 여부를 설정합니다.
    /// </summary>
    /// <param name="isEnabled">입력을 받을 수 있으면 true입니다.</param>
    void SetInputEnabled(bool isEnabled);

    /// <summary>아직 소비하지 않은 입력 버퍼를 모두 비웁니다.</summary>
    void ClearBufferedInputs();

    /// <summary>
    /// 가장 오래된 입력을 하나 꺼냅니다.
    /// </summary>
    /// <param name="inputEvent">소비할 BUY / SELL 입력 이벤트입니다.</param>
    /// <returns>소비할 입력이 있으면 true입니다.</returns>
    bool TryConsumeInput(out ChartInputEvent inputEvent);
}
