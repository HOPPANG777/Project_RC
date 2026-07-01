using UnityEngine;

//==============================================================================
// 기능: interface::IChartJudgementLineView
// 설명: Gameplay 판정선 View가 Presenter에 공개하는 표시 계약
//==============================================================================

/// <summary>
/// 판정선 표시와 판정 기준 Transform 접근을 제공하는 View 계약입니다.
/// </summary>
public interface IChartJudgementLineView
{
    /// <summary>ChartRunController의 runnerTransform으로 연결할 판정 기준 Transform입니다.</summary>
    Transform JudgementTransform { get; }

    /// <summary>판정선을 표시합니다.</summary>
    void Show();

    /// <summary>판정선을 숨깁니다.</summary>
    void Hide();

    /// <summary>
    /// 판정선 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isVisible">표시하면 true입니다.</param>
    void SetVisible(bool isVisible);

    /// <summary>
    /// 판정선 강조 여부를 설정합니다.
    /// </summary>
    /// <param name="isHighlighted">강조 색상을 사용하면 true입니다.</param>
    void SetHighlight(bool isHighlighted);

    /// <summary>
    /// 판정선 기본 색상을 변경합니다.
    /// </summary>
    /// <param name="lineColor">새 기본 색상입니다.</param>
    void SetLineColor(Color lineColor);

    /// <summary>
    /// 판정선 가로 길이를 변경합니다.
    /// </summary>
    /// <param name="lineLength">새 가로 길이입니다.</param>
    void SetLineLength(float lineLength);

    /// <summary>
    /// 판정선 두께를 변경합니다.
    /// </summary>
    /// <param name="lineWidth">새 두께입니다.</param>
    void SetLineWidth(float lineWidth);

    /// <summary>현재 설정값으로 판정선 표시를 다시 적용합니다.</summary>
    void RefreshVisual();
}
