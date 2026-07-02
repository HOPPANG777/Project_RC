using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartNoteView
// 설명: Gameplay 화면에서 BUY / SELL 입력 대상이 되는 캔들 노트를 표시하는 View
//==============================================================================
// 세부 구현 사항
// - Directing 기준에 맞춰 BUY는 초록 상승 캔들, SELL은 빨간 하락 캔들로 표시합니다.
// - 캔들 몸통, 꼬리, 종가 범위를 분리해 판정 기준점을 시각적으로 알 수 있게 합니다.
// - Transform 원점은 ChartPlacedNote.WorldPosition과 일치하는 종가 판정 기준점입니다.
//==============================================================================

[DisallowMultipleComponent]
public class ChartNoteView : MonoBehaviour, IChartNoteView
{
    //==============================================================================
    // 상수
    //==============================================================================

    // 캔들 몸통 크기가 0이 되는 상황을 막기 위한 최소값입니다.
    private const float MIN_BODY_SIZE = 0.01f;

    // 캔들 꼬리 크기가 0이 되는 상황을 막기 위한 최소값입니다.
    private const float MIN_WICK_SIZE = 0.001f;

    // 종가 범위 크기가 0이 되는 상황을 막기 위한 최소값입니다.
    private const float MIN_CLOSE_RANGE_SIZE = 0.001f;

    // 기본 캔들 몸통 너비입니다.
    private const float DEFAULT_BODY_WIDTH = 0.35f;

    // 기본 캔들 몸통 높이입니다.
    private const float DEFAULT_BODY_HEIGHT = 0.9f;

    // 기본 캔들 꼬리 너비입니다.
    private const float DEFAULT_WICK_WIDTH = 0.06f;

    // 기본 캔들 꼬리 높이입니다.
    private const float DEFAULT_WICK_HEIGHT = 1.25f;

    // 기본 종가 범위 높이입니다.
    private const float DEFAULT_CLOSE_RANGE_HEIGHT = 0.12f;

    // 자동 구성할 캔들 파트 이름입니다.
    private const string BODY_PART_NAME = "Body";
    private const string WICK_PART_NAME = "Wick";
    private const string CLOSE_RANGE_PART_NAME = "CloseRange";

    // 캔들 파트가 판정선보다 뒤에 보이도록 사용하는 기본 정렬 순서입니다.
    private const int WICK_SORTING_ORDER = 10;
    private const int BODY_SORTING_ORDER = 11;
    private const int CLOSE_RANGE_SORTING_ORDER = 12;

    // 프로젝트에 표시용 스프라이트가 없을 때 런타임에서 만드는 1x1 흰 사각형입니다.
    private static Texture2D _fallbackTexture;
    private static Sprite _fallbackSprite;

    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Refs")]
    [Tooltip("캔들 몸통의 위치와 크기를 조절할 Transform입니다.")]
    [Required]
    [SerializeField]
    private Transform _bodyTransform;

    [TabGroup("Refs")]
    [Tooltip("캔들 몸통 색상을 표시할 SpriteRenderer입니다.")]
    [Required]
    [SerializeField]
    private SpriteRenderer _bodyRenderer;

    [TabGroup("Refs")]
    [Tooltip("캔들 꼬리의 위치와 크기를 조절할 Transform입니다.")]
    [Required]
    [SerializeField]
    private Transform _wickTransform;

    [TabGroup("Refs")]
    [Tooltip("캔들 꼬리 색상을 표시할 SpriteRenderer입니다.")]
    [Required]
    [SerializeField]
    private SpriteRenderer _wickRenderer;

    [TabGroup("Refs")]
    [Tooltip("종가 판정 범위의 위치와 크기를 조절할 Transform입니다.")]
    [Required]
    [SerializeField]
    private Transform _closeRangeTransform;

    [TabGroup("Refs")]
    [Tooltip("종가 판정 범위를 표시할 SpriteRenderer입니다.")]
    [Required]
    [SerializeField]
    private SpriteRenderer _closeRangeRenderer;

