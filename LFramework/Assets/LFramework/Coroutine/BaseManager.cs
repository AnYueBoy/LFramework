namespace LFramework 
{
    public abstract class BaseManager<T> where T : class, new()
    {
        private static T instance;

        public static T I => instance ??= new T();
    }

    public abstract class BaseManager<IT, T> : BaseManager<T> where IT : class
        where T : class, IT, new()
    {
        private static IT instance;

        public new static IT I => instance ??= new T();
    }
}