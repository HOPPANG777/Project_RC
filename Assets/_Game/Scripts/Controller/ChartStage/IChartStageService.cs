using System.Collections.Generic;

//==============================================================================
// 기능: interface::IChartStageService
// 설명: GameRoot가 외부에 공개하는 차트 스테이지 배치 캐시 계약
//==============================================================================

public interface IChartStageService
{
    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    bool IsInitialized { get; }

    /// <summary>현재 스테이지 데이터가 배치 캐시로 준비되었는지 여부입니다.</summary>
    bool HasStage { get; }

    /// <summary>현재 배치된 원본 스테이지 데이터입니다.</summary>
    ChartStageDataSO CurrentStageData { get; }

    /// <summary>현재 스테이지 ID입니다.</summary>
    string StageId { get; }

    /// <summary>현재 화면 표시용 스테이지 이름입니다.</summary>
    string DisplayName { get; }

    /// <summary>런타임에 계산된 배치 노트 목록입니다.</summary>
    IReadOnlyList<ChartPlacedNote> PlacedNotes { get; }

    /// <summary>런타임에 계산된 배치 노트 수입니다.</summary>
    int PlacedNoteCount { get; }

    /// <summary>
    /// 스테이지 데이터를 읽어 월드 위치가 포함된 런타임 노트 캐시를 생성합니다.
    /// </summary>
    /// <param name="stageData">배치할 스테이지 원본 데이터입니다.</param>
    /// <returns>캐시 생성에 성공하면 true입니다.</returns>
    bool TryBuildStage(ChartStageDataSO stageData);

    /// <summary>현재 스테이지 데이터를 기준으로 런타임 노트 캐시를 다시 생성합니다.</summary>
    /// <returns>캐시 재생성에 성공하면 true입니다.</returns>
    bool TryRebuildStage();

    /// <summary>현재 스테이지와 런타임 노트 캐시를 비웁니다.</summary>
    void ClearStage();

    /// <summary>모든 배치 노트의 판정 상태를 초기화합니다.</summary>
    void ResetJudgementState();

    /// <summary>
    /// 인덱스로 배치 노트를 조회합니다.
    /// </summary>
    /// <param name="index">조회할 배치 노트 인덱스입니다.</param>
    /// <param name="placedNote">조회된 배치 노트입니다.</param>
    /// <returns>배치 노트를 찾으면 true입니다.</returns>
    bool TryGetPlacedNote(int index, out ChartPlacedNote placedNote);

    /// <summary>
    /// 지정 시간에 가장 가까운 미판정 필수 노트를 찾습니다.
    /// </summary>
    /// <param name="time">탐색 기준 스테이지 진행 시간입니다.</param>
    /// <param name="maxTimeWindow">허용할 최대 시간 차이입니다.</param>
    /// <param name="placedNote">가장 가까운 미판정 필수 노트입니다.</param>
    /// <returns>노트를 찾으면 true입니다.</returns>
    bool TryFindNearestUnjudgedNote(float time, float maxTimeWindow, out ChartPlacedNote placedNote);
}