    [TabGroup("Refs")]
    [Tooltip("BUY / SELL 방향을 보조로 표시할 선택 마커 Transform입니다.")]
    [SerializeField]
    private Transform _directionMarkerTransform;

    [TabGroup("Refs")]
    [Tooltip("BUY / SELL 방향 보조 마커 색상을 표시할 선택 SpriteRenderer입니다.")]
    [SerializeField]
    private SpriteRenderer _directionMarkerRenderer;

    [TabGroup("Settings")]
    [Tooltip("캔들 몸통 월드 기준 너비입니다.")]
    [MinValue(MIN_BODY_SIZE)]
    [SerializeField]
    private float _bodyWidth = DEFAULT_BODY_WIDTH;

    [TabGroup("Settings")]
    [Tooltip("캔들 몸통 월드 기준 높이입니다.")]
    [MinValue(MIN_BODY_SIZE)]
    [SerializeField]
    private float _bodyHeight = DEFAULT_BODY_HEIGHT;

    [TabGroup("Settings")]
    [Tooltip("캔들 꼬리 월드 기준 너비입니다.")]
    [MinValue(MIN_WICK_SIZE)]
    [SerializeField]
    private float _wickWidth = DEFAULT_WICK_WIDTH;

    [TabGroup("Settings")]
    [Tooltip("캔들 꼬리 월드 기준 높이입니다.")]
    [MinValue(MIN_WICK_SIZE)]
    [SerializeField]
    private float _wickHeight = DEFAULT_WICK_HEIGHT;

    [TabGroup("Settings")]
    [Tooltip("종가 판정 범위 월드 기준 높이입니다.")]
    [MinValue(MIN_CLOSE_RANGE_SIZE)]
    [SerializeField]
    private float _closeRangeHeight = DEFAULT_CLOSE_RANGE_HEIGHT;

    [TabGroup("Settings")]
    [Tooltip("BUY 노트에 사용할 선명한 초록 색상입니다.")]
    [SerializeField]
    private Color _buyColor = new Color(0.25f, 1.0f, 0.35f, 1.0f);

    [TabGroup("Settings")]
    [Tooltip("SELL 노트에 사용할 선명한 빨강 색상입니다.")]
    [SerializeField]
    private Color _sellColor = new Color(1.0f, 0.22f, 0.18f, 1.0f);

