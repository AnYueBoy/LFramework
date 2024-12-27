using InjectionCore;

namespace CoreTest
{
    public class ProviderService2 : IProvideService
    {
        public void Register()
        {
            #region 服务不绑定到接口

            // 方式1： 明确自己要注入的类型 性能最佳
            // App.Singleton<Service2>(o => new Service2(o[0] as Service1));
            //
            // // 方式2 ： 由容器分析并反射进行注入
            App.Singleton<Service2>();

            #endregion

            #region 服务绑定到接口

            // 服务绑定到接口
            // App.Singleton<IService2, Service2>();

            #endregion
        }

        public void Init()
        {
            // // 方式1：初始化时明确构建关系
            // App.Make<Service2>(App.Make<Service1>());
            //
            // // 方式2：不需要构建时传入需要的参数
            App.Make<Service2>().Init();

            // App.Make<IService2>().Service2Interface();
        }
    }
}