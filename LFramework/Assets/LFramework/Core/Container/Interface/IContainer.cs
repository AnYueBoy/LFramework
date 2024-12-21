using System;
using System.Collections.Generic;

namespace LFramework
{
    public interface IContainer
    {
        IBindable GetBind(Type serviceType);

        bool HasBind(Type serviceType);

        bool HasInstance(Type serviceType);

        bool IsResolved(Type serviceType);

        bool CanMake(Type serviceType);

        bool IsStatic(Type serviceType);

        IBindable Bind(Type serviceType, Type concrete, bool isStatic);

        IBindable Bind(Type serviceType, Func<object[], object> concrete, bool isStatic);

        /// <summary>
        /// 从容器中解绑 绑定数据并释放服务
        /// </summary>
        void Unbind(Type serviceType);

        void Tag(string tag, params Type[] services);

        List<object> Tagged(string tag);

        object Instance(Type serviceType, object instance);

        /// <summary>
        /// 释放服务 不释放绑定数据
        /// </summary>
        bool Release(Type serviceType);

        void Flush();

        object Make(Type serviceType, params object[] userParams);
    }
}