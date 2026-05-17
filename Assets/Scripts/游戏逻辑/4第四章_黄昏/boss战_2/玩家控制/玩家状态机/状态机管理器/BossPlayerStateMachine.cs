using TangmenFramework;

public class BossPlayerStateMachine : StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj>
{
    public BossPlayerStateMachine(IBossPlayerFSMObj aiObj) : base(aiObj)
    {
    }
}
