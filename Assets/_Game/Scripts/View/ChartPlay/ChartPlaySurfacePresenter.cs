using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartPlaySurfacePresenter
// 설명: 차트 스테이지 배치 노트를 읽어 Gameplay 플레이 표면 View 표시 흐름을 제어하는 Presenter
//==============================================================================
// 세부 구현 사항
// - IChartStageService.PlacedNotes를 읽어 ChartNoteView 생성 흐름을 제어합니다.
// - ChartPlacedNote.WorldPosition을 기준으로 노트 View 위치를 배치하고 갱신합니다.
// - 판정 완료된 노트는 View의 판정 상태로 흐리게 표시하거나 설정에 따라 숨깁니다.
// - 입력 판정, 점수 계산, 스테이지 데이터 생성은 Controller 책임으로 남깁니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartPlaySurfacePresenter : MonoBehaviour
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 스테이지 캐시가 없는 상태를 표시하기 위한 노트 수입니다.
    private const int EMPTY_STAGE_NOTE_COUNT = -1;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Refs")]
    [Tooltip("캔들 노트 View 생성과 판정선 표시를 담당하는 플레이 표면 View입니다.")]
    [Required]
    [SerializeField]
    private ChartPlaySurfaceView _surfaceView;

    [TabGroup("Settings")]
    [Tooltip("Start 시 GameRoot의 ChartStage 서비스로 자동 초기화할지 여부입니다.")]
    [SerializeField]
    private bool _autoInitializeFromGameRoot = true;

    [TabGroup("Settings")]
    [Tooltip("초기화가 끝난 뒤 플레이 표면을 표시할지 여부입니다.")]
    [SerializeField]
    private bool _showSurfaceOnInit = true;

    [TabGroup("Settings")]
    [Tooltip("OnEnable 시 스테이지 노트 View를 다시 동기화할지 여부입니다.")]
    [SerializeField]
    private bool _syncOnEnable = true;

    [TabGroup("Settings")]
    [Tooltip("스테이지가 없을 때 기존 노트 View를 제거할지 여부입니다.")]
    [SerializeField]
    private bool _clearViewsWhenStageMissing = true;

    [TabGroup("Settings")]
    [Tooltip("판정 완료된 노트를 숨길지 여부입니다. 꺼두면 ChartNoteView의 판정 완료 투명도로 흐리게 표시합니다.")]
    [SerializeField]
    private bool _hideJudgedNotes;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 런타임 배치 노트 캐시를 제공하는 스테이지 서비스입니다.
    private IChartStageService _chartStageService;

    // Init 또는 자동 초기화가 완료되었는지 여부입니다.
    private bool _isInitialized;

    // 마지막으로 노트 View를 만든 스테이지 데이터입니다.
    private ChartStageDataSO _cachedStageData;

    // 마지막으로 노트 View를 만든 배치 노트 수입니다.
    private int _cachedPlacedNoteCount = EMPTY_STAGE_NOTE_COUNT;

    // 마지막으로 노트 View를 만든 배치 노트 참조 해시입니다.
    private int _cachedPlacedNoteHash;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>Presenter가 초기화되었는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("초기화 여부")]
    public bool IsInitialized => _isInitialized;

    /// <summary>현재 표시 중인 노트 View 수입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("표시 노트 수")]
    public int PresentedNoteCount
    {
        get
        {
            if (_surfaceView == null)
            {
                return 0;
            }

            return _surfaceView.NoteViewCount;
        }
    }

    /// <summary>현재 연결된 스테이지 서비스가 있는지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("스테이지 서비스 연결")]
    private bool HasStageService => _chartStageService != null;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>기본 View 참조를 보정합니다.</summary>
    private void Awake()
    {
        TryCacheDefaultRefs();
    }

    /// <summary>GameRoot가 직접 주입하지 않은 경우 공개 서비스 경로로 자동 초기화합니다.</summary>
    private void Start()
    {
        TryAutoInitializeFromGameRoot();
    }

    /// <summary>오브젝트가 다시 활성화될 때 현재 스테이지 표시 상태를 맞춥니다.</summary>
    private void OnEnable()
    {
        if (_syncOnEnable == false)
        {
            return;
        }

        if (_isInitialized == false)
        {
            return;
        }

        SyncSurfaceWithStage(true, false);
    }

    /// <summary>프레임마다 스테이지 캐시와 판정 표시 상태를 View에 반영합니다.</summary>
    private void Update()
    {
        if (_isInitialized == false)
        {
            return;
        }

        SyncSurfaceWithStage(false, false);
    }

    /// <summary>컴포넌트를 처음 붙였을 때 같은 오브젝트나 자식의 플레이 표면 View를 자동 참조합니다.</summary>
    private void Reset()
    {
        TryCacheDefaultRefs();
    }

    /// <summary>인스펙터 값이 바뀔 때 기본 View 참조를 보정합니다.</summary>
    private void OnValidate()
    {
        TryCacheDefaultRefs();
    }

    //==============================================================================
    // 초기화
    //==============================================================================

    /// <summary>
    /// 플레이 표면 Presenter에 스테이지 서비스를 주입합니다.
    /// </summary>
    /// <param name="chartStageService">런타임 배치 노트 캐시를 제공하는 스테이지 서비스입니다.</param>
    public void Init(IChartStageService chartStageService)
    {
        if (chartStageService == null)
        {
            LogWarning("초기화할 ChartStage 서비스가 null입니다.");
            return;
        }

        TryCacheDefaultRefs();

        _chartStageService = chartStageService;
        _isInitialized = true;

        if (_showSurfaceOnInit && _surfaceView != null)
        {
            _surfaceView.Show();
        }

        SyncSurfaceWithStage(true, true);
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>현재 스테이지 배치 캐시를 기준으로 노트 View를 강제로 다시 생성합니다.</summary>
    public void RebuildPresentedNotes()
    {
        SyncSurfaceWithStage(true, true);
    }

    /// <summary>현재 연결된 노트 View들의 위치와 판정 표시 상태를 즉시 갱신합니다.</summary>
    public void RefreshPresentedNotes()
    {
        if (CanSyncSurface(true) == false)
        {
            return;
        }

        RefreshPresentedNoteViews();
    }

    /// <summary>현재 생성되어 있는 노트 View를 모두 제거합니다.</summary>
    public void ClearPresentedNotes()
    {
        if (_surfaceView == null)
        {
            return;
        }

        _surfaceView.ClearNoteViews();
        ClearCachedStageSnapshot();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>비어 있는 기본 View 참조를 안전한 기본값으로 보정합니다.</summary>
    private void TryCacheDefaultRefs()
    {
        if (_surfaceView != null)
        {
            return;
        }

        _surfaceView = GetComponent<ChartPlaySurfaceView>();

        if (_surfaceView != null)
        {
            return;
        }

        _surfaceView = GetComponentInChildren<ChartPlaySurfaceView>(true);
    }

    /// <summary>GameRoot의 공개 서비스 경로를 통해 자동 초기화를 시도합니다.</summary>
    private void TryAutoInitializeFromGameRoot()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_autoInitializeFromGameRoot == false)
        {
            return;
        }

        if (GameRoot.TryGetInstance(out GameRoot gameRoot) == false || gameRoot == null)
        {
            LogWarning("GameRoot 인스턴스를 찾지 못해 자동 초기화를 건너뜁니다.");
            return;
        }

        IChartStageService chartStageService = gameRoot.ChartStage;
        if (chartStageService == null)
        {
            LogWarning("GameRoot의 ChartStage 서비스가 null이라 자동 초기화를 건너뜁니다.");
            return;
        }

        Init(chartStageService);
    }

    /// <summary>
    /// 현재 스테이지 캐시와 플레이 표면 View를 동기화합니다.
    /// </summary>
    /// <param name="forceRebuild">강제로 노트 View를 다시 만들면 true입니다.</param>
    /// <param name="shouldLog">누락된 참조를 경고 로그로 남기면 true입니다.</param>
    private void SyncSurfaceWithStage(bool forceRebuild, bool shouldLog)
    {
        if (CanSyncSurface(shouldLog) == false)
        {
            return;
        }

        if (_chartStageService.IsInitialized == false)
        {
            return;
        }

        if (_chartStageService.HasStage == false)
        {
            HandleMissingStage();
            return;
        }

        if (forceRebuild || HasStageSnapshotChanged())
        {
            RebuildNoteViewsFromStage();
        }

        RefreshPresentedNoteViews();
    }

    /// <summary>
    /// 동기화에 필요한 참조가 준비되었는지 확인합니다.
    /// </summary>
    /// <param name="shouldLog">누락된 참조를 경고 로그로 남기면 true입니다.</param>
    /// <returns>동기화 가능한 상태이면 true입니다.</returns>
    private bool CanSyncSurface(bool shouldLog)
    {
        if (_surfaceView == null)
        {
            if (shouldLog)
            {
                LogWarning("ChartPlaySurfaceView가 연결되지 않았습니다.");
            }

            return false;
        }

        if (_chartStageService == null)
        {
            if (shouldLog)
            {
                LogWarning("ChartStage 서비스가 주입되지 않았습니다.");
            }

            return false;
        }

        return true;
    }

    /// <summary>스테이지가 사라졌을 때 기존 표시 캐시를 정리합니다.</summary>
    private void HandleMissingStage()
    {
        if (_clearViewsWhenStageMissing == false)
        {
            return;
        }

        if (_surfaceView == null || _surfaceView.NoteViewCount <= 0)
        {
            ClearCachedStageSnapshot();
            return;
        }

        ClearPresentedNotes();
    }

    /// <summary>IChartStageService.PlacedNotes를 읽어 노트 View를 다시 생성합니다.</summary>
    private void RebuildNoteViewsFromStage()
    {
        if (_surfaceView == null || _chartStageService == null)
        {
            return;
        }

        _surfaceView.ClearNoteViews();

        IReadOnlyList<ChartPlacedNote> placedNotes = _chartStageService.PlacedNotes;

        if (placedNotes == null || placedNotes.Count <= 0)
        {
            CacheStageSnapshot(placedNotes);
            return;
        }

        for (int i = 0; i < placedNotes.Count; i++)
        {
            ChartPlacedNote placedNote = placedNotes[i];

            if (placedNote == null)
            {
                LogWarning($"배치 노트 {i}번이 null이라 View 생성을 건너뜁니다.");
                continue;
            }

            if (_surfaceView.TryCreateNoteView(placedNote, out IChartNoteView noteView) == false)
            {
                continue;
            }

            RefreshNoteView(noteView, placedNote);
        }

        CacheStageSnapshot(placedNotes);
    }

    /// <summary>생성된 노트 View들의 위치와 판정 표시 상태를 갱신합니다.</summary>
    private void RefreshPresentedNoteViews()
    {
        if (_surfaceView == null)
        {
            return;
        }

        for (int i = 0; i < _surfaceView.NoteViewCount; i++)
        {
            if (_surfaceView.TryGetNoteView(i, out IChartNoteView noteView) == false)
            {
                continue;
            }

            if (noteView == null || noteView.HasBoundNote == false)
            {
                continue;
            }

            RefreshNoteView(noteView, noteView.BoundNote);
        }
    }

    /// <summary>
    /// 노트 View 하나에 런타임 배치 노트의 최신 표시 상태를 적용합니다.
    /// </summary>
    /// <param name="noteView">갱신할 노트 View입니다.</param>
    /// <param name="placedNote">표시 기준이 되는 런타임 배치 노트입니다.</param>
    private void RefreshNoteView(IChartNoteView noteView, ChartPlacedNote placedNote)
    {
        if (noteView == null)
        {
            return;
        }

        if (placedNote == null || placedNote.Data == null)
        {
            noteView.SetVisible(false);
            return;
        }

        noteView.SetWorldPosition(placedNote.WorldPosition);
        noteView.RefreshFromBoundNote();
        ApplyJudgementDisplay(noteView, placedNote);
    }

    /// <summary>
    /// 판정 완료 상태에 맞춰 노트 View를 흐리게 표시하거나 숨깁니다.
    /// </summary>
    /// <param name="noteView">표시를 조정할 노트 View입니다.</param>
    /// <param name="placedNote">판정 상태를 읽을 런타임 배치 노트입니다.</param>
    private void ApplyJudgementDisplay(IChartNoteView noteView, ChartPlacedNote placedNote)
    {
        if (noteView == null || placedNote == null)
        {
            return;
        }

        if (placedNote.IsJudged && _hideJudgedNotes)
        {
            noteView.SetVisible(false);
            return;
        }

        noteView.SetVisible(true);
        noteView.SetJudged(placedNote.IsJudged);
    }

    /// <summary>
    /// 스테이지 데이터 또는 배치 노트 참조가 바뀌었는지 확인합니다.
    /// </summary>
    /// <returns>노트 View를 다시 만들어야 하면 true입니다.</returns>
    private bool HasStageSnapshotChanged()
    {
        if (_chartStageService == null)
        {
            return false;
        }

        IReadOnlyList<ChartPlacedNote> placedNotes = _chartStageService.PlacedNotes;
        int placedNoteCount = GetPlacedNoteCount(placedNotes);
        int placedNoteHash = CalculatePlacedNoteHash(placedNotes);

        if (_chartStageService.CurrentStageData != _cachedStageData)
        {
            return true;
        }

        if (placedNoteCount != _cachedPlacedNoteCount)
        {
            return true;
        }

        return placedNoteHash != _cachedPlacedNoteHash;
    }

    /// <summary>
    /// 현재 스테이지 배치 상태를 Presenter 캐시에 기록합니다.
    /// </summary>
    /// <param name="placedNotes">현재 스테이지 배치 노트 목록입니다.</param>
    private void CacheStageSnapshot(IReadOnlyList<ChartPlacedNote> placedNotes)
    {
        if (_chartStageService == null)
        {
            ClearCachedStageSnapshot();
            return;
        }

        _cachedStageData = _chartStageService.CurrentStageData;
        _cachedPlacedNoteCount = GetPlacedNoteCount(placedNotes);
        _cachedPlacedNoteHash = CalculatePlacedNoteHash(placedNotes);
    }

    /// <summary>Presenter가 기억하는 스테이지 표시 캐시를 비웁니다.</summary>
    private void ClearCachedStageSnapshot()
    {
        _cachedStageData = null;
        _cachedPlacedNoteCount = EMPTY_STAGE_NOTE_COUNT;
        _cachedPlacedNoteHash = 0;
    }

    /// <summary>
    /// 배치 노트 목록의 안전한 개수를 반환합니다.
    /// </summary>
    /// <param name="placedNotes">확인할 배치 노트 목록입니다.</param>
    /// <returns>null이면 0, 아니면 목록 개수입니다.</returns>
    private static int GetPlacedNoteCount(IReadOnlyList<ChartPlacedNote> placedNotes)
    {
        if (placedNotes == null)
        {
            return 0;
        }

        return placedNotes.Count;
    }

    /// <summary>
    /// 배치 노트 참조 변화를 감지하기 위한 간단한 해시를 계산합니다.
    /// </summary>
    /// <param name="placedNotes">확인할 배치 노트 목록입니다.</param>
    /// <returns>배치 노트 참조 기반 해시입니다.</returns>
    private static int CalculatePlacedNoteHash(IReadOnlyList<ChartPlacedNote> placedNotes)
    {
        if (placedNotes == null)
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;

            for (int i = 0; i < placedNotes.Count; i++)
            {
                ChartPlacedNote placedNote = placedNotes[i];
                hash = hash * 31 + (placedNote == null ? 0 : placedNote.GetHashCode());
            }

            return hash;
        }
    }

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartPlaySurfacePresenter] {message}");
    }
}