    [TabGroup("Settings")]
    [Tooltip("종가 판정 범위에 적용할 명도 배율입니다.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float _closeRangeColorMultiplier = 0.55f;

    [TabGroup("Settings")]
    [Tooltip("판정 전 필수 노트의 기본 투명도입니다.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float _requiredAlpha = 1.0f;

    [TabGroup("Settings")]
    [Tooltip("판정 대상이 아닌 장식 노트의 투명도입니다.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float _decorativeAlpha = 0.45f;

    [TabGroup("Settings")]
    [Tooltip("판정 완료된 노트의 투명도입니다.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float _judgedAlpha = 0.25f;

    [TabGroup("Settings")]
    [Tooltip("Awake 시 노트를 바로 표시할지 여부입니다.")]
    [SerializeField]
    private bool _showOnAwake = true;

    //==============================================================================
    // 내부 상태
    //==============================================================================

    // 현재 연결된 런타임 배치 노트입니다.
    private ChartPlacedNote _boundNote;

    // 현재 런타임 배치 노트가 연결되어 있는지 여부입니다.
    private bool _hasBoundNote;

    // 현재 노트가 요구하는 BUY 또는 SELL 입력 방향입니다.
    private ChartNoteSide _side = ChartNoteSide.NOTE_SIDE_BUY;

    // 현재 노트가 판정 완료 상태로 표시되는지 여부입니다.
    private bool _isJudged;

    // 현재 노트가 판정 대상인 필수 입력 노트인지 여부입니다.
    private bool _isRequired = true;

    // 현재 노트 표시 여부입니다.
    private bool _isVisible = true;

    // 현재 노트에 적용되는 투명도입니다.
    private float _currentAlpha = 1.0f;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>캔들 노트 Transform입니다. 원점은 종가 판정 기준점입니다.</summary>
    public Transform NoteTransform => transform;

    /// <summary>현재 런타임 배치 노트가 연결되어 있는지 여부입니다.</summary>
    public bool HasBoundNote => _hasBoundNote;

    /// <summary>현재 연결된 런타임 배치 노트입니다.</summary>
    public ChartPlacedNote BoundNote => _boundNote;

    /// <summary>현재 노트가 요구하는 BUY 또는 SELL 입력 방향입니다.</summary>
    public ChartNoteSide Side => _side;

    /// <summary>현재 노트가 판정 완료 상태로 표시되는지 여부입니다.</summary>
    public bool IsJudged => _isJudged;

    /// <summary>현재 노트가 판정 대상인 필수 입력 노트인지 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("필수 노트")]
    private bool IsRequired => _isRequired;

    /// <summary>현재 표시 여부입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("표시 상태")]
    private bool IsVisible => _isVisible;

    /// <summary>현재 연결된 노트 인덱스입니다.</summary>
    [TabGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("노트 인덱스")]
    private int NoteIndex
    {
        get
        {
            if (_boundNote == null)
            {
                return -1;
            }

            return _boundNote.Index;
        }
    }

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

    /// <summary>컴포넌트를 처음 붙였을 때 같은 오브젝트의 기본 SpriteRenderer를 자동 참조합니다.</summary>
    private void Reset()
    {
        TryCacheDefaultRefs();
        ClampSettings();
    }

    /// <summary>인스펙터 값이 바뀔 때 노트 표시를 즉시 보정합니다.</summary>
    private void OnValidate()
    {
        TryCacheDefaultRefs();
        ClampSettings();
        RefreshVisual();
    }

    //==============================================================================
    // 공개 API
    //==============================================================================

    /// <summary>
    /// 런타임 배치 노트를 View에 연결하고 표시를 갱신합니다.
    /// </summary>
    /// <param name="placedNote">표시할 런타임 배치 노트입니다.</param>
    /// <returns>연결에 성공하면 true입니다.</returns>
    public bool TryBind(ChartPlacedNote placedNote)
    {
        if (placedNote == null)
        {
            LogWarning("표시할 런타임 배치 노트가 null입니다.");
            Hide();
            return false;
        }

        if (placedNote.Data == null)
        {
            LogWarning("런타임 배치 노트의 원본 데이터가 null입니다.");
            Hide();
            return false;
        }

        _boundNote = placedNote;
        _hasBoundNote = true;
        _side = placedNote.Side;
        _isJudged = placedNote.IsJudged;
        _isRequired = placedNote.Data.IsRequired;

        SetWorldPosition(placedNote.WorldPosition);
        RefreshVisual();
        Show();
        return true;
    }

    /// <summary>연결된 런타임 배치 노트의 최신 상태를 표시로 다시 반영합니다.</summary>
    public void RefreshFromBoundNote()
    {
        if (_boundNote == null)
        {
            return;
        }

        if (_boundNote.Data == null)
        {
            Hide();
            return;
        }

        _side = _boundNote.Side;
        _isJudged = _boundNote.IsJudged;
        _isRequired = _boundNote.Data.IsRequired;

        SetWorldPosition(_boundNote.WorldPosition);
        RefreshVisual();
    }

    /// <summary>
    /// BUY / SELL 입력 방향에 맞는 색상과 형태를 적용합니다.
    /// </summary>
    /// <param name="side">표시할 입력 방향입니다.</param>
    public void SetSide(ChartNoteSide side)
    {
        _side = side;
        RefreshVisual();
    }

    /// <summary>
    /// 판정 완료 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isJudged">판정 완료 상태이면 true입니다.</param>
    public void SetJudged(bool isJudged)
    {
        _isJudged = isJudged;
        ApplyAlphaByState();
    }

    /// <summary>노트를 표시합니다.</summary>
    public void Show()
    {
        SetVisible(true);
    }

    /// <summary>노트를 숨깁니다.</summary>
    public void Hide()
    {
        SetVisible(false);
    }

    /// <summary>
    /// 노트 표시 여부를 설정합니다.
    /// </summary>
    /// <param name="isVisible">표시하면 true입니다.</param>
    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        ApplyVisibleState();
    }

    /// <summary>
    /// 노트 투명도를 설정합니다.
    /// </summary>
    /// <param name="alpha">0-1 범위의 투명도입니다.</param>
    public void SetAlpha(float alpha)
    {
        _currentAlpha = Mathf.Clamp01(alpha);
        ApplyColor();
    }

    /// <summary>
    /// 노트 월드 위치를 설정합니다.
    /// </summary>
    /// <param name="worldPosition">새 월드 위치입니다.</param>
    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    /// <summary>현재 설정값으로 노트 표시를 다시 적용합니다.</summary>
    public void RefreshVisual()
    {
        if (HasRequiredRefs() == false)
        {
            LogWarning("필수 표시 참조가 연결되지 않아 캔들 노트를 갱신할 수 없습니다.");
            return;
        }

        RefreshCandleGeometry();
        ApplyAlphaByState();
        ApplyVisibleState();
    }

    //==============================================================================
    // 내부 로직
    //==============================================================================

    /// <summary>같은 오브젝트에 있는 기본 SpriteRenderer를 몸통 참조로 캐시합니다.</summary>
    private void TryCacheDefaultRefs()
    {
        if (_bodyRenderer == null)
        {
            _bodyRenderer = GetComponent<SpriteRenderer>();
        }

        if (_bodyTransform == null && _bodyRenderer != null)
        {
            _bodyTransform = _bodyRenderer.transform;
        }

        if (_bodyRenderer == null)
        {
            _bodyRenderer = FindOrCreateSpritePart(BODY_PART_NAME, BODY_SORTING_ORDER);
            _bodyTransform = _bodyRenderer.transform;
        }

        if (_wickRenderer == null)
        {
            _wickRenderer = FindOrCreateSpritePart(WICK_PART_NAME, WICK_SORTING_ORDER);
            _wickTransform = _wickRenderer.transform;
        }

        if (_wickTransform == null && _wickRenderer != null)
        {
            _wickTransform = _wickRenderer.transform;
        }

        if (_closeRangeRenderer == null)
        {
            _closeRangeRenderer = FindOrCreateSpritePart(CLOSE_RANGE_PART_NAME, CLOSE_RANGE_SORTING_ORDER);
            _closeRangeTransform = _closeRangeRenderer.transform;
        }

        if (_closeRangeTransform == null && _closeRangeRenderer != null)
        {
            _closeRangeTransform = _closeRangeRenderer.transform;
        }
    }

    /// <summary>지정한 이름의 캔들 파트를 찾거나 새로 만들어 SpriteRenderer를 반환합니다.</summary>
    private SpriteRenderer FindOrCreateSpritePart(string partName, int sortingOrder)
    {
        Transform partTransform = transform.Find(partName);

        if (partTransform == null)
        {
            GameObject partObject = new GameObject(partName);
            partTransform = partObject.transform;
            partTransform.SetParent(transform, false);
        }

        partTransform.localPosition = Vector3.zero;
        partTransform.localRotation = Quaternion.identity;
        partTransform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = partTransform.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = partTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetFallbackSprite();
        }

        spriteRenderer.sortingOrder = sortingOrder;
        return spriteRenderer;
    }

    /// <summary>별도 에셋 없이 캔들 파트를 표시하기 위한 기본 흰 사각형 스프라이트를 반환합니다.</summary>
    private static Sprite GetFallbackSprite()
    {
#if UNITY_EDITOR
        Sprite editorSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        if (editorSprite != null)
        {
            return editorSprite;
        }
#endif

        if (_fallbackSprite != null)
        {
            return _fallbackSprite;
        }

        if (_fallbackTexture == null)
        {
            _fallbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Chart Note Fallback Sprite Texture",
                hideFlags = HideFlags.HideAndDontSave
            };

            _fallbackTexture.SetPixel(0, 0, Color.white);
            _fallbackTexture.Apply();
        }

        _fallbackSprite = Sprite.Create(
            _fallbackTexture,
            new Rect(0.0f, 0.0f, 1.0f, 1.0f),
            new Vector2(0.5f, 0.5f),
            1.0f);

        _fallbackSprite.name = "Chart Note Fallback Sprite";
        _fallbackSprite.hideFlags = HideFlags.HideAndDontSave;
        return _fallbackSprite;
    }

    /// <summary>필수 표시 참조가 모두 연결되어 있는지 확인합니다.</summary>
    /// <returns>필수 참조가 모두 있으면 true입니다.</returns>
    private bool HasRequiredRefs()
    {
        if (_bodyTransform == null || _bodyRenderer == null)
        {
            return false;
        }

        if (_wickTransform == null || _wickRenderer == null)
        {
            return false;
        }

        if (_closeRangeTransform == null || _closeRangeRenderer == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>설정값을 안전한 범위로 보정합니다.</summary>
    private void ClampSettings()
    {
        _bodyWidth = GetSafePositiveValue(_bodyWidth, DEFAULT_BODY_WIDTH, MIN_BODY_SIZE);
        _bodyHeight = GetSafePositiveValue(_bodyHeight, DEFAULT_BODY_HEIGHT, MIN_BODY_SIZE);
        _wickWidth = GetSafePositiveValue(_wickWidth, DEFAULT_WICK_WIDTH, MIN_WICK_SIZE);
        _wickHeight = GetSafePositiveValue(_wickHeight, DEFAULT_WICK_HEIGHT, MIN_WICK_SIZE);
        _closeRangeHeight = GetSafePositiveValue(
            _closeRangeHeight,
            DEFAULT_CLOSE_RANGE_HEIGHT,
            MIN_CLOSE_RANGE_SIZE);
    }

    /// <summary>현재 BUY / SELL 방향에 맞춰 캔들 몸통, 꼬리, 종가 범위 배치를 갱신합니다.</summary>
    private void RefreshCandleGeometry()
    {
        float direction = GetDirectionSign(_side);
        float bodyCenterY = -direction * _bodyHeight * 0.5f;

        _bodyTransform.localPosition = new Vector3(0.0f, bodyCenterY, 0.0f);
        _bodyTransform.localScale = new Vector3(_bodyWidth, _bodyHeight, 1.0f);

        _wickTransform.localPosition = new Vector3(0.0f, bodyCenterY, 0.0f);
        _wickTransform.localScale = new Vector3(_wickWidth, _wickHeight, 1.0f);

        _closeRangeTransform.localPosition = Vector3.zero;
        _closeRangeTransform.localScale = new Vector3(_bodyWidth, _closeRangeHeight, 1.0f);

        RefreshDirectionMarker(direction);
    }

    /// <summary>
    /// 선택 방향 마커가 연결되어 있으면 BUY / SELL 방향에 맞춰 위치를 갱신합니다.
    /// </summary>
    /// <param name="direction">BUY는 1, SELL은 -1입니다.</param>
    private void RefreshDirectionMarker(float direction)
    {
        if (_directionMarkerTransform == null)
        {
            return;
        }

        float markerOffsetY = -direction * (_bodyHeight + _closeRangeHeight);

        _directionMarkerTransform.localPosition = new Vector3(0.0f, markerOffsetY, 0.0f);
        _directionMarkerTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, direction > 0.0f ? 0.0f : 180.0f);
    }

    /// <summary>판정 상태와 필수 여부에 맞는 투명도를 적용합니다.</summary>
    private void ApplyAlphaByState()
    {
        if (_isJudged)
        {
            SetAlpha(_judgedAlpha);
            return;
        }

        if (_isRequired == false)
        {
            SetAlpha(_decorativeAlpha);
            return;
        }

        SetAlpha(_requiredAlpha);
    }

    /// <summary>현재 BUY / SELL 방향과 투명도에 맞춰 색상을 적용합니다.</summary>
    private void ApplyColor()
    {
        Color baseColor = GetSideColor(_side);
        Color closeRangeColor = GetCloseRangeColor(baseColor);

        SetRendererColor(_bodyRenderer, baseColor, _currentAlpha);
        SetRendererColor(_wickRenderer, baseColor, _currentAlpha);
        SetRendererColor(_closeRangeRenderer, closeRangeColor, _currentAlpha);
        SetRendererColor(_directionMarkerRenderer, baseColor, _currentAlpha);
    }

    /// <summary>현재 표시 상태를 각 SpriteRenderer와 선택 마커에 적용합니다.</summary>
    private void ApplyVisibleState()
    {
        SetRendererEnabled(_bodyRenderer, _isVisible);
        SetRendererEnabled(_wickRenderer, _isVisible);
        SetRendererEnabled(_closeRangeRenderer, _isVisible);
        SetRendererEnabled(_directionMarkerRenderer, _isVisible);
    }

    /// <summary>
    /// 입력 방향에 맞는 캔들 색상을 반환합니다.
    /// </summary>
    /// <param name="side">확인할 입력 방향입니다.</param>
    /// <returns>BUY면 초록, SELL이면 빨강입니다.</returns>
    private Color GetSideColor(ChartNoteSide side)
    {
        switch (side)
        {
            case ChartNoteSide.NOTE_SIDE_SELL:
                return _sellColor;
            case ChartNoteSide.NOTE_SIDE_BUY:
            default:
                return _buyColor;
        }
    }

    /// <summary>
    /// 입력 방향에 맞는 캔들 진행 부호를 반환합니다.
    /// </summary>
    /// <param name="side">확인할 입력 방향입니다.</param>
    /// <returns>BUY는 1, SELL은 -1입니다.</returns>
    private static float GetDirectionSign(ChartNoteSide side)
    {
        if (side == ChartNoteSide.NOTE_SIDE_SELL)
        {
            return -1.0f;
        }

        return 1.0f;
    }

    /// <summary>
    /// 종가 범위용으로 명도를 낮춘 색상을 반환합니다.
    /// </summary>
    /// <param name="baseColor">기준 BUY / SELL 색상입니다.</param>
    /// <returns>명도를 낮춘 종가 범위 색상입니다.</returns>
    private Color GetCloseRangeColor(Color baseColor)
    {
        float multiplier = Mathf.Clamp01(_closeRangeColorMultiplier);

        return new Color(
            baseColor.r * multiplier,
            baseColor.g * multiplier,
            baseColor.b * multiplier,
            baseColor.a);
    }

    /// <summary>
    /// SpriteRenderer 색상을 null 안전하게 설정합니다.
    /// </summary>
    /// <param name="targetRenderer">색상을 설정할 렌더러입니다.</param>
    /// <param name="targetColor">적용할 색상입니다.</param>
    /// <param name="alpha">적용할 투명도입니다.</param>
    private static void SetRendererColor(SpriteRenderer targetRenderer, Color targetColor, float alpha)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetColor.a = Mathf.Clamp01(alpha);
        targetRenderer.color = targetColor;
    }

    /// <summary>
    /// SpriteRenderer 표시 여부를 null 안전하게 설정합니다.
    /// </summary>
    /// <param name="targetRenderer">표시 여부를 설정할 렌더러입니다.</param>
    /// <param name="isEnabled">표시하면 true입니다.</param>
    private static void SetRendererEnabled(SpriteRenderer targetRenderer, bool isEnabled)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.enabled = isEnabled;
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
        Debug.LogWarning($"[ChartNoteView] {message}");
    }
}
