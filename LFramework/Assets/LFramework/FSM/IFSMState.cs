namespace LFramework.FSM
{
    public interface IFSMState<TOwner> where TOwner : IOwner<TOwner>
    {
        void OnEnter(TOwner owner);

        void OnUpdate(TOwner owner, float dt);

        void OnExit(TOwner owner);
    }
}