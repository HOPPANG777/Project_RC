using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

//==============================================================================
// 기능: class::ChartInputController
// 설명: BUY / SELL 키 입력을 DSP 기준 ChartInputEvent로 변환해 판정 시스템에 전달하는 컨트롤러
//==============================================================================
// 내부 구현 사항
// - MVP 입력은 Left Shift = BUY, Right Shift = SELL로 고정합니다.
// - 입력 순간의 AudioSettings.dspTime을 기록하고 ChartRun 기준 진행 시간으로 변환합니다.
// - JudgementController가 프레임 손실 없이 순서대로 소비할 수 있도록 입력 이벤트를 버퍼에 보관합니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartInputController : MonoBehaviour, IChartInputService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 입력 버퍼가 비정상적으로 0 이하가 되는 상황을 막기 위한 최소값입니다.
    private const int MIN_BUFFERED_INPUT_COUNT = 1;

    // 판정 쪽 소비가 잠시 늦어져도 입력을 잃지 않기 위한 기본 버퍼 크기입니다.
    private const int DEFAULT_BUFFERED_INPUT_COUNT = 16;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Settings")]
    [Tooltip("GameRoot 초기화 직후 BUY / SELL 입력을 받을지 여부입니다.")]
    [SerializeField]
    private bool _inputEnabledOnStart = true;

    [TabGroup("Settings")]
    [Tooltip("판정 쪽에서 소비하기 전까지 보관할 최대 입력 이벤트 수입니다.")]
    [MinValue(MIN_BUFFERED_INPUT_COUNT)]
    [SerializeField]
    private int _maxBufferedInputCount = DEFAULT_BUFFERED_INPUT_COUNT;

    [TabGroup("Settings")]
    [Tooltip("입력을 비활성화할 때 남아 있는 입력 버퍼를 함께 비울지 여부입니다.")]
    [SerializeField]
    private bool _clearBufferOnDisable = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // GameRoot를 통해 초기화되었는지 여부입니다.
    private bool _isInitialized;

    // 현재 BUY / SELL 입력을 받을 수 있는지 여부입니다.
    private bool _isInputEnabled;

    // DSP 입력 시간을 스테이지 진행 시간으로 변환하는 ChartRun 서비스입니다.
    private IChartPositionProvider _chartPositionProvider;

    // 판정 시스템이 순서대로 소비할 입력 이벤트 버퍼입니다.
    private readonly Queue<ChartInputEvent> _inputBuffer = new Queue<ChartInputEvent>();

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("초기화 여부")]
    public bool IsInitialized => _isInitialized;

    /// <summary>현재 BUY / SELL 입력을 받을 수 있는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("입력 활성화")]
    public bool IsInputEnabled => _isInputEnabled;

    /// <summary>아직 판정 쪽에서 소비하지 않은 입력 수입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("버퍼 입력 수")]
    public int BufferedInputCount => _inputBuffer.Count;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>매 프레임 BUY / SELL 키 입력을 확인합니다.</summary>
    private void Update()
    {
        if (CanReadInput() == false)
        {
            return;
        }

        bool buyPressed = WasBuyPressedThisFrame();
        bool sellPressed = WasSellPressedThisFrame();

        if (buyPressed == false && sellPressed == false)
        {
            return;
        }

        double inputDspTime = AudioSettings.dspTime;
        int frameCount = Time.frameCount;

        if (buyPressed)
        {
            CaptureInput(ChartNoteSide.NOTE_SIDE_BUY, inputDspTime, frameCount);
        }

        if (sellPressed)
        {
            CaptureInput(ChartNoteSide.NOTE_SIDE_SELL, inputDspTime, frameCount);
        }
    }

    /// <summary>인스펙터 값이 바뀔 때 잘못된 설정값을 보정합니다.</summary>
    private void OnValidate()
    {
        ClampSettings();
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="chartPositionProvider">입력 DSP 시간을 스테이지 진행 시간으로 변환하는 ChartRun 서비스입니다.</param>
    public void Init(IChartPositionProvider chartPositionProvider)
    {
        _chartPositionProvider = chartPositionProvider;
        ClampSettings();
        ClearBufferedInputs();

        _isInputEnabled = _inputEnabledOnStart;
        _isInitialized = true;

        if (_chartPositionProvider == null)
        {
            LogWarning("ChartRun 위치 서비스가 없어 입력 시간을 스테이지 시간으로 변환할 수 없습니다.");
        }
    }

    //==============================================================================
    // 공개 API (IChartInputService)
    //==============================================================================

    /// <summary>
    /// BUY / SELL 입력 활성화 여부를 설정합니다.
    /// </summary>
    /// <param name="isEnabled">입력을 받을 수 있으면 true입니다.</param>
    public void SetInputEnabled(bool isEnabled)
    {
        if (_isInputEnabled == isEnabled)
        {
            return;
        }

        _isInputEnabled = isEnabled;

        if (_isInputEnabled == false && _clearBufferOnDisable)
        {
            ClearBufferedInputs();
        }
    }

    /// <summary>아직 소비하지 않은 입력 버퍼를 모두 비웁니다.</summary>
    public void ClearBufferedInputs()
    {
        _inputBuffer.Clear();
    }

    /// <summary>
    /// 가장 오래된 입력 이벤트를 하나 꺼냅니다.
    /// </summary>
    /// <param name="inputEvent">소비할 BUY / SELL 입력 이벤트입니다.</param>
    /// <returns>소비할 입력이 있으면 true입니다.</returns>
    public bool TryConsumeInput(out ChartInputEvent inputEvent)
    {
        inputEvent = default;

        if (_inputBuffer.Count <= 0)
        {
            return false;
        }

        inputEvent = _inputBuffer.Dequeue();
        return inputEvent.IsValid;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>입력 이벤트를 읽어도 되는 상태인지 확인합니다.</summary>
    /// <returns>입력을 읽을 수 있으면 true입니다.</returns>
    private bool CanReadInput()
    {
        if (_isInitialized == false)
        {
            return false;
        }

        if (_isInputEnabled == false)
        {
            return false;
        }

        return _chartPositionProvider != null;
    }

    /// <summary>
    /// 입력 방향과 DSP 시간을 ChartInputEvent로 변환해 버퍼에 저장합니다.
    /// </summary>
    /// <param name="side">입력된 BUY / SELL 방향입니다.</param>
    /// <param name="inputDspTime">입력이 발생한 AudioSettings.dspTime입니다.</param>
    /// <param name="frameCount">입력이 감지된 Unity 프레임 번호입니다.</param>
    private void CaptureInput(ChartNoteSide side, double inputDspTime, int frameCount)
    {
        float inputRunTime = _chartPositionProvider.GetRunTimeAtDspTime(inputDspTime);
        ChartInputEvent inputEvent = new ChartInputEvent(
            side,
            inputDspTime,
            inputRunTime,
            frameCount);

        EnqueueInput(inputEvent);
    }

    /// <summary>
    /// 입력 이벤트를 버퍼에 추가하고, 버퍼가 가득 차면 가장 오래된 입력을 제거합니다.
    /// </summary>
    /// <param name="inputEvent">추가할 입력 이벤트입니다.</param>
    private void EnqueueInput(ChartInputEvent inputEvent)
    {
        while (_inputBuffer.Count >= _maxBufferedInputCount)
        {
            _inputBuffer.Dequeue();
            LogWarning("입력 버퍼가 가득 차 가장 오래된 입력을 제거했습니다.");
        }

        _inputBuffer.Enqueue(inputEvent);
    }

    /// <summary>설정값을 안전한 범위로 보정합니다.</summary>
    private void ClampSettings()
    {
        if (_maxBufferedInputCount < MIN_BUFFERED_INPUT_COUNT)
        {
            _maxBufferedInputCount = DEFAULT_BUFFERED_INPUT_COUNT;
        }
    }

    /// <summary>BUY 입력 키가 이번 프레임에 눌렸는지 확인합니다.</summary>
    /// <returns>Left Shift가 눌렸으면 true입니다.</returns>
    private static bool WasBuyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.leftShiftKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.LeftShift);
#endif
    }

    /// <summary>SELL 입력 키가 이번 프레임에 눌렸는지 확인합니다.</summary>
    /// <returns>Right Shift가 눌렸으면 true입니다.</returns>
    private static bool WasSellPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.rightShiftKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.RightShift);
#endif
    }

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartInputController] {message}");
    }
}
