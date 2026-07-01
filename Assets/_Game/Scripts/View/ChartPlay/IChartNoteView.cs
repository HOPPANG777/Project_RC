using UnityEngine;

//==============================================================================
// 기능: interface::IChartNoteView
// 설명: Gameplay 캔들 노트 View가 Presenter에 공개하는 표시 계약
//==============================================================================

/// <summary>
/// BUY / SELL 캔들 노트 표시와 런타임 배치 노트 바인딩을 제공하는 View 계약입니다.
/// </summary>
public interface IChartNoteView
{
    /// <summary>캔들 노트 Transform입니다. 원점은 종가 판정 기준점입니다.</summary>
    Transform NoteTransform { get; }

    /// <summary>현재 런타임 배치 노트가 연결되어 있는지 여부입니다.</summary>
    bool HasBoundNote { get; }

    /// <summary>현재 연결된 런타임 배치 노트입니다.</summary>
    ChartPlacedNote BoundNote { get; }

    /// <summary>현재 노트가 요구하는 BUY 또는 SELL 입력 방향입니다.</summary>
    ChartNoteSide Side { get; }

    /// <summary>현재 노트가 판정 완료 상태로 표시되는지 여부입니다.</summary>
    bool IsJudged { get; }

    /// <summary>
    /// 런타임 배치 노트를 View에 연결하고 표시를 갱신합니다.
    /// </summary>
    /// <param name="placedNote">표시할 런타임 배치 노트입니다.</param>
    /// <returns>연결에 성공하면 true입니다.</returns>
    bool TryBind(ChartPlacedNote placedNote);

    /// <summary>연결된 런타임 배치 노트의 최신 상태를 표시로 다시 반영합니다.</summary>
    void RefreshFromBoundNote();

    /// <summary>
    /// BUY / SELL 입력 방향에 맞는 색상과 형태를 적용합니다.
    /// </summary>
    /// <param name="side">표시할 입력 방향입니다.</param>
    void SetSide(ChartNoteSide side);

    /// <summary>
    /// 판정 완료 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isJudged">판정 완료 상태이면 true입니다.</param>
    void SetJudged(bool isJudged);

    /// <summary>
    /// 노트 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isVisible">표시하면 true입니다.</param>
    void SetVisible(bool isVisible);

    /// <summary>
    /// 노트 투명도를 설정합니다.
    /// </summary>
    /// <param name="alpha">0-1 범위의 투명도입니다.</param>
    void SetAlpha(float alpha);

    /// <summary>
    /// 노트 월드 위치를 설정합니다.
    /// </summary>
    /// <param name="worldPosition">새 월드 위치입니다.</param>
    void SetWorldPosition(Vector3 worldPosition);

    /// <summary>현재 설정값으로 노트 표시를 다시 적용합니다.</summary>
    void RefreshVisual();
}
