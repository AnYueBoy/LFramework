using LFramework;

namespace CoreTest
{
    public class ProviderService1 : IProvideService
    {
        public void Register()
        {
            // App.Bind<Service1>(() => new Service1());
        }

        public void Init()
        {

        }
    }
}