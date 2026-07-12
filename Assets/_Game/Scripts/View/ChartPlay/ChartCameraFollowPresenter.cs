using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartCameraFollowPresenter
// 설명: ChartRun의 CurrentX를 따라 카메라 또는 플레이 표면 루트를 이동시키는 Presenter
//==============================================================================
// 세부 구현 사항
// - Directing 기준의 긴 30초 차트 레인에서 판정선과 노트가 화면 밖으로 나가지 않게 합니다.
// - ChartRunController 구현체가 아니라 IChartRunService 계약의 CurrentX만 사용합니다.
// - 기본 모드는 카메라 이동이며, 고정 카메라 구성이 필요하면 플레이 표면 루트를 반대로 이동시킵니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartCameraFollowPresenter : MonoBehaviour
{
    //==============================================================================
    // 상수
    //==============================================================================

    // Follow 배율이 음수가 되어 진행 방향이 뒤집히는 상황을 막기 위한 최소값입니다.
    private const float MIN_FOLLOW_SCALE = 0.0f;

    // CurrentX를 월드 X 이동량으로 반영할 기본 배율입니다.
    private const float DEFAULT_FOLLOW_SCALE = 1.0f;

    private const float DEFAULT_VERTICAL_FOLLOW_SHARPNESS = 10.0f;

    private const float DEFAULT_INPUT_NOTE_FAST_APPROACH_RATIO = 0.75f;

    private const float DEFAULT_INPUT_NOTE_APPROACH_SHARPNESS = 16.0f;

    private const float DEFAULT_INPUT_NOTE_ARRIVAL_DISTANCE = 0.02f;

    private const float DEFAULT_INPUT_NOTE_ACCELERATION_DURATION = 0.08f;

    private const float MIN_NORMALIZED_RATIO = 0.0f;

    private const float MAX_NORMALIZED_RATIO = 1.0f;

    // 입력 캔들 목표로 이동할 때 사용하는 기본 빠른 접근 비율입니다.

    // 입력 캔들 목표로 이동할 때 사용하는 기본 보간 강도입니다.

    // 입력 캔들 목표 도달로 인정할 기본 거리입니다.

    // 입력 캔들 포커스 이동 시작 시 첫 프레임 급가속을 막는 기본 램프 시간입니다.

    // 비율 보정에 사용하는 최소값입니다.

    // 비율 보정에 사용하는 최대값입니다.

    //==============================================================================
    // 내부 타입
    //==============================================================================

    private enum ChartCameraFollowMode
    {
        FOLLOW_MODE_CAMERA = 0,
        FOLLOW_MODE_SURFACE_ROOT = 1
    }

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Refs")]
    [Tooltip("CurrentX를 따라 이동할 카메라입니다. 비어 있으면 같은 오브젝트의 Camera, 이후 Camera.main 순서로 사용합니다.")]
    [SerializeField]
    private Camera _targetCamera;

    [TabGroup("Refs")]
    [Tooltip("플레이 표면 루트 이동 모드에서 사용할 ChartPlaySurfaceView입니다.")]
    [SerializeField]
    private ChartPlaySurfaceView _playSurfaceView;

    [TabGroup("Refs")]
    [Tooltip("플레이 표면 루트 이동 모드에서 ChartPlaySurfaceView 대신 직접 이동할 Transform입니다.")]
    [SerializeField]
    private Transform _surfaceRootOverride;

    [TabGroup("Settings")]
    [Tooltip("CurrentX를 카메라 이동으로 따라갈지, 플레이 표면 루트 반대 이동으로 따라갈지 선택합니다.")]
    [SerializeField]
    private ChartCameraFollowMode _followMode = ChartCameraFollowMode.FOLLOW_MODE_CAMERA;

    [TabGroup("Settings")]
    [Tooltip("GameRoot에 직접 연결되지 않았을 때 런타임에 GameRoot에서 ChartRun 서비스를 자동으로 가져올지 여부입니다.")]
    [SerializeField]
    private bool _autoBindFromGameRoot = true;

    [TabGroup("Settings")]
    [Tooltip("초기화 직후 Follow를 활성화할지 여부입니다.")]
    [SerializeField]
    private bool _followEnabledOnStart = true;

    [TabGroup("Settings")]
    [Tooltip("ChartRun CurrentX 변화량을 월드 X 이동량으로 반영할 배율입니다.")]
    [MinValue(MIN_FOLLOW_SCALE)]
    [SerializeField]
    private float _followScale = DEFAULT_FOLLOW_SCALE;

    [TabGroup("Settings")]
    [Tooltip("Follow 결과에 추가할 월드 X 오프셋입니다.")]
    [SerializeField]
    private float _xOffset;

    [TabGroup("Settings")]
    [Tooltip("현재 차트 캔들 중심 Y를 따라 카메라 또는 플레이 표면 루트를 함께 이동할지 여부입니다.")]
    [SerializeField]
    private bool _followChartCenterY = true;

    [TabGroup("Settings")]
    [Tooltip("차트 중심 Y 변화량을 월드 Y 이동량으로 반영할 배율입니다.")]
    [MinValue(MIN_FOLLOW_SCALE)]
    [SerializeField]
    private float _yFollowScale = DEFAULT_FOLLOW_SCALE;

    [TabGroup("Settings")]
    [Tooltip("Y Follow 결과에 추가할 월드 Y 오프셋입니다.")]
    [SerializeField]
    private float _yOffset;

    [TabGroup("Settings")]
    [Tooltip("차트 중심 Y를 따라갈 때 사용하는 보간 강도입니다. 0이면 즉시 이동합니다.")]
    [MinValue(MIN_FOLLOW_SCALE)]
    [SerializeField]
    private float _verticalFollowSharpness = DEFAULT_VERTICAL_FOLLOW_SHARPNESS;

    [TabGroup("Input Note Focus")]
    [Tooltip("입력이 포함된 판정 결과의 캔들을 화면 중심 목표로 반영할지 여부입니다.")]
    [SerializeField]
    private bool _inputNoteFollowEnabled = true;

    [TabGroup("Input Note Focus")]
    [Tooltip("입력 캔들을 화면 중심에 맞출 때 추가할 월드 X 오프셋입니다.")]
    [SerializeField]
    private float _inputNoteFocusXOffset;

    [TabGroup("Input Note Focus")]
    [Tooltip("입력 캔들을 화면 중심에 맞출 때 추가할 월드 Y 오프셋입니다.")]
    [SerializeField]
    private float _inputNoteFocusYOffset;

    [TabGroup("Input Note Focus")]
    [Tooltip("전체 이동 거리 중 빠르게 접근할 비율입니다.")]
    [Range(MIN_NORMALIZED_RATIO, MAX_NORMALIZED_RATIO)]
    [SerializeField]
    private float _inputNoteFastApproachRatio = DEFAULT_INPUT_NOTE_FAST_APPROACH_RATIO;

    [TabGroup("Input Note Focus")]
    [Tooltip("입력 캔들 목표로 이동할 때 사용하는 보간 강도입니다. 값이 클수록 초반 이동이 빨라집니다.")]
    [MinValue(MIN_FOLLOW_SCALE)]
    [SerializeField]
    private float _inputNoteApproachSharpness = DEFAULT_INPUT_NOTE_APPROACH_SHARPNESS;

    [TabGroup("Input Note Focus")]
    [Tooltip("입력 캔들 목표에 도달했다고 인정할 월드 거리입니다.")]
    [MinValue(MIN_FOLLOW_SCALE)]
    [SerializeField]
    private float _inputNoteArrivalDistance = DEFAULT_INPUT_NOTE_ARRIVAL_DISTANCE;

    [TabGroup("Input Note Focus")]
    [Tooltip("입력 캔들 목표로 이동을 시작할 때 첫 프레임 급가속을 막는 램프 시간(초)입니다.")]
    [MinValue(MIN_FOLLOW_SCALE)]
    [SerializeField]
    private float _inputNoteAccelerationDuration = DEFAULT_INPUT_NOTE_ACCELERATION_DURATION;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // GameRoot 또는 외부 초기화로 주입된 차트 러닝 서비스입니다.
    private IChartRunService _chartRunService;

    // CurrentTime 주변의 캔들 중심 Y를 제공하는 스테이지 배치 서비스입니다.
    private IChartStageService _chartStageService;

    // 새 판정 결과 발생 이벤트를 제공하는 판정 서비스입니다.
    private IChartJudgementService _chartJudgementService;

    // 차트 러닝 서비스가 연결되어 Follow 가능한지 여부입니다.
    private bool _isInitialized;

    // 현재 Follow 적용 여부입니다.
    private bool _isFollowEnabled;

    // 현재 적용 중인 입력 캔들 중심 보정 오프셋입니다.
    private Vector3 _currentJudgementFocusOffset;

    // 입력 캔들 중심으로 이동하기 위해 도달해야 하는 보정 오프셋입니다.
    private Vector3 _targetJudgementFocusOffset;

    // 입력 캔들 중심으로 이동하기 위해 참조 중인 배치 캔들입니다.
    private ChartPlacedNote _judgementFocusPlacedNote;

    // 입력 캔들 포커스가 시작될 때의 이동 거리입니다.
    private float _judgementFocusInitialDistance;

    // 입력 캔들 포커스가 시작된 뒤 흐른 시간입니다.
    private float _judgementFocusElapsed;

    // 입력 캔들 중심 보정 오프셋이 활성화되어 있는지 여부입니다.
    private bool _hasJudgementFocusOffset;

    // 판정 결과 이벤트 구독이 이미 등록되어 있는지 여부입니다.
    private bool _isInputNoteFocusEventSubscribed;

    // Follow 기준으로 삼는 초기 CurrentX입니다.
    private float _originCurrentX;

    // Follow 기준으로 삼는 초기 차트 중심 Y입니다.
    private float _originChartCenterY;

    // 기준 CurrentX를 잡을 때의 카메라 위치입니다.
    private Vector3 _originCameraPosition;

    // 기준 CurrentX를 잡을 때의 플레이 표면 루트 위치입니다.
    private Vector3 _originSurfaceRootPosition;

    // 기준 위치가 캡처되었는지 여부입니다.
    private bool _hasFollowOrigin;

    // 같은 누락 경고를 매 프레임 반복하지 않기 위한 플래그입니다.
    private bool _loggedMissingChartRun;

    // 같은 누락 경고를 매 프레임 반복하지 않기 위한 플래그입니다.
    private bool _loggedMissingChartStage;

    // 같은 누락 경고를 매 프레임 반복하지 않기 위한 플래그입니다.
    private bool _loggedMissingCamera;

    // 같은 누락 경고를 매 프레임 반복하지 않기 위한 플래그입니다.
    private bool _loggedMissingSurfaceRoot;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>차트 러닝 서비스가 연결되어 Follow 가능한지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("초기화 여부")]
    public bool IsInitialized => _isInitialized;

    /// <summary>현재 Follow 적용 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Follow 활성화")]
    public bool IsFollowEnabled => _isFollowEnabled;

    /// <summary>현재 Follow 기준 CurrentX입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("기준 CurrentX")]
    private float OriginCurrentX => _originCurrentX;

    /// <summary>현재 차트 러닝 서비스가 제공하는 CurrentX입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("현재 CurrentX")]
    private float CurrentX
    {
        get
        {
            if (_chartRunService == null)
            {
                return 0.0f;
            }

            return _chartRunService.CurrentX;
        }
    }

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>같은 오브젝트에 있는 기본 Camera 참조를 보정합니다.</summary>
    private void Awake()
    {
        TryCacheDefaultRefs();
        ClampSettings();
    }

    /// <summary>GameRoot 자동 바인딩이 켜져 있으면 차트 러닝 서비스를 가져옵니다.</summary>
    private void Start()
    {
        if (_isInitialized)
        {
            return;
        }

        TryAutoBindFromGameRoot();
    }

    /// <summary>ChartRun의 Update가 끝난 뒤 Follow 대상 위치를 보정합니다.</summary>
    private void LateUpdate()
    {
        if (_isInitialized == false && _autoBindFromGameRoot)
        {
            TryAutoBindFromGameRoot();
        }

        if (CanFollow() == false)
        {
            return;
        }

        ApplyFollow();
    }

    /// <summary>컴포넌트를 처음 붙였을 때 같은 오브젝트의 Camera를 자동 참조합니다.</summary>
    private void Reset()
    {
        TryCacheDefaultRefs();
        ClampSettings();
    }

    /// <summary>인스펙터 값이 바뀔 때 잘못된 설정값을 보정합니다.</summary>
    private void OnValidate()
    {
        TryCacheDefaultRefs();
        ClampSettings();
    }

    /// <summary>입력 노트 포커스 이벤트 구독을 정리합니다.</summary>
    private void OnDestroy()
    {
        UnsubscribeInputNoteFocusEvent();
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot 또는 외부 Presenter 구성 코드에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    public void Init(IChartRunService chartRunService)
    {
        Init(chartRunService, _chartStageService, _chartJudgementService);
    }

    /// <summary>
    /// GameRoot 또는 외부 Presenter 구성 코드에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    /// <param name="chartStageService">현재 시간대의 캔들 중심 Y를 제공하는 스테이지 서비스입니다.</param>
    public void Init(IChartRunService chartRunService, IChartStageService chartStageService)
    {
        Init(chartRunService, chartStageService, _chartJudgementService);
    }

    /// <summary>
    /// GameRoot 또는 외부 Presenter 구성 코드에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    /// <param name="chartStageService">현재 시간대의 캔들 중심 Y를 제공하는 스테이지 서비스입니다.</param>
    /// <param name="chartJudgementService">입력 판정 결과 이벤트를 제공하는 판정 서비스입니다.</param>
    public void Init(
        IChartRunService chartRunService,
        IChartStageService chartStageService,
        IChartJudgementService chartJudgementService)
    {
        if (chartRunService == null)
        {
            LogMissingChartRun();
            return;
        }

        _chartRunService = chartRunService;
        _chartStageService = chartStageService;
        SetChartJudgementService(chartJudgementService);
        _isInitialized = true;
        _isFollowEnabled = _followEnabledOnStart;
        _loggedMissingChartRun = false;
        _loggedMissingChartStage = false;

        ClampSettings();
        CaptureFollowOrigin();

        if (_isFollowEnabled)
        {
            ApplyFollow();
        }
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// Follow 적용 여부를 변경합니다.
    /// </summary>
    /// <param name="isEnabled">Follow를 적용하면 true입니다.</param>
    public void SetFollowEnabled(bool isEnabled)
    {
        _isFollowEnabled = isEnabled;

        if (_isFollowEnabled)
        {
            CaptureFollowOrigin();
            ApplyFollow();
        }
    }

    /// <summary>현재 CurrentX와 대상 위치를 새 Follow 기준점으로 다시 잡습니다.</summary>
    public void CaptureFollowOrigin()
    {
        if (_chartRunService == null)
        {
            LogMissingChartRun();
            return;
        }

        _originCurrentX = _chartRunService.CurrentX;
        _originChartCenterY = GetCurrentChartCenterY();
        _originCameraPosition = GetCameraPosition();
        _originSurfaceRootPosition = GetSurfaceRootPosition();
        _hasFollowOrigin = true;
        ClearJudgementFocusOffset();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>같은 오브젝트에 있는 기본 참조를 캐시합니다.</summary>
    private void TryCacheDefaultRefs()
    {
        if (_targetCamera == null)
        {
            _targetCamera = GetComponent<Camera>();
        }
    }

    /// <summary>설정값을 안전한 범위로 보정합니다.</summary>
    private void ClampSettings()
    {
        _followScale = GetSafeNonNegativeValue(_followScale, DEFAULT_FOLLOW_SCALE);
        _yFollowScale = GetSafeNonNegativeValue(_yFollowScale, DEFAULT_FOLLOW_SCALE);
        _verticalFollowSharpness = GetSafeNonNegativeValue(
            _verticalFollowSharpness,
            DEFAULT_VERTICAL_FOLLOW_SHARPNESS);
        _inputNoteFastApproachRatio = Mathf.Clamp(
            GetSafeNonNegativeValue(
                _inputNoteFastApproachRatio,
                DEFAULT_INPUT_NOTE_FAST_APPROACH_RATIO),
            MIN_NORMALIZED_RATIO,
            MAX_NORMALIZED_RATIO);
        _inputNoteApproachSharpness = GetSafeNonNegativeValue(
            _inputNoteApproachSharpness,
            DEFAULT_INPUT_NOTE_APPROACH_SHARPNESS);
        _inputNoteArrivalDistance = GetSafeNonNegativeValue(
            _inputNoteArrivalDistance,
            DEFAULT_INPUT_NOTE_ARRIVAL_DISTANCE);
        _inputNoteAccelerationDuration = GetSafeNonNegativeValue(
            _inputNoteAccelerationDuration,
            DEFAULT_INPUT_NOTE_ACCELERATION_DURATION);
    }

    /// <summary>GameRoot에 등록된 ChartRun 서비스를 자동으로 연결합니다.</summary>
    /// <returns>자동 바인딩에 성공하면 true입니다.</returns>
    private bool TryAutoBindFromGameRoot()
    {
        if (_autoBindFromGameRoot == false)
        {
            return false;
        }

        if (GameRoot.TryGetInstance(out GameRoot gameRoot) == false || gameRoot == null)
        {
            LogMissingChartRun();
            return false;
        }

        return TryBindChartRunService(gameRoot.ChartRun, gameRoot.ChartStage, gameRoot.ChartJudgement);
    }

    /// <summary>
    /// 전달된 ChartRun 서비스를 내부 Follow 의존성으로 연결합니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    /// <returns>바인딩에 성공하면 true입니다.</returns>
    private bool TryBindChartRunService(IChartRunService chartRunService)
    {
        return TryBindChartRunService(chartRunService, _chartStageService, _chartJudgementService);
    }

    /// <summary>
    /// 전달된 ChartRun / ChartStage 서비스를 내부 Follow 의존성으로 연결합니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    /// <param name="chartStageService">현재 시간대의 캔들 중심 Y를 제공하는 스테이지 서비스입니다.</param>
    /// <returns>바인딩에 성공하면 true입니다.</returns>
    private bool TryBindChartRunService(
        IChartRunService chartRunService,
        IChartStageService chartStageService)
    {
        return TryBindChartRunService(chartRunService, chartStageService, _chartJudgementService);
    }

    /// <summary>
    /// 전달된 ChartRun / ChartStage / ChartJudgement 서비스를 내부 Follow 의존성으로 연결합니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    /// <param name="chartStageService">현재 시간대의 캔들 중심 Y를 제공하는 스테이지 서비스입니다.</param>
    /// <param name="chartJudgementService">입력 판정 결과 이벤트를 제공하는 판정 서비스입니다.</param>
    /// <returns>바인딩에 성공하면 true입니다.</returns>
    private bool TryBindChartRunService(
        IChartRunService chartRunService,
        IChartStageService chartStageService,
        IChartJudgementService chartJudgementService)
    {
        if (chartRunService == null)
        {
            LogMissingChartRun();
            return false;
        }

        Init(chartRunService, chartStageService, chartJudgementService);
        return true;
    }

    /// <summary>판정 서비스 이벤트 구독 대상을 교체합니다.</summary>
    /// <param name="chartJudgementService">새로 구독할 판정 서비스입니다.</param>
    private void SetChartJudgementService(IChartJudgementService chartJudgementService)
    {
        if (_chartJudgementService == chartJudgementService)
        {
            SubscribeInputNoteFocusEvent();
            return;
        }

        UnsubscribeInputNoteFocusEvent();
        _chartJudgementService = chartJudgementService;
        SubscribeInputNoteFocusEvent();
    }

    /// <summary>입력 노트 포커스 요청 이벤트를 구독합니다.</summary>
    private void SubscribeInputNoteFocusEvent()
    {
        if (_chartJudgementService == null)
        {
            return;
        }

        if (_isInputNoteFocusEventSubscribed)
        {
            return;
        }

        _chartJudgementService.InputNoteFocusRequested += HandleInputNoteFocusRequested;
        _isInputNoteFocusEventSubscribed = true;
    }

    /// <summary>입력 노트 포커스 요청 이벤트 구독을 해제합니다.</summary>
    private void UnsubscribeInputNoteFocusEvent()
    {
        if (_chartJudgementService == null)
        {
            _isInputNoteFocusEventSubscribed = false;
            return;
        }

        if (_isInputNoteFocusEventSubscribed == false)
        {
            return;
        }

        _chartJudgementService.InputNoteFocusRequested -= HandleInputNoteFocusRequested;
        _isInputNoteFocusEventSubscribed = false;
    }

    /// <summary>입력 포커스 요청으로 전달된 캔들을 기준으로 카메라 보정 목표를 갱신합니다.</summary>
    /// <param name="placedNote">카메라가 포커스할 배치 캔들입니다.</param>
    private void HandleInputNoteFocusRequested(ChartPlacedNote placedNote)
    {
        if (_inputNoteFollowEnabled == false)
        {
            return;
        }

        if (placedNote == null)
        {
            return;
        }

        SetJudgementFocusTarget(placedNote);
    }

    /// <summary>입력된 캔들이 화면 중심 쪽에 오도록 Follow 보정 목표를 계산합니다.</summary>
    /// <param name="placedNote">입력 판정에 사용된 배치 캔들입니다.</param>
    private void SetJudgementFocusTarget(ChartPlacedNote placedNote)
    {
        if (_chartRunService == null)
        {
            LogMissingChartRun();
            return;
        }

        if (_hasFollowOrigin == false)
        {
            CaptureFollowOrigin();
        }

        if (_hasFollowOrigin == false)
        {
            return;
        }

        _judgementFocusPlacedNote = placedNote;
        _hasJudgementFocusOffset = true;
        _judgementFocusElapsed = 0.0f;

        float deltaX = (_chartRunService.CurrentX - _originCurrentX) * _followScale;
        float deltaY = GetChartCenterDeltaY();
        Vector3 baseTargetPosition = _followMode == ChartCameraFollowMode.FOLLOW_MODE_SURFACE_ROOT
            ? GetSurfaceRootFollowTargetPosition(deltaX, deltaY)
            : GetCameraFollowTargetPosition(deltaX, deltaY);
        UpdateJudgementFocusTargetOffset(baseTargetPosition);
        _judgementFocusInitialDistance = Mathf.Max(
            GetPlanarDistance(_currentJudgementFocusOffset, _targetJudgementFocusOffset),
            _inputNoteArrivalDistance);
    }

    /// <summary>현재 Follow를 적용할 수 있는지 확인합니다.</summary>
    /// <returns>Follow를 적용할 수 있으면 true입니다.</returns>
    private bool CanFollow()
    {
        if (_isInitialized == false)
        {
            return false;
        }

        if (_isFollowEnabled == false)
        {
            return false;
        }

        if (_chartRunService == null)
        {
            LogMissingChartRun();
            return false;
        }

        if (_hasFollowOrigin == false)
        {
            CaptureFollowOrigin();
        }

        return _hasFollowOrigin;
    }

    /// <summary>현재 Follow 모드에 맞춰 대상 위치를 갱신합니다.</summary>
    private void ApplyFollow()
    {
        float deltaX = (_chartRunService.CurrentX - _originCurrentX) * _followScale;
        float deltaY = GetChartCenterDeltaY();

        if (_followMode == ChartCameraFollowMode.FOLLOW_MODE_SURFACE_ROOT)
        {
            MoveSurfaceRoot(deltaX, deltaY);
            return;
        }

        MoveCamera(deltaX, deltaY);
    }

    /// <summary>
    /// 카메라를 CurrentX 변화량만큼 같은 방향으로 이동시킵니다.
    /// </summary>
    /// <param name="deltaX">Follow 기준점에서 이동한 차트 X 거리입니다.</param>
    private void MoveCamera(float deltaX, float deltaY)
    {
        Transform cameraTransform = GetCameraTransform();

        if (cameraTransform == null)
        {
            LogMissingCamera();
            return;
        }

        bool isApplyingJudgementFocus = CanApplyJudgementFocusOffset();
        Vector3 targetPosition = GetJudgementAdjustedTargetPosition(
            GetCameraFollowTargetPosition(deltaX, deltaY));
        Vector3 nextPosition = cameraTransform.position;
        nextPosition.x = targetPosition.x;
        nextPosition.y = isApplyingJudgementFocus
            ? targetPosition.y
            : GetFollowedYPosition(cameraTransform.position.y, targetPosition.y);
        cameraTransform.position = nextPosition;
    }

    /// <summary>
    /// 플레이 표면 루트를 CurrentX 변화량의 반대 방향으로 이동시킵니다.
    /// </summary>
    /// <param name="deltaX">Follow 기준점에서 이동한 차트 X 거리입니다.</param>
    private void MoveSurfaceRoot(float deltaX, float deltaY)
    {
        Transform surfaceRoot = GetSurfaceRootTransform();

        if (surfaceRoot == null)
        {
            LogMissingSurfaceRoot();
            return;
        }

        bool isApplyingJudgementFocus = CanApplyJudgementFocusOffset();
        Vector3 targetPosition = GetJudgementAdjustedTargetPosition(
            GetSurfaceRootFollowTargetPosition(deltaX, deltaY));
        Vector3 nextPosition = surfaceRoot.position;
        nextPosition.x = targetPosition.x;
        nextPosition.y = isApplyingJudgementFocus
            ? targetPosition.y
            : GetFollowedYPosition(surfaceRoot.position.y, targetPosition.y);
        surfaceRoot.position = nextPosition;
    }

    /// <summary>
    /// 카메라 이동 모드에서 CurrentX/Y Follow만 반영한 기본 목표 위치를 반환합니다.
    /// </summary>
    /// <param name="deltaX">Follow 기준점에서 이동한 차트 X 거리입니다.</param>
    /// <param name="deltaY">Follow 기준점에서 이동한 차트 Y 거리입니다.</param>
    /// <returns>카메라 기본 목표 위치입니다.</returns>
    private Vector3 GetCameraFollowTargetPosition(float deltaX, float deltaY)
    {
        Vector3 targetPosition = _originCameraPosition;
        targetPosition.x = _originCameraPosition.x + deltaX + _xOffset;
        targetPosition.y = _originCameraPosition.y + deltaY + _yOffset;
        return targetPosition;
    }

    /// <summary>
    /// 플레이 표면 루트 이동 모드에서 CurrentX/Y Follow만 반영한 기본 목표 위치를 반환합니다.
    /// </summary>
    /// <param name="deltaX">Follow 기준점에서 이동한 차트 X 거리입니다.</param>
    /// <param name="deltaY">Follow 기준점에서 이동한 차트 Y 거리입니다.</param>
    /// <returns>플레이 표면 루트 기본 목표 위치입니다.</returns>
    private Vector3 GetSurfaceRootFollowTargetPosition(float deltaX, float deltaY)
    {
        Vector3 targetPosition = _originSurfaceRootPosition;
        targetPosition.x = _originSurfaceRootPosition.x - deltaX + _xOffset;
        targetPosition.y = _originSurfaceRootPosition.y - deltaY + _yOffset;
        return targetPosition;
    }

    /// <summary>
    /// 입력 판정 캔들 중심으로 향하는 보정 오프셋을 기본 목표 위치에 반영합니다.
    /// </summary>
    /// <param name="baseTargetPosition">CurrentX/Y Follow만 반영한 기본 목표 위치입니다.</param>
    /// <returns>입력 판정 캔들 보정까지 반영한 목표 위치입니다.</returns>
    private Vector3 GetJudgementAdjustedTargetPosition(Vector3 baseTargetPosition)
    {
        if (CanApplyJudgementFocusOffset() == false)
        {
            return baseTargetPosition;
        }

        UpdateJudgementFocusTargetOffset(baseTargetPosition);

        _currentJudgementFocusOffset = GetFollowedJudgementOffset(
            _currentJudgementFocusOffset,
            _targetJudgementFocusOffset);

        Vector3 adjustedTargetPosition = baseTargetPosition + _currentJudgementFocusOffset;
        adjustedTargetPosition.z = baseTargetPosition.z;

        if (IsJudgementFocusArrived())
        {
            CaptureFollowOriginPreservingPosition(adjustedTargetPosition);
            ClearJudgementFocusOffset();
        }

        return adjustedTargetPosition;
    }

    /// <summary>현재 기본 Follow 목표에서 입력 캔들이 중앙에 오기 위한 목표 오프셋을 갱신합니다.</summary>
    /// <param name="baseTargetPosition">CurrentX/Y Follow만 반영한 기본 목표 위치입니다.</param>
    private void UpdateJudgementFocusTargetOffset(Vector3 baseTargetPosition)
    {
        if (_judgementFocusPlacedNote == null)
        {
            ClearJudgementFocusOffset();
            return;
        }

        Vector3 noteCenterPosition = GetPlacedNoteCenterPosition(_judgementFocusPlacedNote);
        noteCenterPosition.x += _inputNoteFocusXOffset;
        noteCenterPosition.y += _inputNoteFocusYOffset;

        Vector3 focusTargetPosition = baseTargetPosition;

        if (_followMode == ChartCameraFollowMode.FOLLOW_MODE_SURFACE_ROOT)
        {
            Transform cameraTransform = GetCameraTransform();

            if (cameraTransform == null)
            {
                LogMissingCamera();
                ClearJudgementFocusOffset();
                return;
            }

            focusTargetPosition.x = cameraTransform.position.x - noteCenterPosition.x + _originSurfaceRootPosition.x;
            focusTargetPosition.y = cameraTransform.position.y - noteCenterPosition.y + _originSurfaceRootPosition.y;
        }
        else
        {
            focusTargetPosition.x = noteCenterPosition.x;
            focusTargetPosition.y = noteCenterPosition.y;
        }

        _targetJudgementFocusOffset = focusTargetPosition - baseTargetPosition;
        _targetJudgementFocusOffset.z = 0.0f;
    }

    /// <summary>입력 판정 캔들 보정 오프셋을 현재 프레임에 적용할 수 있는지 확인합니다.</summary>
    /// <returns>보정 오프셋을 적용할 수 있으면 true입니다.</returns>
    private bool CanApplyJudgementFocusOffset()
    {
        if (_inputNoteFollowEnabled == false)
        {
            return false;
        }

        if (_hasJudgementFocusOffset == false)
        {
            return false;
        }

        return _judgementFocusPlacedNote != null;
    }

    /// <summary>
    /// 입력 판정 캔들 보정 오프셋을 목표 오프셋으로 부드럽게 보간합니다.
    /// </summary>
    /// <param name="currentOffset">현재 보정 오프셋입니다.</param>
    /// <param name="targetOffset">도달해야 하는 보정 오프셋입니다.</param>
    /// <returns>보간된 보정 오프셋입니다.</returns>
    private Vector3 GetFollowedJudgementOffset(Vector3 currentOffset, Vector3 targetOffset)
    {
        if (_inputNoteApproachSharpness <= 0.0f)
        {
            return targetOffset;
        }

        float remainingDistance = GetPlanarDistance(currentOffset, targetOffset);

        if (remainingDistance <= _inputNoteArrivalDistance)
        {
            return targetOffset;
        }

        _judgementFocusElapsed += Time.deltaTime;

        float t = GetJudgementFocusApproachT(remainingDistance);
        return Vector3.Lerp(currentOffset, targetOffset, Mathf.Clamp01(t));
    }

    /// <summary>입력 캔들 포커스의 현재 보간 계수를 반환합니다.</summary>
    /// <param name="remainingDistance">목표 오프셋까지 남은 거리입니다.</param>
    /// <returns>이번 프레임에 사용할 0-1 보간 계수입니다.</returns>
    private float GetJudgementFocusApproachT(float remainingDistance)
    {
        float sharpness = _inputNoteApproachSharpness;
        float initialDistance = Mathf.Max(_judgementFocusInitialDistance, remainingDistance);
        float slowZoneDistance = initialDistance * (1.0f - _inputNoteFastApproachRatio);

        if (slowZoneDistance > 0.0001f && remainingDistance <= slowZoneDistance)
        {
            float normalizedSlowDistance = Mathf.Clamp01(remainingDistance / slowZoneDistance);
            sharpness *= Mathf.Lerp(0.25f, 1.0f, normalizedSlowDistance);
        }

        float t = 1.0f - Mathf.Exp(-sharpness * Time.deltaTime);

        if (_inputNoteAccelerationDuration <= 0.0f)
        {
            return t;
        }

        float accelerationT = Mathf.Clamp01(_judgementFocusElapsed / _inputNoteAccelerationDuration);
        float accelerationWeight = Mathf.SmoothStep(0.0f, 1.0f, accelerationT);
        return t * accelerationWeight;
    }

    /// <summary>입력 캔들 포커스가 목표 오프셋에 도달했는지 확인합니다.</summary>
    /// <returns>도착 허용 거리 안에 들어왔으면 true입니다.</returns>
    private bool IsJudgementFocusArrived()
    {
        return GetPlanarDistance(_currentJudgementFocusOffset, _targetJudgementFocusOffset)
            <= _inputNoteArrivalDistance;
    }

    /// <summary>입력 캔들 중심 보정 상태를 초기화합니다.</summary>
    private void ClearJudgementFocusOffset()
    {
        _currentJudgementFocusOffset = Vector3.zero;
        _targetJudgementFocusOffset = Vector3.zero;
        _judgementFocusPlacedNote = null;
        _judgementFocusInitialDistance = 0.0f;
        _judgementFocusElapsed = 0.0f;
        _hasJudgementFocusOffset = false;
    }

    /// <summary>현재 위치가 유지되도록 Follow 기준점을 다시 잡습니다.</summary>
    /// <param name="currentPosition">현재 프레임에 적용할 카메라 위치입니다.</param>
    private void CaptureFollowOriginPreservingPosition(Vector3 currentPosition)
    {
        if (_chartRunService == null)
        {
            LogMissingChartRun();
            return;
        }

        _originCurrentX = _chartRunService.CurrentX;
        _originChartCenterY = GetCurrentChartCenterY();

        if (_followMode == ChartCameraFollowMode.FOLLOW_MODE_CAMERA)
        {
            _originCameraPosition = currentPosition;
            _originCameraPosition.x -= _xOffset;
            _originCameraPosition.y -= _yOffset;
        }
        else
        {
            _originSurfaceRootPosition = currentPosition;
            _originSurfaceRootPosition.x -= _xOffset;
            _originSurfaceRootPosition.y -= _yOffset;
        }

        _hasFollowOrigin = true;
    }

    /// <summary>두 위치 사이의 XY 평면 거리만 반환합니다.</summary>
    /// <param name="from">시작 위치입니다.</param>
    /// <param name="to">목표 위치입니다.</param>
    /// <returns>XY 평면 거리입니다.</returns>
    private static float GetPlanarDistance(Vector3 from, Vector3 to)
    {
        float x = to.x - from.x;
        float y = to.y - from.y;
        return Mathf.Sqrt(x * x + y * y);
    }

    /// <summary>
    /// 현재 차트 중심 Y가 Follow 기준점에서 얼마나 이동했는지 반환합니다.
    /// </summary>
    /// <returns>차트 중심 Y 변화량입니다.</returns>
    private float GetChartCenterDeltaY()
    {
        if (_followChartCenterY == false)
        {
            return 0.0f;
        }

        return (GetCurrentChartCenterY() - _originChartCenterY) * _yFollowScale;
    }

    /// <summary>
    /// 현재 Y에서 목표 Y로 보간한 위치를 반환합니다.
    /// </summary>
    /// <param name="currentY">현재 대상 Y 위치입니다.</param>
    /// <param name="targetY">이동해야 하는 목표 Y 위치입니다.</param>
    /// <returns>보간된 Y 위치입니다.</returns>
    private float GetFollowedYPosition(float currentY, float targetY)
    {
        if (_followChartCenterY == false)
        {
            return targetY;
        }

        if (_verticalFollowSharpness <= 0.0f)
        {
            return targetY;
        }

        float t = 1.0f - Mathf.Exp(-_verticalFollowSharpness * Time.deltaTime);
        return Mathf.Lerp(currentY, targetY, Mathf.Clamp01(t));
    }

    /// <summary>
    /// 현재 ChartRun 시간에 해당하는 캔들 중심 Y를 반환합니다.
    /// </summary>
    /// <returns>현재 차트 중심 Y입니다.</returns>
    private float GetCurrentChartCenterY()
    {
        if (_followChartCenterY == false)
        {
            return _originChartCenterY;
        }

        if (_chartStageService == null)
        {
            LogMissingChartStage();
            return _originChartCenterY;
        }

        IReadOnlyList<ChartPlacedNote> placedNotes = _chartStageService.PlacedNotes;

        if (placedNotes == null || placedNotes.Count <= 0)
        {
            return _originChartCenterY;
        }

        float currentTime = _chartRunService == null ? 0.0f : _chartRunService.CurrentTime;
        return GetChartCenterYAtTime(placedNotes, currentTime);
    }

    /// <summary>
    /// 지정 시간 주변의 두 캔들 중심 Y를 보간합니다.
    /// </summary>
    /// <param name="placedNotes">배치된 캔들 목록입니다.</param>
    /// <param name="time">조회할 스테이지 시간입니다.</param>
    /// <returns>보간된 차트 중심 Y입니다.</returns>
    private static float GetChartCenterYAtTime(IReadOnlyList<ChartPlacedNote> placedNotes, float time)
    {
        ChartPlacedNote previousNote = null;

        for (int i = 0; i < placedNotes.Count; i++)
        {
            ChartPlacedNote nextNote = placedNotes[i];

            if (nextNote == null)
            {
                continue;
            }

            if (time <= nextNote.Time)
            {
                if (previousNote == null)
                {
                    return GetPlacedNoteCenterY(nextNote);
                }

                float timeRange = Mathf.Max(nextNote.Time - previousNote.Time, 0.0001f);
                float t = Mathf.Clamp01((time - previousNote.Time) / timeRange);
                return Mathf.Lerp(
                    GetPlacedNoteCenterY(previousNote),
                    GetPlacedNoteCenterY(nextNote),
                    t);
            }

            previousNote = nextNote;
        }

        if (previousNote == null)
        {
            return 0.0f;
        }

        return GetPlacedNoteCenterY(previousNote);
    }

    /// <summary>
    /// 한 캔들의 화면 중심으로 사용할 Y 값을 반환합니다.
    /// </summary>
    /// <param name="placedNote">조회할 배치 캔들입니다.</param>
    /// <returns>캔들 중심 Y입니다.</returns>
    private static float GetPlacedNoteCenterY(ChartPlacedNote placedNote)
    {
        if (placedNote == null)
        {
            return 0.0f;
        }

        if (placedNote.HasCustomCandleShape)
        {
            return (placedNote.HighWorldY + placedNote.LowWorldY) * 0.5f;
        }

        return placedNote.WorldPosition.y;
    }

    /// <summary>
    /// 한 캔들의 화면 중심으로 사용할 월드 위치를 반환합니다.
    /// </summary>
    /// <param name="placedNote">조회할 배치 캔들입니다.</param>
    /// <returns>캔들 중심 월드 위치입니다.</returns>
    private static Vector3 GetPlacedNoteCenterPosition(ChartPlacedNote placedNote)
    {
        if (placedNote == null)
        {
            return Vector3.zero;
        }

        Vector3 centerPosition = placedNote.WorldPosition;
        centerPosition.y = GetPlacedNoteCenterY(placedNote);
        return centerPosition;
    }

    /// <summary>대상 카메라 Transform을 가져옵니다.</summary>
    /// <returns>사용 가능한 카메라 Transform입니다.</returns>
    private Transform GetCameraTransform()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }

        if (_targetCamera == null)
        {
            return null;
        }

        return _targetCamera.transform;
    }

    /// <summary>플레이 표면 루트 Transform을 가져옵니다.</summary>
    /// <returns>사용 가능한 플레이 표면 루트 Transform입니다.</returns>
    private Transform GetSurfaceRootTransform()
    {
        if (_surfaceRootOverride != null)
        {
            return _surfaceRootOverride;
        }

        if (_playSurfaceView == null)
        {
            return null;
        }

        return _playSurfaceView.SurfaceTransform;
    }

    /// <summary>현재 카메라 위치를 null 안전하게 가져옵니다.</summary>
    /// <returns>카메라 위치입니다.</returns>
    private Vector3 GetCameraPosition()
    {
        Transform cameraTransform = GetCameraTransform();

        if (cameraTransform == null)
        {
            return Vector3.zero;
        }

        return cameraTransform.position;
    }

    /// <summary>현재 플레이 표면 루트 위치를 null 안전하게 가져옵니다.</summary>
    /// <returns>플레이 표면 루트 위치입니다.</returns>
    private Vector3 GetSurfaceRootPosition()
    {
        Transform surfaceRoot = GetSurfaceRootTransform();

        if (surfaceRoot == null)
        {
            return Vector3.zero;
        }

        return surfaceRoot.position;
    }

    /// <summary>
    /// 0 이상이어야 하는 설정값을 안전하게 보정합니다.
    /// </summary>
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

        return Mathf.Max(value, MIN_FOLLOW_SCALE);
    }

    /// <summary>ChartRun 서비스 누락 경고를 한 번만 출력합니다.</summary>
    private void LogMissingChartRun()
    {
        if (_loggedMissingChartRun)
        {
            return;
        }

        _loggedMissingChartRun = true;
        LogWarning("ChartRun 서비스가 없어 CurrentX Follow를 적용할 수 없습니다.");
    }

    /// <summary>카메라 누락 경고를 한 번만 출력합니다.</summary>
    private void LogMissingCamera()
    {
        if (_loggedMissingCamera)
        {
            return;
        }

        _loggedMissingCamera = true;
        LogWarning("이동할 카메라가 연결되지 않았습니다.");
    }

    /// <summary>ChartStage 서비스 누락 경고를 한 번만 출력합니다.</summary>
    private void LogMissingChartStage()
    {
        if (_loggedMissingChartStage)
        {
            return;
        }

        _loggedMissingChartStage = true;
        LogWarning("ChartStage 서비스가 없어 차트 중심 Y Follow를 적용할 수 없습니다.");
    }

    /// <summary>플레이 표면 루트 누락 경고를 한 번만 출력합니다.</summary>
    private void LogMissingSurfaceRoot()
    {
        if (_loggedMissingSurfaceRoot)
        {
            return;
        }

        _loggedMissingSurfaceRoot = true;
        LogWarning("이동할 플레이 표면 루트가 연결되지 않았습니다.");
    }

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartCameraFollowPresenter] {message}");
    }
}
