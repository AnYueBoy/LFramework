namespace LFramework
{
    public interface ICoroutine : IAwaitable<CoroutineAwaiter>
    {
        CoroutineState State { get; }

        void Complete();

        void Pause();

        void Resume();

        void Bind(object target);
    }
}