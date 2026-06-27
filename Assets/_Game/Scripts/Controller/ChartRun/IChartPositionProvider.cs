//==============================================================================
// 기능: interface::IChartPositionProvider
// 설명: 가로 차트 러닝의 시간, X 위치, 노드 인덱스를 제공하는 계약
//==============================================================================

public interface IChartPositionProvider
{
    /// <summary>스테이지 전체 진행 시간입니다.</summary>
    float StageDuration { get; }

    /// <summary>캔들/노드 하나가 차지하는 시간 간격입니다.</summary>
    float NodeInterval { get; }

    /// <summary>현재 스테이지 진행 시간입니다.</summary>
    float CurrentTime { get; }

    /// <summary>현재 플레이어의 차트 X 위치입니다.</summary>
    float CurrentX { get; }

    /// <summary>현재 시간 기준의 캔들/노드 인덱스입니다.</summary>
    int CurrentNodeIndex { get; }

    /// <summary>스테이지 전체 진행률입니다.</summary>
    float NormalizedProgress { get; }

    /// <summary>특정 시간에 해당하는 차트 X 위치를 계산합니다.</summary>
    /// <param name="time">계산할 시간입니다.</param>
    /// <returns>해당 시간의 차트 X 위치입니다.</returns>
    float GetXAtTime(float time);

    /// <summary>특정 차트 X 위치에 해당하는 시간을 계산합니다.</summary>
    /// <param name="x">계산할 차트 X 위치입니다.</param>
    /// <returns>해당 X 위치의 시간입니다.</returns>
    float GetTimeAtX(float x);

    /// <summary>특정 시간에 해당하는 캔들/노드 인덱스를 계산합니다.</summary>
    /// <param name="time">계산할 시간입니다.</param>
    /// <returns>해당 시간의 캔들/노드 인덱스입니다.</returns>
    int GetNodeIndexAtTime(float time);
}
