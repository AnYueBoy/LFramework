using System;

namespace InjectionCore
{
    public static class ContainerExtension
    {
        public static IBindable Bind<TService>(this IContainer container)
        {
            return container.Bind(typeof(TService), typeof(TService), false);
        }

        public static IBindable Bind<TService, TConcrete>(this IContainer container)
        {
            return container.Bind(typeof(TService), typeof(TConcrete), false);
        }

        public static IBindable Bind<TService>(this IContainer container, Func<object[], object> concrete)
        {
            return container.Bind(typeof(TService), concrete, false);
        }

        public static IBindable Bind<TService>(this IContainer container, Func<object> concrete)
        {
            return container.Bind(typeof(TService), _ => concrete.Invoke(), false);
        }

        public static IBindable Singleton<TService>(this IContainer container)
        {
            return container.Bind(typeof(TService), typeof(TService), true);
        }

        public static IBindable Singleton<TService, TConcrete>(this IContainer container)
        {
            return container.Bind(typeof(TService), typeof(TConcrete), true);
        }

        public static IBindable Singleton<TService>(this IContainer container, Func<object[], object> concrete)
        {
            return container.Bind(typeof(TService), concrete, true);
        }

        public static IBindable Singleton<TService>(this IContainer container, Func<object> concrete)
        {
            return container.Bind(typeof(TService), _ => concrete.Invoke(), true);
        }

        public static void Unbind<TService>(this IContainer container)
        {
            container.Unbind(typeof(TService));
        }

        public static void Tag<TService>(this IContainer container, string tag)
        {
            container.Tag(tag, typeof(TService));
        }

        public static object Instance<TService>(this IContainer container, object instance)
        {
            return container.Instance(typeof(TService), instance);
        }

        public static bool Release<TService>(this IContainer container)
        {
            return container.Release(typeof(TService));
        }

        public static TService Make<TService>(this IContainer container, params object[] userParams)
        {
            return (TService)container.Make(typeof(TService), userParams);
        }
    }
}