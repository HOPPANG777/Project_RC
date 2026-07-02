using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartPlayController
// 설명: 스테이지 준비 확인부터 ChartRun 시작까지의 한 판 시작 흐름을 담당하는 컨트롤러
//==============================================================================
// 세부 구현 사항
// - 스테이지 캐시가 준비되었는지 확인합니다.
// - 노트와 판정 결과 상태를 한 판 시작 전 초기화합니다.
// - 입력과 판정을 활성화한 뒤 ChartRun.TryStart()를 호출합니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartPlayController : MonoBehaviour, IChartPlayService
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Settings")]
    [Tooltip("GameRoot 초기화가 끝나면 바로 스테이지 플레이를 시작할지 여부입니다.")]
    [SerializeField]
    private bool _autoStartOnInit = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // GameRoot를 통해 초기화되었는지 여부입니다.
    private bool _isInitialized;

    // 현재 스테이지 준비 상태와 배치 노트 상태를 제공하는 서비스입니다.
    private IChartStageService _chartStageService;

    // BUY / SELL 입력 활성화와 입력 버퍼 초기화를 제공하는 서비스입니다.
    private IChartInputService _chartInputService;

    // 입력 판정 활성화와 판정 상태 초기화를 제공하는 서비스입니다.
    private IChartJudgementService _chartJudgementService;

    // 실제 차트 시간축 시작을 담당하는 러닝 서비스입니다.
    private IChartRunService _chartRunService;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("초기화 여부")]
    public bool IsInitialized => _isInitialized;

    /// <summary>현재 한 판을 시작할 수 있는 준비 상태인지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("시작 가능")]
    public bool CanStartPlay => TryGetStartBlockReason(out string _) == false;

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="chartStageService">스테이지 준비 상태와 배치 노트를 제공하는 서비스입니다.</param>
    /// <param name="chartInputService">BUY / SELL 입력을 제어하는 서비스입니다.</param>
    /// <param name="chartJudgementService">입력 판정을 제어하는 서비스입니다.</param>
    /// <param name="chartRunService">차트 러닝을 시작하는 서비스입니다.</param>
    public void Init(
        IChartStageService chartStageService,
        IChartInputService chartInputService,
        IChartJudgementService chartJudgementService,
        IChartRunService chartRunService)
    {
        _chartStageService = chartStageService;
        _chartInputService = chartInputService;
        _chartJudgementService = chartJudgementService;
        _chartRunService = chartRunService;

        _isInitialized = true;
        LogMissingDependencies();

        if (_autoStartOnInit)
        {
            TryStartPlay();
        }
    }

    //==============================================================================
    // 공개 API (IChartPlayService)
    //==============================================================================

    /// <summary>스테이지와 판정 상태를 준비한 뒤 차트 러닝을 시작합니다.</summary>
    /// <returns>한 판 시작에 성공하면 true입니다.</returns>
    public bool TryStartPlay()
    {
        if (TryGetStartBlockReason(out string blockReason))
        {
            LogWarning(blockReason);
            return false;
        }

        ResetPlayState();
        SetPlaySystemsEnabled(true);

        if (_chartRunService.TryStart())
        {
            return true;
        }

        SetPlaySystemsEnabled(false);
        LogWarning("ChartRun 시작에 실패해 입력과 판정을 비활성화했습니다.");
        return false;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>한 판 시작 전에 남아 있는 노트, 입력, 판정 흔적을 초기화합니다.</summary>
    private void ResetPlayState()
    {
        _chartJudgementService.ResetJudgementState();
        _chartInputService.ClearBufferedInputs();
    }

    /// <summary>
    /// 입력과 판정 처리 활성화 여부를 함께 설정합니다.
    /// </summary>
    /// <param name="isEnabled">입력과 판정을 활성화하려면 true입니다.</param>
    private void SetPlaySystemsEnabled(bool isEnabled)
    {
        _chartInputService.SetInputEnabled(isEnabled);
        _chartJudgementService.SetJudgementEnabled(isEnabled);
    }

    /// <summary>
    /// 시작을 막는 이유가 있는지 확인합니다.
    /// </summary>
    /// <param name="blockReason">시작을 막는 이유입니다.</param>
    /// <returns>시작을 막는 이유가 있으면 true입니다.</returns>
    private bool TryGetStartBlockReason(out string blockReason)
    {
        blockReason = string.Empty;

        if (_isInitialized == false)
        {
            blockReason = "초기화 전에는 플레이를 시작할 수 없습니다.";
            return true;
        }

        if (_chartStageService == null)
        {
            blockReason = "ChartStage 서비스가 없어 플레이를 시작할 수 없습니다.";
            return true;
        }

        if (_chartStageService.IsInitialized == false)
        {
            blockReason = "ChartStage 서비스가 초기화되지 않았습니다.";
            return true;
        }

        if (_chartStageService.HasStage == false)
        {
            blockReason = "준비된 스테이지가 없어 플레이를 시작할 수 없습니다.";
            return true;
        }

        if (_chartStageService.PlacedNoteCount <= 0)
        {
            blockReason = "배치된 노트가 없어 플레이를 시작할 수 없습니다.";
            return true;
        }

        if (_chartInputService == null)
        {
            blockReason = "ChartInput 서비스가 없어 플레이를 시작할 수 없습니다.";
            return true;
        }

        if (_chartInputService.IsInitialized == false)
        {
            blockReason = "ChartInput 서비스가 초기화되지 않았습니다.";
            return true;
        }

        if (_chartJudgementService == null)
        {
            blockReason = "ChartJudgement 서비스가 없어 플레이를 시작할 수 없습니다.";
            return true;
        }

        if (_chartJudgementService.IsInitialized == false)
        {
            blockReason = "ChartJudgement 서비스가 초기화되지 않았습니다.";
            return true;
        }

        if (_chartRunService == null)
        {
            blockReason = "ChartRun 서비스가 없어 플레이를 시작할 수 없습니다.";
            return true;
        }

        if (_chartRunService.IsRunning)
        {
            blockReason = "이미 차트 러닝이 진행 중입니다.";
            return true;
        }

        return false;
    }

    /// <summary>초기화 시 누락된 의존성이 있는지 확인해 경고를 출력합니다.</summary>
    private void LogMissingDependencies()
    {
        if (_chartStageService == null)
        {
            LogWarning("ChartStage 서비스가 연결되지 않았습니다.");
        }

        if (_chartInputService == null)
        {
            LogWarning("ChartInput 서비스가 연결되지 않았습니다.");
        }

        if (_chartJudgementService == null)
        {
            LogWarning("ChartJudgement 서비스가 연결되지 않았습니다.");
        }

        if (_chartRunService == null)
        {
            LogWarning("ChartRun 서비스가 연결되지 않았습니다.");
        }
    }

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartPlayController] {message}");
    }
}
