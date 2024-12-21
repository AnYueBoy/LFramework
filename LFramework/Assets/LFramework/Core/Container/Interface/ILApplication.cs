namespace LFramework
{
    public interface ILApplication : IContainer
    {
        void Register(IProvideService provider, bool force = false);

        bool IsRegistered(IProvideService provider);

        void Terminate();
    }
}