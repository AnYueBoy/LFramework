using System;

namespace LFramework
{
    public sealed class Bindable : IBindable
    {
        public Type ServiceType { get; }
        public Func< object[], object> Concrete { get; }
        public bool IsStatic { get; }

        public Bindable(Type serviceType, Func< object[], object> concrete, bool isStatic)
        {
            ServiceType = serviceType;
            Concrete = concrete;
            IsStatic = isStatic;
        }
    }
}