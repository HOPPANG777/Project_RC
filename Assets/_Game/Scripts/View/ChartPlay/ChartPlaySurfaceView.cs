using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartPlaySurfaceView
// 설명: Gameplay 화면의 차트 레인, 판정선, 캔들 노트들을 담는 플레이 표면 View
//==============================================================================
// 세부 구현 사항
// - Directing 기준의 Gameplay 핵심 요소인 Chart Lane, Judgement Line, Candle Notes를 묶습니다.
// - Presenter가 요청한 런타임 배치 노트를 ChartNoteView 인스턴스로 생성하고 관리합니다.
// - 실제 입력 판정, 스테이지 흐름, 노트 데이터 생성은 Controller / Presenter 책임으로 남깁니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartPlaySurfaceView : MonoBehaviour, IChartPlaySurfaceView
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Refs")]
    [Tooltip("플레이 표면 전체를 표시하거나 숨길 루트 Transform입니다. 비어 있으면 이 오브젝트 Transform을 사용합니다.")]
    [SerializeField]
    private Transform _surfaceRoot;

    [TabGroup("Refs")]
    [Tooltip("캔들 노트 View들이 생성될 루트 Transform입니다. 비어 있으면 이 오브젝트 Transform을 사용합니다.")]
    [SerializeField]
    private Transform _noteRoot;

    [TabGroup("Refs")]
    [Tooltip("생성할 캔들 노트 View 프리팹입니다.")]
    [Required]
    [SerializeField]
    private ChartNoteView _noteViewPrefab;

    [TabGroup("Refs")]
    [Tooltip("입력 타이밍 기준이 되는 판정선 View입니다.")]
    [Required]
    [SerializeField]
    private ChartJudgementLineView _judgementLineView;

    [TabGroup("Settings")]
    [Tooltip("Awake 시 플레이 표면을 바로 표시할지 여부입니다.")]
    [SerializeField]
    private bool _showOnAwake = true;

    [TabGroup("Settings")]
    [Tooltip("ClearNoteViews 호출 시 생성된 노트 View 오브젝트를 파괴할지 여부입니다.")]
    [SerializeField]
    private bool _destroyNoteObjectsOnClear = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 생성되어 관리 중인 캔들 노트 View 목록입니다.
    private readonly List<ChartNoteView> _noteViews = new List<ChartNoteView>();

    // 현재 플레이 표면 표시 여부입니다.
    private bool _isVisible = true;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>플레이 표면 전체 Transform입니다.</summary>
    public Transform SurfaceTransform => GetSafeSurfaceRoot();

    /// <summary>캔들 노트 View들이 생성될 루트 Transform입니다.</summary>
    public Transform NoteRoot => GetSafeNoteRoot();

    /// <summary>입력 타이밍 기준이 되는 판정선 View입니다.</summary>
    public IChartJudgementLineView JudgementLineView => _judgementLineView;

    /// <summary>현재 생성되어 관리 중인 노트 View 수입니다.</summary>
    public int NoteViewCount => _noteViews.Count;

    /// <summary>현재 플레이 표면 표시 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("표시 상태")]
    private bool IsVisible => _isVisible;

    /// <summary>현재 판정선 View 연결 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("판정선 연결")]
    private bool HasJudgementLineView => _judgementLineView != null;

    /// <summary>현재 노트 View 프리팹 연결 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("노트 프리팹 연결")]
    private bool HasNoteViewPrefab => _noteViewPrefab != null;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>기본 참조를 보정하고 초기 표시 상태를 적용합니다.</summary>
    private void Awake()
    {
        TryCacheDefaultRefs();
        RefreshVisual();
        SetVisible(_showOnAwake);
    }

    /// <summary>컴포넌트를 처음 붙였을 때 자식 판정선 View를 자동 참조합니다.</summary>
    private void Reset()
    {
        TryCacheDefaultRefs();
    }

    /// <summary>인스펙터 값이 바뀔 때 플레이 표면 표시를 즉시 보정합니다.</summary>
    private void OnValidate()
    {
        TryCacheDefaultRefs();
        RefreshVisual();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>플레이 표면을 표시합니다.</summary>
    public void Show()
    {
        SetVisible(true);
    }

    /// <summary>플레이 표면을 숨깁니다.</summary>
    public void Hide()
    {
        SetVisible(false);
    }

    /// <summary>
    /// 플레이 표면 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isVisible">표시하면 true입니다.</param>
    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        ApplyVisibleState();
    }

    /// <summary>
    /// 런타임 배치 노트를 표시할 캔들 노트 View를 생성합니다.
    /// </summary>
    /// <param name="placedNote">표시할 런타임 배치 노트입니다.</param>
    /// <param name="noteView">생성된 캔들 노트 View입니다.</param>
    /// <returns>생성에 성공하면 true입니다.</returns>
    public bool TryCreateNoteView(ChartPlacedNote placedNote, out IChartNoteView noteView)
    {
        noteView = null;

        if (placedNote == null)
        {
            LogWarning("표시할 런타임 배치 노트가 null입니다.");
            return false;
        }

        if (_noteViewPrefab == null)
        {
            LogWarning("ChartNoteView 프리팹이 연결되지 않아 노트를 생성할 수 없습니다.");
            return false;
        }

        ChartNoteView createdNoteView = Instantiate(_noteViewPrefab, GetSafeNoteRoot());

        if (createdNoteView == null)
        {
            LogWarning("ChartNoteView 인스턴스 생성에 실패했습니다.");
            return false;
        }

        if (createdNoteView.TryBind(placedNote) == false)
        {
            DestroyNoteViewObject(createdNoteView);
            return false;
        }

        _noteViews.Add(createdNoteView);
        noteView = createdNoteView;
        return true;
    }

    /// <summary>
    /// 인덱스로 생성된 캔들 노트 View를 조회합니다.
    /// </summary>
    /// <param name="index">조회할 노트 View 인덱스입니다.</param>
    /// <param name="noteView">조회된 노트 View입니다.</param>
    /// <returns>노트 View를 찾으면 true입니다.</returns>
    public bool TryGetNoteView(int index, out IChartNoteView noteView)
    {
        noteView = null;

        if (index < 0 || index >= _noteViews.Count)
        {
            return false;
        }

        ChartNoteView foundNoteView = _noteViews[index];

        if (foundNoteView == null)
        {
            return false;
        }

        noteView = foundNoteView;
        return true;
    }

    /// <summary>생성되어 관리 중인 모든 캔들 노트 View를 갱신합니다.</summary>
    public void RefreshNoteViews()
    {
        for (int i = 0; i < _noteViews.Count; i++)
        {
            ChartNoteView noteView = _noteViews[i];

            if (noteView == null)
            {
                continue;
            }

            noteView.RefreshFromBoundNote();
        }
    }

    /// <summary>생성되어 관리 중인 모든 캔들 노트 View를 제거합니다.</summary>
    public void ClearNoteViews()
    {
        for (int i = _noteViews.Count - 1; i >= 0; i--)
        {
            ChartNoteView noteView = _noteViews[i];

            if (noteView == null)
            {
                continue;
            }

            if (_destroyNoteObjectsOnClear)
            {
                DestroyNoteViewObject(noteView);
                continue;
            }

            noteView.Hide();
        }

        _noteViews.Clear();
    }

    /// <summary>현재 설정값으로 플레이 표면 표시를 다시 적용합니다.</summary>
    public void RefreshVisual()
    {
        ApplyVisibleState();

        if (_judgementLineView == null)
        {
            LogWarning("판정선 View가 연결되지 않았습니다.");
            return;
        }

        _judgementLineView.RefreshVisual();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>비어 있는 기본 참조를 안전한 기본값으로 보정합니다.</summary>
    private void TryCacheDefaultRefs()
    {
        if (_surfaceRoot == null)
        {
            _surfaceRoot = transform;
        }

        if (_noteRoot == null)
        {
            _noteRoot = transform;
        }

        if (_judgementLineView == null)
        {
            _judgementLineView = GetComponentInChildren<ChartJudgementLineView>(true);
        }
    }

    /// <summary>플레이 표면 전체 표시 상태를 적용합니다.</summary>
    private void ApplyVisibleState()
    {
        Transform surfaceRoot = GetSafeSurfaceRoot();

        if (surfaceRoot == null)
        {
            return;
        }

        if (surfaceRoot.gameObject.activeSelf == _isVisible)
        {
            return;
        }

        surfaceRoot.gameObject.SetActive(_isVisible);
    }

    /// <summary>플레이 표면 루트가 비어 있으면 이 오브젝트 Transform을 반환합니다.</summary>
    /// <returns>사용할 플레이 표면 루트 Transform입니다.</returns>
    private Transform GetSafeSurfaceRoot()
    {
        if (_surfaceRoot != null)
        {
            return _surfaceRoot;
        }

        return transform;
    }

    /// <summary>노트 루트가 비어 있으면 이 오브젝트 Transform을 반환합니다.</summary>
    /// <returns>사용할 노트 루트 Transform입니다.</returns>
    private Transform GetSafeNoteRoot()
    {
        if (_noteRoot != null)
        {
            return _noteRoot;
        }

        return transform;
    }

    /// <summary>
    /// 생성된 노트 View 오브젝트를 안전하게 제거합니다.
    /// </summary>
    /// <param name="noteView">제거할 노트 View입니다.</param>
    private static void DestroyNoteViewObject(ChartNoteView noteView)
    {
        if (noteView == null)
        {
            return;
        }

        GameObject noteGameObject = noteView.gameObject;

        if (noteGameObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(noteGameObject);
            return;
        }

        DestroyImmediate(noteGameObject);
    }

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartPlaySurfaceView] {message}");
    }
}
