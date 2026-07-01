using UnityEngine;

//==============================================================================
// 기능: interface::IChartPlaySurfaceView
// 설명: Gameplay 플레이 표면 View가 Presenter에 공개하는 표시 계약
//==============================================================================

/// <summary>
/// 판정선과 캔들 노트들을 담는 Gameplay 플레이 표면 View 계약입니다.
/// </summary>
public interface IChartPlaySurfaceView
{
    /// <summary>플레이 표면 전체 Transform입니다.</summary>
    Transform SurfaceTransform { get; }

    /// <summary>캔들 노트 View들이 생성될 루트 Transform입니다.</summary>
    Transform NoteRoot { get; }

    /// <summary>입력 타이밍 기준이 되는 판정선 View입니다.</summary>
    IChartJudgementLineView JudgementLineView { get; }

    /// <summary>현재 생성되어 관리 중인 노트 View 수입니다.</summary>
    int NoteViewCount { get; }

    /// <summary>플레이 표면을 표시합니다.</summary>
    void Show();

    /// <summary>플레이 표면을 숨깁니다.</summary>
    void Hide();

    /// <summary>
    /// 플레이 표면 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isVisible">표시하면 true입니다.</param>
    void SetVisible(bool isVisible);

    /// <summary>
    /// 런타임 배치 노트를 표시할 캔들 노트 View를 생성합니다.
    /// </summary>
    /// <param name="placedNote">표시할 런타임 배치 노트입니다.</param>
    /// <param name="noteView">생성된 캔들 노트 View입니다.</param>
    /// <returns>생성에 성공하면 true입니다.</returns>
    bool TryCreateNoteView(ChartPlacedNote placedNote, out IChartNoteView noteView);

    /// <summary>
    /// 인덱스로 생성된 캔들 노트 View를 조회합니다.
    /// </summary>
    /// <param name="index">조회할 노트 View 인덱스입니다.</param>
    /// <param name="noteView">조회된 노트 View입니다.</param>
    /// <returns>노트 View를 찾으면 true입니다.</returns>
    bool TryGetNoteView(int index, out IChartNoteView noteView);

    /// <summary>생성되어 관리 중인 모든 캔들 노트 View를 갱신합니다.</summary>
    void RefreshNoteViews();

    /// <summary>생성되어 관리 중인 모든 캔들 노트 View를 제거합니다.</summary>
    void ClearNoteViews();

    /// <summary>현재 설정값으로 플레이 표면 표시를 다시 적용합니다.</summary>
    void RefreshVisual();
}
