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

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // GameRoot 또는 외부 초기화로 주입된 차트 러닝 서비스입니다.
    private IChartRunService _chartRunService;

    // 차트 러닝 서비스가 연결되어 Follow 가능한지 여부입니다.
    private bool _isInitialized;

    // 현재 Follow 적용 여부입니다.
    private bool _isFollowEnabled;

    // Follow 기준으로 삼는 초기 CurrentX입니다.
    private float _originCurrentX;

    // 기준 CurrentX를 잡을 때의 카메라 위치입니다.
    private Vector3 _originCameraPosition;

    // 기준 CurrentX를 잡을 때의 플레이 표면 루트 위치입니다.
    private Vector3 _originSurfaceRootPosition;

    // 기준 위치가 캡처되었는지 여부입니다.
    private bool _hasFollowOrigin;

    // 같은 누락 경고를 매 프레임 반복하지 않기 위한 플래그입니다.
    private bool _loggedMissingChartRun;

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

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// GameRoot 또는 외부 Presenter 구성 코드에서 호출하는 초기화 함수입니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    public void Init(IChartRunService chartRunService)
    {
        if (chartRunService == null)
        {
            LogMissingChartRun();
            return;
        }

        _chartRunService = chartRunService;
        _isInitialized = true;
        _isFollowEnabled = _followEnabledOnStart;
        _loggedMissingChartRun = false;

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
        _originCameraPosition = GetCameraPosition();
        _originSurfaceRootPosition = GetSurfaceRootPosition();
        _hasFollowOrigin = true;
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

        return TryBindChartRunService(gameRoot.ChartRun);
    }

    /// <summary>
    /// 전달된 ChartRun 서비스를 내부 Follow 의존성으로 연결합니다.
    /// </summary>
    /// <param name="chartRunService">CurrentX를 제공하는 차트 러닝 서비스입니다.</param>
    /// <returns>바인딩에 성공하면 true입니다.</returns>
    private bool TryBindChartRunService(IChartRunService chartRunService)
    {
        if (chartRunService == null)
        {
            LogMissingChartRun();
            return false;
        }

        Init(chartRunService);
        return true;
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

        if (_followMode == ChartCameraFollowMode.FOLLOW_MODE_SURFACE_ROOT)
        {
            MoveSurfaceRoot(deltaX);
            return;
        }

        MoveCamera(deltaX);
    }

    /// <summary>
    /// 카메라를 CurrentX 변화량만큼 같은 방향으로 이동시킵니다.
    /// </summary>
    /// <param name="deltaX">Follow 기준점에서 이동한 차트 X 거리입니다.</param>
    private void MoveCamera(float deltaX)
    {
        Transform cameraTransform = GetCameraTransform();

        if (cameraTransform == null)
        {
            LogMissingCamera();
            return;
        }

        Vector3 nextPosition = cameraTransform.position;
        nextPosition.x = _originCameraPosition.x + deltaX + _xOffset;
        cameraTransform.position = nextPosition;
    }

    /// <summary>
    /// 플레이 표면 루트를 CurrentX 변화량의 반대 방향으로 이동시킵니다.
    /// </summary>
    /// <param name="deltaX">Follow 기준점에서 이동한 차트 X 거리입니다.</param>
    private void MoveSurfaceRoot(float deltaX)
    {
        Transform surfaceRoot = GetSurfaceRootTransform();

        if (surfaceRoot == null)
        {
            LogMissingSurfaceRoot();
            return;
        }

        Vector3 nextPosition = surfaceRoot.position;
        nextPosition.x = _originSurfaceRootPosition.x - deltaX + _xOffset;
        surfaceRoot.position = nextPosition;
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
