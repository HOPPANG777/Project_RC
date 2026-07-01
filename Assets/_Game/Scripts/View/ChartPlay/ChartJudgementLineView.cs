using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartJudgementLineView
// 설명: Gameplay 화면에서 캔들 노트의 입력 타이밍 기준이 되는 판정선을 표시하는 View
//==============================================================================
// 세부 구현 사항
// - Directing 기준에 맞춰 노트보다 먼저 읽히는 밝고 명확한 수평선을 담당합니다.
// - Transform 원점은 ChartRunController의 runnerTransform으로 연결할 판정 기준점입니다.
// - 판정 로직과 노트 생성은 Controller / Presenter 책임으로 남기고, 이 클래스는 표시만 담당합니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartJudgementLineView : MonoBehaviour, IChartJudgementLineView
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 판정선 길이가 0이 되는 상황을 막기 위한 최소 길이입니다.
    private const float MIN_LINE_LENGTH = 0.01f;

    // 판정선 두께가 0이 되는 상황을 막기 위한 최소 두께입니다.
    private const float MIN_LINE_WIDTH = 0.001f;

    // 판정선 기본 가로 길이입니다.
    private const float DEFAULT_LINE_LENGTH = 12.0f;

    // 판정선 기본 두께입니다.
    private const float DEFAULT_LINE_WIDTH = 0.05f;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Refs")]
    [Tooltip("판정선을 그릴 LineRenderer입니다. Transform 원점이 실제 판정 기준점입니다.")]
    [Required]
    [SerializeField]
    private LineRenderer _lineRenderer;

    [TabGroup("Refs")]
    [Tooltip("캔들 종가 지점이 닿는 기준점을 강조할 선택 마커입니다.")]
    [SerializeField]
    private Transform _closePointMarker;

    [TabGroup("Settings")]
    [Tooltip("월드 기준 판정선 가로 길이입니다.")]
    [MinValue(MIN_LINE_LENGTH)]
    [SerializeField]
    private float _lineLength = DEFAULT_LINE_LENGTH;

    [TabGroup("Settings")]
    [Tooltip("월드 기준 판정선 두께입니다.")]
    [MinValue(MIN_LINE_WIDTH)]
    [SerializeField]
    private float _lineWidth = DEFAULT_LINE_WIDTH;

    [TabGroup("Settings")]
    [Tooltip("기본 판정선 색상입니다. 어두운 거래창 배경 위에서 잘 보이는 청록 계열입니다.")]
    [SerializeField]
    private Color _lineColor = new Color(0.35f, 0.95f, 1.0f, 1.0f);

    [TabGroup("Settings")]
    [Tooltip("입력 피드백 등으로 판정선을 강조할 때 사용할 색상입니다.")]
    [SerializeField]
    private Color _highlightColor = new Color(1.0f, 0.2f, 0.85f, 1.0f);

    [TabGroup("Settings")]
    [Tooltip("Awake 시 판정선을 바로 표시할지 여부입니다.")]
    [SerializeField]
    private bool _showOnAwake = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 판정선 표시 여부입니다.
    private bool _isVisible = true;

    // 현재 판정선 강조 여부입니다.
    private bool _isHighlighted;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>ChartRunController의 runnerTransform으로 연결할 판정 기준 Transform입니다.</summary>
    public Transform JudgementTransform => transform;

    /// <summary>현재 판정선 표시 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("표시 상태")]
    private bool IsVisible => _isVisible;

    /// <summary>현재 판정선 강조 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("강조 상태")]
    private bool IsHighlighted => _isHighlighted;

    //==============================================================================
    // 유니티 생명주기
    //==============================================================================

    /// <summary>참조와 설정값을 보정하고 초기 표시 상태를 적용합니다.</summary>
    private void Awake()
    {
        TryCacheDefaultRefs();
        ClampSettings();
        RefreshVisual();
        SetVisible(_showOnAwake);
    }

    /// <summary>컴포넌트를 처음 붙였을 때 같은 오브젝트의 LineRenderer를 자동 참조합니다.</summary>
    private void Reset()
    {
        TryCacheDefaultRefs();
        ClampSettings();
    }

    /// <summary>인스펙터 값이 바뀔 때 판정선 표시를 즉시 보정합니다.</summary>
    private void OnValidate()
    {
        TryCacheDefaultRefs();
        ClampSettings();

        if (_lineRenderer == null)
        {
            return;
        }

        RefreshVisual();
        ApplyVisibleState();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>판정선을 표시합니다.</summary>
    public void Show()
    {
        SetVisible(true);
    }

    /// <summary>판정선을 숨깁니다.</summary>
    public void Hide()
    {
        SetVisible(false);
    }

    /// <summary>
    /// 판정선 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isVisible">표시하면 true입니다.</param>
    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        ApplyVisibleState();
    }

    /// <summary>
    /// 판정선 강조 여부를 설정합니다.
    /// </summary>
    /// <param name="isHighlighted">강조 색상을 사용하면 true입니다.</param>
    public void SetHighlight(bool isHighlighted)
    {
        if (_isHighlighted == isHighlighted)
        {
            return;
        }

        _isHighlighted = isHighlighted;
        ApplyLineColor();
    }

    /// <summary>
    /// 판정선 기본 색상을 변경합니다.
    /// </summary>
    /// <param name="lineColor">새 기본 색상입니다.</param>
    public void SetLineColor(Color lineColor)
    {
        _lineColor = lineColor;
        ApplyLineColor();
    }

    /// <summary>
    /// 판정선 가로 길이를 변경합니다.
    /// </summary>
    /// <param name="lineLength">새 가로 길이입니다.</param>
    public void SetLineLength(float lineLength)
    {
        _lineLength = GetSafePositiveValue(lineLength, DEFAULT_LINE_LENGTH, MIN_LINE_LENGTH);
        RefreshLinePosition();
    }

    /// <summary>
    /// 판정선 두께를 변경합니다.
    /// </summary>
    /// <param name="lineWidth">새 두께입니다.</param>
    public void SetLineWidth(float lineWidth)
    {
        _lineWidth = GetSafePositiveValue(lineWidth, DEFAULT_LINE_WIDTH, MIN_LINE_WIDTH);
        RefreshLineWidth();
    }

    /// <summary>현재 설정값으로 판정선 표시를 다시 적용합니다.</summary>
    public void RefreshVisual()
    {
        if (_lineRenderer == null)
        {
            LogWarning("LineRenderer가 연결되지 않아 판정선을 표시할 수 없습니다.");
            return;
        }

        _lineRenderer.useWorldSpace = false;
        _lineRenderer.positionCount = 2;

        RefreshLinePosition();
        RefreshLineWidth();
        ApplyLineColor();
        ApplyVisibleState();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>같은 오브젝트에 있는 기본 참조를 캐시합니다.</summary>
    private void TryCacheDefaultRefs()
    {
        if (_lineRenderer != null)
        {
            return;
        }

        _lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>설정값을 안전한 범위로 보정합니다.</summary>
    private void ClampSettings()
    {
        _lineLength = GetSafePositiveValue(_lineLength, DEFAULT_LINE_LENGTH, MIN_LINE_LENGTH);
        _lineWidth = GetSafePositiveValue(_lineWidth, DEFAULT_LINE_WIDTH, MIN_LINE_WIDTH);
    }

    /// <summary>LineRenderer의 좌우 점 위치를 판정선 길이에 맞춰 갱신합니다.</summary>
    private void RefreshLinePosition()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        float halfLength = _lineLength * 0.5f;

        _lineRenderer.SetPosition(0, new Vector3(-halfLength, 0.0f, 0.0f));
        _lineRenderer.SetPosition(1, new Vector3(halfLength, 0.0f, 0.0f));
    }

    /// <summary>LineRenderer의 두께를 갱신합니다.</summary>
    private void RefreshLineWidth()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
    }

    /// <summary>현재 강조 상태에 맞춰 판정선 색상을 적용합니다.</summary>
    private void ApplyLineColor()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        Color targetColor = _isHighlighted ? _highlightColor : _lineColor;

        _lineRenderer.startColor = targetColor;
        _lineRenderer.endColor = targetColor;
    }

    /// <summary>현재 표시 상태를 LineRenderer와 선택 마커에 적용합니다.</summary>
    private void ApplyVisibleState()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = _isVisible;
        }

        if (_closePointMarker == null)
        {
            return;
        }

        _closePointMarker.gameObject.SetActive(_isVisible);
    }

    /// <summary>
    /// 양수여야 하는 설정값을 안전하게 보정합니다.
    /// </summary>
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

    /// <summary>에디터와 개발 빌드에서만 출력되는 경고 로그입니다.</summary>
    /// <param name="message">출력할 경고 내용입니다.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[ChartJudgementLineView] {message}");
    }
}
