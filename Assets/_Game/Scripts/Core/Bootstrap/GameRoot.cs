using Sirenix.OdinInspector;
using UnityEngine;


//==============================================================================
// 기능: class::GameRoot
// 설명: 프로젝트 전역에서 하나만 존재하며 주요 컨트롤러 접근 경로를 제공하는 루트
//==============================================================================
// 세부 구현 사항
// - 프로젝트 내 유일한 싱글턴입니다.
// - 각 컨트롤러는 직렬화 필드로 보관하고 외부에는 인터페이스로만 공개합니다.
// - 컨트롤러 초기화는 Start 단계에서 명시적으로 호출합니다.
//==============================================================================

public class GameRoot : Singleton<GameRoot>
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Controller")]
    [Tooltip("가로 차트 러닝 시간축과 플레이어 기준 위치를 관리하는 컨트롤러입니다.")]
    [Required]
    [SerializeField]
    private ChartRunController _chartRunController;

    [TabGroup("Controller")]
    [Tooltip("스테이지 데이터를 읽어 차트 노트의 월드 배치 캐시를 생성하는 컨트롤러입니다.")]
    [Required]
    [SerializeField]
    private ChartStageController _chartStageController;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>가로 차트 러닝 시스템의 외부 공개 계약입니다.</summary>
    public IChartRunService ChartRun => _chartRunController;

    /// <summary>차트 스테이지 배치 캐시 시스템의 외부 공개 계약입니다.</summary>
    public IChartStageService ChartStage => _chartStageController;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>등록된 컨트롤러들을 초기화합니다.</summary>
    private void Start()
    {
        InitControllers();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>GameRoot가 보관하는 컨트롤러들을 순서대로 초기화합니다.</summary>
    private void InitControllers()
    {
        if (_chartRunController == null)
        {
            Debug.LogWarning("[GameRoot] ChartRunController가 연결되지 않았습니다.");
            return;
        }

        _chartRunController.Init();

        if (_chartStageController == null)
        {
            Debug.LogWarning("[GameRoot] ChartStageController가 연결되지 않았습니다.");
            return;
        }

        _chartStageController.Init(_chartRunController);
    }
}
