using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartRunController
// 설명: 가로 차트 위를 달리는 플레이어 기준점의 시간축과 X축 이동을 관리하는 컨트롤러
//==============================================================================
// 세부 구현 사항
// - 차트와 캔들 노드는 월드에 고정되어 있고, 플레이어 기준점만 X축으로 이동합니다.
// - 판정, 입력, 점수, 카메라 보간은 다른 컨트롤러의 책임으로 분리합니다.
// - 0.1초 = 1 캔들/노드 규칙을 월드 X 위치로 변환합니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartRunController : MonoBehaviour, IChartRunService
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 스테이지 길이가 0이 되는 상황을 막기 위한 최소 진행 시간입니다.
    private const float MIN_STAGE_DURATION = 0.01f;

    // 노드 간격이 0이 되는 상황을 막기 위한 최소 노드 시간 간격입니다.
    private const float MIN_NODE_INTERVAL = 0.01f;

    // 시간에서 X 위치로 변환할 때 0 나누기를 막기 위한 최소 노드 거리입니다.
    private const float MIN_UNITS_PER_NODE = 0.01f;

    // 오디오 DSP 예약 시작 지연의 최소값입니다.
    private const float MIN_DSP_START_DELAY = 0.0f;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Refs")]
    [Tooltip("차트 시간축을 따라 이동할 플레이어 또는 판정 기준점 Transform입니다.")]
    [Required]
    [SerializeField]
    private Transform _runnerTransform;

    [TabGroup("Refs")]
    [Tooltip("차트 러닝과 동기화할 음악 AudioSource입니다. 없으면 DSP 시간만 기준으로 진행합니다.")]
    [SerializeField]
    private AudioSource _musicAudioSource;

    [TabGroup("Settings")]
    [Tooltip("스테이지가 시작되는 월드 X 위치입니다.")]
    [SerializeField]
    private float _startX = 0.0f;

    [TabGroup("Settings")]
    [Tooltip("러너가 이동할 고정 월드 Y 위치입니다.")]
    [SerializeField]
    private float _runnerY = 0.0f;

    [TabGroup("Settings")]
    [Tooltip("캔들/노드 하나가 차지하는 시간 간격입니다.")]
    [MinValue(MIN_NODE_INTERVAL)]
    [SerializeField]
    private float _nodeInterval = 0.1f;

    [TabGroup("Settings")]
    [Tooltip("캔들/노드 하나가 차지하는 월드 X 거리입니다.")]
    [MinValue(MIN_UNITS_PER_NODE)]
    [SerializeField]
    private float _unitsPerNode = 1.0f;

    [TabGroup("Settings")]
    [Tooltip("스테이지 전체 진행 시간입니다.")]
    [MinValue(MIN_STAGE_DURATION)]
    [SerializeField]
    private float _stageDuration = 30.0f;

    [TabGroup("Settings")]
    [Tooltip("AudioSettings.dspTime 기준으로 시작을 예약할 지연 시간입니다.")]
    [MinValue(MIN_DSP_START_DELAY)]
    [SerializeField]
    private float _dspStartDelay = 0.1f;

    [TabGroup("Settings")]
    [Tooltip("러닝 시작/일시정지/정지 시 연결된 AudioSource도 함께 제어할지 여부입니다.")]
    [SerializeField]
    private bool _controlMusicAudioSource = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // GameRoot를 통해 초기화되었는지 여부입니다.
    private bool _isInitialized;

    // 현재 스테이지가 진행 중인지 여부입니다.
    private bool _isRunning;

    // 현재 스테이지가 일시정지 상태인지 여부입니다.
    private bool _isPaused;

    // DSP 예약 시작 전 대기 중인지 여부입니다.
    private bool _isWaitingForDspStart;

    // 현재 스테이지 진행 시간입니다.
    private float _currentTime;

    // 현재 플레이어 기준점의 차트 X 위치입니다.
    private float _currentX;

    // 현재 0.1초 기준 캔들/노드 인덱스입니다.
    private int _currentNodeIndex;

    // 차트 러닝이 시작되도록 예약된 Unity 오디오 DSP 시간입니다.
    private double _runStartDspTime;

    // 일시정지를 시작한 Unity 오디오 DSP 시간입니다.
    private double _pauseStartDspTime;

    // 누적 일시정지 시간을 DSP 시간 기준으로 기록한 값입니다.
    private double _totalPausedDspDuration;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>GameRoot를 통해 초기화되었는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("초기화 여부")]
    private bool IsInitialized => _isInitialized;

    /// <summary>현재 진행 중인지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("진행 중")]
    public bool IsRunning => _isRunning;

    /// <summary>현재 일시정지 상태인지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("일시정지")]
    public bool IsPaused => _isPaused;

    /// <summary>DSP 예약 시작 전 대기 중인지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("DSP 시작 대기")]
    private bool IsWaitingForDspStart => _isWaitingForDspStart;

    /// <summary>스테이지 전체 진행 시간입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("스테이지 길이")]
    public float StageDuration => _stageDuration;

    /// <summary>캔들/노드 하나가 차지하는 시간 간격입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("노드 시간 간격")]
    public float NodeInterval => _nodeInterval;

    /// <summary>현재 Unity 오디오 DSP 시간입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("현재 DSP 시간")]
    public double CurrentDspTime => AudioSettings.dspTime;

    /// <summary>현재 러닝이 시작된 오디오 DSP 시간입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("러닝 시작 DSP 시간")]
    public double RunStartDspTime => _runStartDspTime;

    /// <summary>현재 스테이지 진행 시간입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("현재 시간")]
    public float CurrentTime => _currentTime;

    /// <summary>DSP 시간으로 즉시 계산한 현재 스테이지 진행 시간입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("DSP 기준 현재 시간")]
    private float DspCurrentTime => GetCurrentRunTime();

    /// <summary>현재 플레이어 기준점의 차트 X 위치입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("현재 X 위치")]
    public float CurrentX => _currentX;

    /// <summary>현재 시간 기준의 캔들/노드 인덱스입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("현재 노드 인덱스")]
    public int CurrentNodeIndex => _currentNodeIndex;

    /// <summary>스테이지 전체 진행률입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("진행률")]
    [ProgressBar(0.0f, 1.0f)]
    public float NormalizedProgress
    {
        get
        {
            if (_stageDuration <= 0.0f)
            {
                return 0.0f;
            }

            return Mathf.Clamp01(_currentTime / _stageDuration);
        }
    }

    /// <summary>러너 Transform의 현재 월드 위치입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("러너 월드 위치")]
    private Vector3 RunnerPosition
    {
        get
        {
            if (_runnerTransform == null)
            {
                return Vector3.zero;
            }

            return _runnerTransform.position;
        }
    }

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>프레임마다 러너 진행 시간을 갱신합니다.</summary>
    private void Update()
    {
        if (_isRunning == false)
        {
            return;
        }

        if (_isPaused)
        {
            return;
        }

        SyncRunTimeFromDsp();
    }

    /// <summary>인스펙터 값이 바뀔 때 잘못된 설정값을 보정합니다.</summary>
    private void OnValidate()
    {
        ClampSettings();
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>GameRoot에서 호출하는 초기화 함수입니다.</summary>
    public void Init()
    {
        ClampSettings();
        ResetRunState();

        _isInitialized = true;
    }

    //==============================================================================
    // 공개 API (IRunnable)
    //==============================================================================

    /// <summary>차트 러닝을 시작합니다.</summary>
    /// <returns>시작에 성공하면 true입니다.</returns>
    public bool TryStart()
    {
        if (_isInitialized == false)
        {
            Init();
        }

        if (_isRunning)
        {
            LogWarning("이미 차트 러닝이 진행 중입니다.");
            return false;
        }

        if (CanRun() == false)
        {
            return false;
        }

        _currentTime = 0.0f;
        PrepareDspStart();
        _isRunning = true;
        _isPaused = false;

        RefreshRunPosition();
        PlayMusicSourceScheduled();
        return true;
    }

    /// <summary>차트 러닝을 일시정지합니다.</summary>
    public void Pause()
    {
        if (_isRunning == false)
        {
            return;
        }

        if (_isPaused)
        {
            return;
        }

        SyncRunTimeFromDsp();
        _pauseStartDspTime = AudioSettings.dspTime;
        _isPaused = true;
        PauseMusicSource();
    }

    /// <summary>일시정지된 차트 러닝을 재개합니다.</summary>
    public void Resume()
    {
        if (_isRunning == false)
        {
            return;
        }

        if (_isPaused == false)
        {
            return;
        }

        double currentDspTime = AudioSettings.dspTime;
        double pausedDuration = currentDspTime - _pauseStartDspTime;

        if (pausedDuration > 0.0d)
        {
            _totalPausedDspDuration += pausedDuration;
        }

        _pauseStartDspTime = 0.0d;
        _isPaused = false;
        ResumeMusicSource();
    }

    /// <summary>차트 러닝을 중지하고 시작 위치로 되돌립니다.</summary>
    public void Stop()
    {
        StopMusicSource();
        ResetRunState();
    }

    //==============================================================================
    // 공개 API (IChartPositionProvider)
    //==============================================================================

    /// <summary>특정 시간에 해당하는 차트 X 위치를 계산합니다.</summary>
    /// <param name="time">계산할 시간입니다.</param>
    /// <returns>해당 시간의 차트 X 위치입니다.</returns>
    public float GetXAtTime(float time)
    {
        float clampedTime = Mathf.Clamp(time, 0.0f, _stageDuration);
        return _startX + (clampedTime / _nodeInterval) * _unitsPerNode;
    }

    /// <summary>특정 차트 X 위치에 해당하는 시간을 계산합니다.</summary>
    /// <param name="x">계산할 차트 X 위치입니다.</param>
    /// <returns>해당 X 위치의 시간입니다.</returns>
    public float GetTimeAtX(float x)
    {
        float rawTime = ((x - _startX) / _unitsPerNode) * _nodeInterval;
        return Mathf.Clamp(rawTime, 0.0f, _stageDuration);
    }

    /// <summary>특정 시간에 해당하는 캔들/노드 인덱스를 계산합니다.</summary>
    /// <param name="time">계산할 시간입니다.</param>
    /// <returns>해당 시간의 캔들/노드 인덱스입니다.</returns>
    public int GetNodeIndexAtTime(float time)
    {
        float clampedTime = Mathf.Clamp(time, 0.0f, _stageDuration);
        int nodeIndex = Mathf.FloorToInt(clampedTime / _nodeInterval);
        int maxNodeIndex = Mathf.FloorToInt(_stageDuration / _nodeInterval);

        return Mathf.Clamp(nodeIndex, 0, maxNodeIndex);
    }

    /// <summary>현재 오디오 DSP 시간을 기준으로 스테이지 진행 시간을 계산합니다.</summary>
    /// <returns>현재 스테이지 진행 시간입니다.</returns>
    public float GetCurrentRunTime()
    {
        return GetRunTimeAtDspTime(AudioSettings.dspTime);
    }

    /// <summary>특정 오디오 DSP 시간에 해당하는 스테이지 진행 시간을 계산합니다.</summary>
    /// <param name="dspTime">계산할 오디오 DSP 시간입니다.</param>
    /// <returns>해당 DSP 시간의 스테이지 진행 시간입니다.</returns>
    public float GetRunTimeAtDspTime(double dspTime)
    {
        if (_isRunning == false)
        {
            return _currentTime;
        }

        if (_runStartDspTime <= 0.0d)
        {
            return _currentTime;
        }

        double pausedDuration = _totalPausedDspDuration;

        if (_isPaused && dspTime > _pauseStartDspTime)
        {
            pausedDuration += dspTime - _pauseStartDspTime;
        }

        double rawRunTime = dspTime - _runStartDspTime - pausedDuration;

        if (rawRunTime <= 0.0d)
        {
            return 0.0f;
        }

        return Mathf.Clamp((float)rawRunTime, 0.0f, _stageDuration);
    }

    //==============================================================================
    // 공개 API (테스트 / 편의)
    //==============================================================================

    /// <summary>러너를 특정 스테이지 시간 위치로 이동시킵니다.</summary>
    /// <param name="time">이동할 스테이지 시간입니다.</param>
    /// <returns>이동에 성공하면 true입니다.</returns>
    public bool TrySeek(float time)
    {
        if (_isInitialized == false)
        {
            Init();
        }

        if (CanRun() == false)
        {
            return false;
        }

        _currentTime = Mathf.Clamp(time, 0.0f, _stageDuration);
        RebaseDspStartToCurrentTime();
        RefreshRunPosition();
        SyncMusicSourceToCurrentTime();
        return true;
    }

    /// <summary>러너 Transform 참조를 런타임에 교체합니다.</summary>
    /// <param name="runnerTransform">새로 사용할 러너 Transform입니다.</param>
    /// <returns>교체에 성공하면 true입니다.</returns>
    public bool TrySetRunnerTransform(Transform runnerTransform)
    {
        if (runnerTransform == null)
        {
            LogWarning("러너 Transform 교체 실패: 전달된 참조가 null입니다.");
            return false;
        }

        _runnerTransform = runnerTransform;
        RefreshRunPosition();
        return true;
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>설정값을 안전한 범위로 보정합니다.</summary>
    private void ClampSettings()
    {
        _stageDuration = GetSafePositiveValue(_stageDuration, 30.0f, MIN_STAGE_DURATION);
        _nodeInterval = GetSafePositiveValue(_nodeInterval, 0.1f, MIN_NODE_INTERVAL);
        _unitsPerNode = GetSafePositiveValue(_unitsPerNode, 1.0f, MIN_UNITS_PER_NODE);
        _dspStartDelay = GetSafeNonNegativeValue(_dspStartDelay, 0.1f);
    }

    /// <summary>러닝 상태를 초기값으로 되돌리고 러너를 시작 위치에 배치합니다.</summary>
    private void ResetRunState()
    {
        _isRunning = false;
        _isPaused = false;
        _isWaitingForDspStart = false;
        _currentTime = 0.0f;
        _runStartDspTime = 0.0d;
        _pauseStartDspTime = 0.0d;
        _totalPausedDspDuration = 0.0d;

        RefreshRunPosition();
    }

    /// <summary>오디오 DSP 시간을 기준으로 현재 러닝 시간을 동기화합니다.</summary>
    private void SyncRunTimeFromDsp()
    {
        _currentTime = GetCurrentRunTime();
        _isWaitingForDspStart = AudioSettings.dspTime < _runStartDspTime;

        if (_currentTime >= _stageDuration)
        {
            _currentTime = _stageDuration;
            RefreshRunPosition();
            CompleteRun();
            return;
        }

        RefreshRunPosition();
    }

    /// <summary>스테이지 완료 상태로 전환합니다.</summary>
    private void CompleteRun()
    {
        _isRunning = false;
        _isPaused = false;
        _isWaitingForDspStart = false;
        StopMusicSource();
    }

    /// <summary>현재 DSP 시간을 기준으로 차트 러닝 시작 시간을 예약합니다.</summary>
    private void PrepareDspStart()
    {
        _runStartDspTime = AudioSettings.dspTime + _dspStartDelay;
        _pauseStartDspTime = 0.0d;
        _totalPausedDspDuration = 0.0d;
        _isWaitingForDspStart = _dspStartDelay > 0.0f;
    }

    /// <summary>현재 스테이지 시간에 맞춰 DSP 시작 기준점을 다시 계산합니다.</summary>
    private void RebaseDspStartToCurrentTime()
    {
        double currentDspTime = AudioSettings.dspTime;

        _runStartDspTime = currentDspTime - _currentTime;
        _pauseStartDspTime = _isPaused ? currentDspTime : 0.0d;
        _totalPausedDspDuration = 0.0d;
        _isWaitingForDspStart = false;
    }

    /// <summary>현재 시간 기준으로 X 위치와 노드 인덱스를 갱신합니다.</summary>
    private void RefreshRunPosition()
    {
        _currentX = GetXAtTime(_currentTime);
        _currentNodeIndex = GetNodeIndexAtTime(_currentTime);

        MoveRunnerToCurrentPosition();
    }

    /// <summary>러너 Transform을 현재 차트 위치로 이동시킵니다.</summary>
    private void MoveRunnerToCurrentPosition()
    {
        if (_runnerTransform == null)
        {
            return;
        }

        Vector3 nextPosition = _runnerTransform.position;
        nextPosition.x = _currentX;
        nextPosition.y = _runnerY;

        _runnerTransform.position = nextPosition;
    }

    /// <summary>러닝에 필요한 필수 참조와 설정값이 유효한지 확인합니다.</summary>
    /// <returns>러닝 가능한 상태이면 true입니다.</returns>
    private bool CanRun()
    {
        if (_runnerTransform == null)
        {
            LogWarning("러너 Transform이 연결되지 않아 차트 러닝을 시작할 수 없습니다.");
            return false;
        }

        if (_stageDuration <= 0.0f)
        {
            LogWarning("스테이지 길이가 0 이하라 차트 러닝을 시작할 수 없습니다.");
            return false;
        }

        if (_nodeInterval <= 0.0f)
        {
            LogWarning("노드 시간 간격이 0 이하라 차트 러닝을 시작할 수 없습니다.");
            return false;
        }

        if (_unitsPerNode <= 0.0f)
        {
            LogWarning("노드 월드 거리가 0 이하라 차트 러닝을 시작할 수 없습니다.");
            return false;
        }

        return true;
    }

    /// <summary>연결된 음악 AudioSource를 DSP 시작 시간에 맞춰 예약 재생합니다.</summary>
    private void PlayMusicSourceScheduled()
    {
        if (CanControlMusicSource() == false)
        {
            return;
        }

        _musicAudioSource.Stop();
        _musicAudioSource.time = 0.0f;
        _musicAudioSource.PlayScheduled(_runStartDspTime);
    }

    /// <summary>연결된 음악 AudioSource를 일시정지합니다.</summary>
    private void PauseMusicSource()
    {
        if (CanControlMusicSource() == false)
        {
            return;
        }

        _musicAudioSource.Pause();
    }

    /// <summary>연결된 음악 AudioSource를 재개합니다.</summary>
    private void ResumeMusicSource()
    {
        if (CanControlMusicSource() == false)
        {
            return;
        }

        _musicAudioSource.UnPause();
    }

    /// <summary>연결된 음악 AudioSource를 정지합니다.</summary>
    private void StopMusicSource()
    {
        if (CanControlMusicSource() == false)
        {
            return;
        }

        _musicAudioSource.Stop();
    }

    /// <summary>연결된 음악 AudioSource의 재생 위치를 현재 스테이지 시간에 맞춥니다.</summary>
    private void SyncMusicSourceToCurrentTime()
    {
        if (CanControlMusicSource() == false)
        {
            return;
        }

        _musicAudioSource.time = _currentTime;

        if (_isRunning && _isPaused == false)
        {
            _musicAudioSource.Play();
        }
    }

    /// <summary>음악 AudioSource를 이 컨트롤러가 제어할 수 있는지 확인합니다.</summary>
    /// <returns>제어 가능한 상태이면 true입니다.</returns>
    private bool CanControlMusicSource()
    {
        if (_controlMusicAudioSource == false)
        {
            return false;
        }

        if (_musicAudioSource == null)
        {
            return false;
        }

        if (_musicAudioSource.clip == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>양수여야 하는 설정값을 안전하게 보정합니다.</summary>
    /// <param name="value">검증할 원본 값입니다.</param>
    /// <param name="fallback">잘못된 값일 때 사용할 대체 값입니다.</param>
    /// <param name="minimum">허용할 최소 값입니다.</param>
    /// <returns>보정된 양수 값입니다.</returns>
    private static float GetSafePositiveValue(float value, float fallback, float minimum)
    {
        if (float.IsNaN(value))
        {
            return fallback;
        }

        if (float.IsInfinity(value))
        {
            return fallback;
        }

        return Mathf.Max(value, minimum);
    }

    /// <summary>0 이상이어야 하는 설정값을 안전하게 보정합니다.</summary>
    /// <param name="value">검증할 원본 값입니다.</param>
    /// <param name="fallback">잘못된 값일 때 사용할 대체 값입니다.</param>
    /// <returns>보정된 0 이상의 값입니다.</returns>
    private static float GetSafeNonNegativeValue(float value, float fallback)
    {
        if (float.IsNaN(value))
        {
            return fallback;
        }

        if (float.IsInfinity(value))
        {
            return fallback;
        }

        return Mathf.Max(value, 0.0f);
    }

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartRunController] {message}");
    }
}
