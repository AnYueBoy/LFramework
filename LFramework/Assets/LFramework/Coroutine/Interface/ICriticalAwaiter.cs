using System.Runtime.CompilerServices;

namespace LFramework
{
    public interface ICriticalAwaiter : IAwaiter, ICriticalNotifyCompletion
    {
    }

    public interface ICriticalAwaiter<out TResult> : IAwaiter<TResult>, ICriticalNotifyCompletion
    {
    }
}