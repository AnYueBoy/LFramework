using System;

namespace InjectionCore
{
    public interface IBindable
    {
        Type ServiceType { get; }

        Func<object[], object> Concrete { get; }

        bool IsStatic { get; }
    }
}