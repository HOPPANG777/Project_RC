using System;
using System.Threading;
using UnityEngine;

//==============================================================================
// 파일: Singleton.cs
// 설명: 프로젝트 내 유니티 MonoBehaviour 기반 싱글톤 컴포넌트의 베이스 클래스
//==============================================================================

/// <summary>
/// 싱글톤 타입별 작동 정책을 선언적으로 지정하는 어트리뷰트입니다.
/// </summary>
/// <remarks>
/// static 생성자에서 정책을 설정하면, 상속된 정적 멤버(Instance 등)를
/// 파생 타입 이름으로 접근할 때 파생 타입의 static 생성자가 실행된다는 보장이 없어
/// 정책이 누락될 수 있습니다. 따라서 정책은 본 어트리뷰트로 선언합니다.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class SingletonConfigAttribute : Attribute
{
    /// <summary>인스턴스가 없을 때 런타임 자동 생성을 허용할지 여부입니다. (기본값: false)</summary>
    public bool AutoCreateIfNotFound { get; set; } = false;

    /// <summary>자동 생성 시 씬에 배치될 GameObject의 이름입니다. (미지정 시 기본 규칙 사용)</summary>
    public string AutoCreatedObjectName { get; set; }

    /// <summary>씬이 변경되어도 파괴되지 않고 유지(DontDestroyOnLoad)할지 여부입니다. (기본값: true)</summary>
    public bool PersistAcrossScenes { get; set; } = true;
}

/// <summary>
/// 런타임 세션 단위의 전역 상태(세션 ID, 메인 스레드 ID)를 관리하는 내부 헬퍼입니다.
/// </summary>
/// <remarks>
/// "Enter Play Mode → Reload Domain 끄기" 환경에서는 static 필드가 플레이 세션 사이에
/// 초기화되지 않습니다. 본 헬퍼가 매 플레이 진입 시 세션 ID를 증가시키고,
/// 각 싱글톤은 이를 비교해 잔존 정적 상태를 지연 리셋합니다.
/// </remarks>
internal static class SingletonRuntime
{
    /// <summary>플레이 세션마다 증가하는 식별자입니다.</summary>
    public static int SessionId { get; private set; }

    /// <summary>유니티 메인 스레드의 관리 스레드 ID입니다. (0이면 미확정)</summary>
    public static int MainThreadId { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OnRuntimeInitialize()
    {
        SessionId++;
        MainThreadId = Thread.CurrentThread.ManagedThreadId;
    }
}

/// <summary>
/// MonoBehaviour 기반의 제네릭 싱글톤 베이스 클래스입니다.
/// </summary>
/// <remarks>
/// ⚠️ [코딩 컨벤션 1.3] 프로젝트 전역 싱글톤은 오직 'GameRoot' 하나만 두는 것이 원칙입니다.
/// 특정 컨트롤러(도메인 관리) 클래스들은 본 클래스를 상속받지 말고, GameRoot의 멤버 필드로 등록하여 사용하세요.
/// 정책이 필요한 경우 파생 클래스에 [SingletonConfig(...)] 어트리뷰트를 부착합니다.
/// 모든 정적 접근(Instance/TryGetInstance 등)은 메인 스레드에서만 호출해야 합니다.
/// </remarks>
/// <typeparam name="T">MonoBehaviour를 상속받은 싱글톤 타입</typeparam>
[DisallowMultipleComponent]
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    //==============================================================================
    // 내부 상태 (Static Fields)
    //==============================================================================

    /// <summary>싱글톤 인스턴스의 static 참조 필드입니다.</summary>
    private static T _instance;

    /// <summary>애플리케이션이 종료(Quit) 중인지 여부를 나타내는 플래그입니다.</summary>
    private static bool _isQuitting;

    /// <summary>씬 내에서 기존 인스턴스 검색을 실행했는지 여부입니다.</summary>
    private static bool _searchedInScene;

    /// <summary>이 타입이 마지막으로 동기화한 세션 ID입니다. (-1이면 미동기화)</summary>
    private static int _syncedSessionId = -1;

    //--- 정책(어트리뷰트로부터 1회 해석되어 캐시됨) ---

    /// <summary>정책이 이미 해석되었는지 여부입니다.</summary>
    private static bool _policyResolved;

    /// <summary>인스턴스가 없을 때 자동 생성(Create On Demand)을 허용할지 여부입니다.</summary>
    private static bool _autoCreateIfNotFound;

    /// <summary>씬이 변경되어도 파괴되지 않고 유지(DontDestroyOnLoad)할지 여부입니다.</summary>
    private static bool _persistAcrossScenes = true;

    /// <summary>자동 생성 시 씬에 배치될 GameObject의 이름입니다.</summary>
    private static string _autoCreatedObjectName;

    //==============================================================================
    // 정책 해석 (Policy Resolution)
    //==============================================================================

    /// <summary>인스턴스가 없을 때 자동 생성을 허용할지 여부입니다.</summary>
    private static bool AutoCreateIfNotFound { get { EnsurePolicyResolved(); return _autoCreateIfNotFound; } }

