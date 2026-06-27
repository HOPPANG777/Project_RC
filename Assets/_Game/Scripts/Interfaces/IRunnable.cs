//==============================================================================
// 기능: interface::IRunnable
// 설명: 시작, 일시정지, 재개, 정지가 가능한 진행 대상의 공용 행동 계약
//==============================================================================

public interface IRunnable
{
    /// <summary>현재 진행 중인지 여부입니다.</summary>
    bool IsRunning { get; }

    /// <summary>현재 일시정지 상태인지 여부입니다.</summary>
    bool IsPaused { get; }

    /// <summary>진행을 시작합니다.</summary>
    /// <returns>시작에 성공하면 true입니다.</returns>
    bool TryStart();

    /// <summary>진행을 일시정지합니다.</summary>
    void Pause();

    /// <summary>일시정지된 진행을 재개합니다.</summary>
    void Resume();

    /// <summary>진행을 중지하고 초기 상태로 되돌립니다.</summary>
    void Stop();
}
