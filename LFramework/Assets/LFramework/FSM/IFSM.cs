namespace LFramework.FSM
{
    public interface IFSM<TOwner> where TOwner : IOwner<TOwner>
    {
        void OnUpdate(float dt);

        void ChangeState<TState>(bool isForce = false) where TState : class, IFSMState<TOwner>;

        bool IsInState<TState>() where TState : class, IFSMState<TOwner>;

        TState GetState<TState>() where TState : class, IFSMState<TOwner>;
    }
}