    /// <summary>DontDestroyOnLoad 적용 여부입니다.</summary>
    private static bool PersistAcrossScenes { get { EnsurePolicyResolved(); return _persistAcrossScenes; } }

    /// <summary>자동 생성 시 사용할 오브젝트 이름입니다.</summary>
    private static string AutoCreatedObjectName { get { EnsurePolicyResolved(); return _autoCreatedObjectName; } }

    /// <summary>
    /// [SingletonConfig] 어트리뷰트로부터 타입별 정책을 1회 해석해 캐시합니다.
    /// </summary>
    private static void EnsurePolicyResolved()
    {
        if (_policyResolved)
        {
            return;
        }
        _policyResolved = true;

        var config = (SingletonConfigAttribute)Attribute.GetCustomAttribute(
            typeof(T), typeof(SingletonConfigAttribute), inherit: true);

        if (config != null)
        {
            _autoCreateIfNotFound = config.AutoCreateIfNotFound;
            _persistAcrossScenes = config.PersistAcrossScenes;
            _autoCreatedObjectName = config.AutoCreatedObjectName;
        }
        else
        {
            // 컨벤션상 기본은 '자동 생성 금지'이며, 씬/프리팹에 미리 배치되어야 합니다.
            _autoCreateIfNotFound = false;
            _persistAcrossScenes = true;
        }

        if (string.IsNullOrWhiteSpace(_autoCreatedObjectName))
        {
            _autoCreatedObjectName = $"[Singleton] {typeof(T).Name}";
        }
    }

    //==============================================================================
    // 세션 동기화 & 스레드 가드
    //==============================================================================

    /// <summary>
    /// 새 플레이 세션이 시작되었으면 잔존 정적 상태를 리셋합니다.
    /// (도메인 리로드를 끈 환경에서 static 필드가 살아남는 문제를 방지)
    /// </summary>
    private static void SyncSession()
    {
        int currentSession = SingletonRuntime.SessionId;
        if (_syncedSessionId == currentSession)
        {
            return;
        }

        _syncedSessionId = currentSession;
        _instance = null;
        _isQuitting = false;
        _searchedInScene = false;
        // 정책(_policyResolved 등)은 타입 불변이므로 리셋하지 않습니다.
    }

    /// <summary>
    /// 메인 스레드가 아닌 곳에서의 접근을 개발 환경에서 경고합니다. (유니티 API는 메인 스레드 전용)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void WarnIfOffMainThread(string apiName)
    {
        int mainThreadId = SingletonRuntime.MainThreadId;
        if (mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId != mainThreadId)
        {
            LogWarning($"{apiName}는 메인 스레드에서만 호출해야 합니다. (현재 스레드 ID: {Thread.CurrentThread.ManagedThreadId})");
        }
    }

    //==============================================================================
    // 외부 접근 API (Properties & Methods)
    //==============================================================================

    /// <summary>
    /// 싱글톤 고유 인스턴스에 접근하는 프로퍼티입니다. (캡슐화 및 정책 적용)
    /// </summary>
    public static T Instance
    {
        get
        {
            SyncSession();
            WarnIfOffMainThread(nameof(Instance));

            // 종료 프로세스 진행 중에는 메모리 릭 및 유령 오브젝트 생성을 방지하기 위해 생성을 무시합니다.
            if (_isQuitting)
            {
                Log($"Instance 요청 무시 (애플리케이션 종료 중): {typeof(T).Name}");
                return null;
            }

            if (_instance != null)
            {
                return _instance;
            }

            // 1) 씬에 이미 배치되어 있는 기존 컴포넌트를 최초 1회 탐색합니다.
            if (!_searchedInScene)
            {
                _searchedInScene = true;

                T found = FindInScene();
                if (found != null)
                {
                    Log($"씬에서 기존 인스턴스 발견: {GetHierarchyPath(found.gameObject)}");
                    return AdoptSceneInstance(found);
                }
            }

            // 2) 씬에 존재하지 않을 경우, 타입별 정책에 따라 자동 생성 여부를 결정합니다.
            if (!AutoCreateIfNotFound)
            {
                LogWarning($"Instance 요청 실패 (자동 생성 제한 타입): {typeof(T).Name} - 프리팹 또는 씬에 미리 배치되어야 합니다.");
                return null;
            }

            return CreateAutoInstance();
        }
    }

    /// <summary>
    /// 현재 싱글톤 인스턴스가 런타임 상에 메모리 할당 및 유효하게 존재하는지 여부만 빠르게 확인합니다.
    /// </summary>
    public static bool HasInstance
    {
        get
        {
            SyncSession();
            return _instance != null && !_isQuitting;
        }
    }

