using System;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: class::ChartNoteData
// 설명: 캔들 노트 1개의 판정 시간, 입력 방향, 차트 높이와 캔들 형태 정보를 담는 데이터
//==============================================================================
// 세부 구현 사항
// - 0.1초 = 1 노드 기준을 따르지만, 모든 노드에 입력 노트가 있을 필요는 없습니다.
// - Time은 판정선에 닿아야 하는 스테이지 진행 시간입니다.
// - Value는 ChartStageController가 월드 Y 위치로 변환할 종가 기준 차트 높이 값입니다.
// - 커스텀 캔들 형태를 켜면 Open / High / Low 값으로 실제 OHLC 캔들처럼 표시합니다.
//==============================================================================

[Serializable]
public class ChartNoteData
{
    //==============================================================================
    // 직렬화 필드
    //==============================================================================

    [TabGroup("Note")]
    [Tooltip("노트가 판정선에 닿아야 하는 스테이지 진행 시간(초)입니다.")]
    [MinValue(0.0f)]
    [SerializeField]
    private float _time;

    [TabGroup("Note")]
    [Tooltip("이 노트가 요구하는 BUY 또는 SELL 입력 방향입니다.")]
    [SerializeField]
    private ChartNoteSide _side = ChartNoteSide.NOTE_SIDE_BUY;

    [TabGroup("Note")]
    [Tooltip("종가 기준 차트 높이 값입니다. ChartStageController가 월드 Y 위치와 판정 기준점으로 변환합니다.")]
    [SerializeField]
    private float _value = 0.5f;

    [TabGroup("Candle")]
    [Tooltip("켜면 Open/High/Low 값으로 실제 OHLC 캔들 형태를 표시합니다. 꺼두면 입력 방향 기준 기본 캔들 형태를 사용합니다.")]
    [SerializeField]
    private bool _useCustomCandleShape;

    [TabGroup("Candle")]
    [Tooltip("커스텀 캔들에서 시가로 사용할 차트 높이 값입니다.")]
    [SerializeField]
    private float _openValue = 0.0f;

    [TabGroup("Candle")]
    [Tooltip("커스텀 캔들에서 고가로 사용할 차트 높이 값입니다. 시가/종가보다 낮으면 런타임에서 보정됩니다.")]
    [SerializeField]
    private float _highValue = 0.75f;

    [TabGroup("Candle")]
    [Tooltip("커스텀 캔들에서 저가로 사용할 차트 높이 값입니다. 시가/종가보다 높으면 런타임에서 보정됩니다.")]
    [SerializeField]
    private float _lowValue = -0.75f;

    [TabGroup("Note")]
    [Tooltip("기획자가 구간 의미를 표시하기 위한 짧은 라벨입니다. 예: Drop, Rebound, Switch")]
    [SerializeField]
    private string _label;

    [TabGroup("Note")]
    [Tooltip("판정 대상이 되는 필수 입력 노트인지 여부입니다. false면 장식/보조 차트 노드로 사용할 수 있습니다.")]
    [SerializeField]
    private bool _isRequired = true;

    //==============================================================================
    // 프로퍼티
    //==============================================================================

    /// <summary>노트가 판정선에 닿아야 하는 스테이지 진행 시간(초)입니다.</summary>
    public float Time => _time;

    /// <summary>이 노트가 요구하는 BUY 또는 SELL 입력 방향입니다.</summary>
    public ChartNoteSide Side => _side;

    /// <summary>종가 기준 차트 높이 값입니다. 런타임에서 월드 Y 위치와 판정 기준점으로 변환됩니다.</summary>
    public float Value => _value;

    /// <summary>커스텀 OHLC 캔들 형태를 사용할지 여부입니다.</summary>
    public bool UseCustomCandleShape => _useCustomCandleShape;

    /// <summary>커스텀 캔들에서 시가로 사용할 차트 높이 값입니다.</summary>
    public float OpenValue => _openValue;

    /// <summary>커스텀 캔들에서 고가로 사용할 차트 높이 값입니다.</summary>
    public float HighValue => _highValue;

    /// <summary>커스텀 캔들에서 저가로 사용할 차트 높이 값입니다.</summary>
    public float LowValue => _lowValue;

    /// <summary>커스텀 캔들에서 종가로 사용할 차트 높이 값입니다.</summary>
    public float CloseValue => _value;

    /// <summary>기획자가 구간 의미를 표시하기 위한 짧은 라벨입니다.</summary>
    public string Label => _label;

    /// <summary>판정 대상이 되는 필수 입력 노트인지 여부입니다.</summary>
    public bool IsRequired => _isRequired;
}
