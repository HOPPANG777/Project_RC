//==============================================================================
// 기능: struct::ChartInputEvent
// 설명: BUY / SELL 입력 1회의 방향과 DSP 기준 시간을 담는 런타임 데이터
//==============================================================================
// 세부 구현 사항
// - ChartInputController가 입력 순간의 AudioSettings.dspTime을 기록해 생성합니다.
// - InputRunTime은 ChartRun.GetRunTimeAtDspTime(inputDspTime)으로 변환한 스테이지 시간입니다.
// - JudgementController는 이 데이터를 소비해 가까운 노트와 판정 폭을 계산합니다.
//==============================================================================

public readonly struct ChartInputEvent
{
    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 입력된 BUY 또는 SELL 방향입니다.
    private readonly ChartNoteSide _side;

    // 입력이 발생한 AudioSettings.dspTime입니다.
    private readonly double _inputDspTime;

    // 입력 DSP 시간을 ChartRun 기준으로 변환한 스테이지 진행 시간입니다.
    private readonly float _inputRunTime;

    // 입력이 감지된 Unity 프레임 번호입니다.
    private readonly int _frameCount;

    // 기본값 이벤트가 실제 입력처럼 소비되는 것을 막기 위한 유효성 플래그입니다.
    private readonly bool _isValid;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>입력된 BUY 또는 SELL 방향입니다.</summary>
    public ChartNoteSide Side => _side;

    /// <summary>입력이 발생한 AudioSettings.dspTime입니다.</summary>
    public double InputDspTime => _inputDspTime;

    /// <summary>입력 DSP 시간을 ChartRun 기준으로 변환한 스테이지 진행 시간입니다.</summary>
    public float InputRunTime => _inputRunTime;

    /// <summary>입력이 감지된 Unity 프레임 번호입니다.</summary>
    public int FrameCount => _frameCount;

    /// <summary>실제 입력에서 생성된 이벤트인지 여부입니다.</summary>
    public bool IsValid => _isValid;

    //==============================================================================
    // 생성자
    //==============================================================================

    /// <summary>
    /// BUY / SELL 입력 이벤트를 생성합니다.
    /// </summary>
    /// <param name="side">입력된 BUY 또는 SELL 방향입니다.</param>
    /// <param name="inputDspTime">입력이 발생한 AudioSettings.dspTime입니다.</param>
    /// <param name="inputRunTime">입력 DSP 시간을 ChartRun 기준으로 변환한 스테이지 진행 시간입니다.</param>
    /// <param name="frameCount">입력이 감지된 Unity 프레임 번호입니다.</param>
    public ChartInputEvent(
        ChartNoteSide side,
        double inputDspTime,
        float inputRunTime,
        int frameCount)
    {
        _side = side;
        _inputDspTime = inputDspTime;
        _inputRunTime = inputRunTime;
        _frameCount = frameCount;
        _isValid = true;
    }
}