    /// <summary>
    /// 인스턴스를 안전하게 가져오려고 시도하며, 자동 생성을 유발하지 않고 유효성 여부를 반환합니다.
    /// </summary>
    /// <param name="value">출력될 싱글톤 인스턴스 참조</param>
    /// <returns>인스턴스 존재 여부 (True / False)</returns>
    public static bool TryGetInstance(out T value)
    {
        SyncSession();
        WarnIfOffMainThread(nameof(TryGetInstance));

        if (_isQuitting)
        {
            value = null;
            return false;
        }

        if (_instance != null)
        {
            value = _instance;
            return true;
        }

        if (!_searchedInScene)
        {
            _searchedInScene = true;

            T found = FindInScene();
            if (found != null)
            {
                // Instance 게터와 동일하게 DontDestroyOnLoad 정책을 일관되게 적용합니다.
                AdoptSceneInstance(found);
            }
        }

        value = _instance;
        return value != null;
    }

    //==============================================================================
    // 유니티 생명주기 (Unity Lifecycle)
    //==============================================================================

    /// <summary>
    /// 객체 생성 시 싱글톤 유일성을 검증하고 등록 및 중복 제거를 처리합니다.
    /// </summary>
    protected virtual void Awake()
    {
        // Awake는 종료 시점에는 호출되지 않으므로, 여기서 세션을 동기화하면
        // 잔존한 _isQuitting/_instance 등을 안전하게 리셋할 수 있습니다.
        SyncSession();

        if (_instance == null)
        {
            _instance = this as T;
            _searchedInScene = true;

            if (PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            Log($"싱글톤 등록 완료: {GetHierarchyPath(gameObject)}");
        }
        else if (_instance != this)
        {
            LogWarning($"중복 싱글톤 감지 및 예외 파괴 완료: {GetHierarchyPath(gameObject)} / 원본 인스턴스: {GetHierarchyPath(_instance.gameObject)}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 유니티 애플리케이션 종료 시점에 런타임 유령 오브젝트가 생성되는 부작용을 방지합니다.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    /// <summary>
    /// 객체 파괴 시 싱글톤 인스턴스의 static 참조를 초기화하여 메모리를 해제합니다.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            Log($"싱글톤 참조 해제(파괴): {GetHierarchyPath(gameObject)}");
            _instance = null;
            // 동일 세션 내 재탐색을 허용해, 파괴 후 다른 인스턴스가 있으면 다시 찾을 수 있게 합니다.
            _searchedInScene = false;
        }
    }

    //==============================================================================
    // 내부 유틸리티 로직 (Private Utilities)
    //==============================================================================

    /// <summary>
    /// 씬에서 타입 T의 컴포넌트를 탐색합니다. (유니티 버전별 API 분기)
    /// </summary>
    private static T FindInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }

    /// <summary>
    /// 씬에서 발견된 인스턴스를 정식 싱글톤으로 채택하고 유지 정책을 적용합니다.
    /// </summary>
    private static T AdoptSceneInstance(T found)
    {
        _instance = found;
        _searchedInScene = true;

        if (PersistAcrossScenes && Application.isPlaying)
        {
            DontDestroyOnLoad(found.gameObject);
        }

        return found;
    }

    /// <summary>
    /// 씬 내에 인스턴스가 없을 때 런타임 상에서 동적으로 고유 게임오브젝트와 컴포넌트를 생성합니다.
    /// </summary>
    /// <returns>생성된 싱글톤 컴포넌트 인스턴스</returns>
    private static T CreateAutoInstance()
    {
        if (!Application.isPlaying)
        {
            LogWarning($"자동 생성 거부 (플레이 모드 아님): {typeof(T).Name}");
            return null;
        }

        GameObject autoGameObject = new GameObject(AutoCreatedObjectName);
        // AddComponent는 Awake를 동기 호출하므로, 이 시점에 _instance가 설정되고 유지 정책이 적용됩니다.
        T createdComponent = autoGameObject.AddComponent<T>();

        Log($"런타임 동적 자동 생성 완료: {GetHierarchyPath(autoGameObject)}");
        return createdComponent;
    }

    /// <summary>
    /// 디버깅 가독성을 향상시키기 위해 해당 게임오브젝트의 하이러키 전체 경로 문자열을 빌드합니다.
    /// </summary>
    /// <param name="go">조회할 게임오브젝트</param>
    /// <returns>하이라키 부모-자식 경로 구조 문자열</returns>
    private static string GetHierarchyPath(GameObject go)
    {
        if (go == null)
        {
            return "(null)";
        }

        Transform currentTransform = go.transform;
        string hierarchyPath = currentTransform.name;

        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
            hierarchyPath = $"{currentTransform.name}/{hierarchyPath}";
        }

        return hierarchyPath;
    }

    /// <summary>
    /// 에디터 혹은 개발 빌드 환경에서만 조건부로 출력되는 싱글톤 내부 전용 안전 로그 함수입니다.
    /// </summary>
    /// <param name="message">출력할 로그 내용</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void Log(string message)
    {
        Debug.Log($"[{typeof(T).Name}] {message}");
    }

    /// <summary>
    /// 에디터 혹은 개발 빌드 환경에서만 조건부로 출력되는 싱글톤 내부 전용 예외 경고 로그 함수입니다.
    /// </summary>
    /// <param name="message">출력할 경고 내용</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[{typeof(T).Name}] {message}");
    }
}
