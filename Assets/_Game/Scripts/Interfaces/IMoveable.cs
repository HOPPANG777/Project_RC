using UnityEngine;

//==============================================================================
// 기능: interface::IMoveable
// 설명: 월드 위치를 직접 이동할 수 있는 대상의 공용 행동 계약
//==============================================================================

public interface IMoveable
{
    /// <summary>현재 월드 위치입니다.</summary>
    Vector3 Position { get; }

    /// <summary>대상을 지정한 월드 위치로 이동시킵니다.</summary>
    /// <param name="position">이동할 월드 위치입니다.</param>
    void MoveTo(Vector3 position);

    /// <summary>대상을 현재 위치에서 지정한 거리만큼 이동시킵니다.</summary>
    /// <param name="delta">이동할 월드 거리입니다.</param>
    void MoveBy(Vector3 delta);
}
