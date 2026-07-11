using Sirenix.OdinInspector;
using UnityEngine;


//==============================================================================
// 기능: class::GameRoot
// 설명: 프로젝트 전역에서 하나만 존재하며 주요 컨트롤러 접근 경로를 제공하는 루트
//==============================================================================
// 세부 구현 사항
// - 프로젝트 내 유일한 싱글턴입니다.
// - 각 컨트롤러는 직렬화 필드로 보관하고 외부에는 인터페이스로만 공개합니다.
// - 컨트롤러와 선택 Presenter 초기화는 Start 단계에서 명시적으로 호출합니다.
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

    [TabGroup("Controller")]
    [Tooltip("Left Shift / Right Shift 입력을 DSP 기준 BUY / SELL 이벤트로 수집하는 컨트롤러입니다.")]
    [Required]
    [SerializeField]
    private ChartInputController _chartInputController;

    [TabGroup("Controller")]
    [Tooltip("BUY / SELL 입력과 차트 노트를 비교해 판정 결과를 생성하는 컨트롤러입니다.")]
    [Required]
    [SerializeField]
    private ChartJudgementController _chartJudgementController;

    [TabGroup("Controller")]
    [Tooltip("스테이지 준비 확인부터 차트 러닝 시작까지의 한 판 시작 흐름을 관리하는 컨트롤러입니다.")]
    [Required]
    [SerializeField]
    private ChartPlayController _chartPlayController;

    [TabGroup("Presenter")]
    [Tooltip("ChartRun CurrentX를 따라 카메라 또는 플레이 표면 루트를 이동시키는 Presenter입니다.")]
    [SerializeField]
    private ChartCameraFollowPresenter _chartCameraFollowPresenter;

    [TabGroup("Presenter")]
    [Tooltip("ChartStage 배치 캐시를 Gameplay 플레이 표면 View에 표시하는 Presenter입니다.")]
    [SerializeField]
    private ChartPlaySurfacePresenter _chartPlaySurfacePresenter;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>가로 차트 러닝 시스템의 외부 공개 계약입니다.</summary>
    public IChartRunService ChartRun => _chartRunController;

    /// <summary>차트 스테이지 배치 캐시 시스템의 외부 공개 계약입니다.</summary>
    public IChartStageService ChartStage => _chartStageController;

    /// <summary>BUY / SELL 입력 수집 시스템의 외부 공개 계약입니다.</summary>
    public IChartInputService ChartInput => _chartInputController;

    /// <summary>차트 노트 판정 시스템의 외부 공개 계약입니다.</summary>
    public IChartJudgementService ChartJudgement => _chartJudgementController;

    /// <summary>차트 플레이 시작 흐름의 외부 공개 계약입니다.</summary>
    public IChartPlayService ChartPlay => _chartPlayController;

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
        InitChartPlaySurfacePresenter();

        if (_chartInputController == null)
        {
            Debug.LogWarning("[GameRoot] ChartInputController가 연결되지 않았습니다.");
            return;
        }

        _chartInputController.Init(_chartRunController);

        if (_chartJudgementController == null)
        {
            Debug.LogWarning("[GameRoot] ChartJudgementController가 연결되지 않았습니다.");
            return;
        }

        _chartJudgementController.Init(
            _chartInputController,
            _chartStageController,
            _chartRunController);
        InitChartCameraFollowPresenter();

        if (_chartPlayController == null)
        {
            Debug.LogWarning("[GameRoot] ChartPlayController가 연결되지 않았습니다.");
            return;
        }

        _chartPlayController.Init(
            _chartStageController,
            _chartInputController,
            _chartJudgementController,
            _chartRunController);
    }

    /// <summary>선택적으로 등록된 카메라 Follow Presenter를 초기화합니다.</summary>
    private void InitChartCameraFollowPresenter()
    {
        if (_chartCameraFollowPresenter == null)
        {
            return;
        }

        _chartCameraFollowPresenter.Init(
            _chartRunController,
            _chartStageController,
            _chartJudgementController);
    }

    /// <summary>선택적으로 등록된 플레이 표면 Presenter를 초기화합니다.</summary>
    private void InitChartPlaySurfacePresenter()
    {
        if (_chartPlaySurfacePresenter == null)
        {
            return;
        }

        _chartPlaySurfacePresenter.Init(_chartStageController);
    }
}
