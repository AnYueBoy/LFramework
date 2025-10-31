namespace LFramework.FSM
{
    public interface IOwner<T> where T : IOwner<T>
    {
        IFSM<T> GetFSM();
    }
}