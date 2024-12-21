using System;

namespace LFramework
{
    public class App
    {
        public static ILApplication That { get; set; }

        public static IBindable Bind<TService>()
        {
            return That.Bind(typeof(TService), typeof(TService), false);
        }

        public static IBindable Bind<TService, TConcrete>()
        {
            return That.Bind(typeof(TService), typeof(TConcrete), false);
        }

        public static IBindable Bind<TService>(Func<object[], object> concrete)
        {
            return That.Bind(typeof(TService), concrete, false);
        }

        public static IBindable Bind<TService>(Func<object> concrete)
        {
            return That.Bind(typeof(TService), _ => concrete.Invoke(), false);
        }

        public static IBindable Singleton<TService>()
        {
            return That.Bind(typeof(TService), typeof(TService), true);
        }

        public static IBindable Singleton<TService, TConcrete>()
        {
            return That.Bind(typeof(TService), typeof(TConcrete), true);
        }

        public static IBindable Singleton<TService>(Func<object[], object> concrete)
        {
            return That.Bind(typeof(TService), concrete, true);
        }

        public static IBindable Singleton<TService>(Func<object> concrete)
        {
            return That.Bind(typeof(TService), _ => concrete.Invoke(), true);
        }

        public static void Unbind<TService>()
        {
            That.Unbind(typeof(TService));
        }

        public static void Tag<TService>(string tag)
        {
            That.Tag(tag, typeof(TService));
        }

        public static object Instance<TService>(object instance)
        {
            return That.Instance(typeof(TService), instance);
        }

        public static bool Release<TService>()
        {
            return That.Release(typeof(TService));
        }

        public static TService Make<TService>(params object[] userParams)
        {
            return (TService)That.Make(typeof(TService), userParams);
        }

        public static void Register(IProvideService provider, bool force = false)
        {
            That.Register(provider, force);
        }

        public static bool IsRegistered(IProvideService provider)
        {
            return That.IsRegistered(provider);
        }

        public static void Terminate()
        {
            That.Terminate();
        }
    }
}