using System;
using System.Collections.Generic;
using System.Reflection;

namespace LFramework
{
    public class Container : IContainer
    {
        /// <summary>
        /// 容器内的所有绑定数据
        /// </summary>
        private readonly Dictionary<Type, Bindable> bindings;

        /// <summary>
        /// 容器内的所有单例（静态） service-instance
        /// </summary>
        private readonly Dictionary<Type, object> instances;

        /// <summary>
        /// 容器内所有注册的tag映射 tag- list
        /// </summary>
        private readonly Dictionary<string, List<Type>> tags;

        /// <summary>
        /// 已解析的服务的哈希集。
        /// </summary>
        private readonly HashSet<Type> resolved;

        /// <summary>
        /// 容器是否正在重置
        /// </summary>
        private bool flushing;

        /// <summary>
        /// 获取栈内当前正在构建的服务
        /// </summary>
        private Stack<Type> BuildStack { get; }

        public Container(int prime = 64)
        {
            prime = Math.Max(8, prime);
            tags = new Dictionary<string, List<Type>>((int)(prime * 0.25));
            instances = new Dictionary<Type, object>(prime * 4);
            bindings = new Dictionary<Type, Bindable>(prime * 4);
            resolved = new HashSet<Type>();
            BuildStack = new Stack<Type>(32);
            flushing = false;
        }

        public IBindable GetBind(Type serviceType)
        {
            return bindings.GetValueOrDefault(serviceType);
        }

        public bool HasBind(Type serviceType)
        {
            return GetBind(serviceType) != null;
        }

        public bool HasInstance(Type serviceType)
        {
            return instances.ContainsKey(serviceType);
        }

        public bool IsResolved(Type serviceType)
        {
            return resolved.Contains(serviceType) || instances.ContainsKey(serviceType);
        }

        public bool CanMake(Type serviceType)
        {
            if (HasBind(serviceType) || HasInstance(serviceType))
            {
                return true;
            }

            // 服务是否是可构建的类型
            return !IsBasicType(serviceType) && !IsUnableType(serviceType);
        }

        public bool IsStatic(Type serviceType)
        {
            var bind = GetBind(serviceType);
            return bind != null && bind.IsStatic;
        }

        public IBindable Bind(Type serviceType, Type concrete, bool isStatic)
        {
            if (IsUnableType(concrete))
            {
                throw new Exception($"类型{concrete.FullName} 是不可构建的类型");
            }

            return Bind(serviceType, WrapperTypeBuilder(serviceType, concrete), isStatic);
        }

        public IBindable Bind(Type serviceType, Func<object[], object> concrete, bool isStatic)
        {
            if (flushing)
            {
                throw new Exception("容器正在重置");
            }

            if (bindings.ContainsKey(serviceType))
            {
                throw new Exception($"需要添加的绑定的服务{serviceType.FullName} 已经存在");
            }

            if (instances.ContainsKey(serviceType))
            {
                throw new Exception($"单例服务 {serviceType.FullName} 已经存在");
            }

            var bindable = new Bindable(serviceType, concrete, isStatic);
            bindings.Add(serviceType, bindable);

            if (!IsResolved(serviceType))
            {
                return bindable;
            }

            if (isStatic)
            {
                Make(serviceType);
            }

            return bindable;
        }

        public void Unbind(Type serviceType)
        {
            if (flushing)
            {
                throw new Exception("容器正在重置");
            }

            Release(serviceType);
            bindings.Remove(serviceType);
        }

        public void Tag(string tag, params Type[] services)
        {
            if (flushing)
            {
                throw new Exception("容器正在重置");
            }

            if (!tags.TryGetValue(tag, out List<Type> collection))
            {
                tags[tag] = collection = new List<Type>();
            }

            foreach (var service in services ?? Array.Empty<Type>())
            {
                collection.Add(service);
            }
        }

        public List<object> Tagged(string tag)
        {
            if (!tags.TryGetValue(tag, out List<Type> services))
            {
                throw new Exception($"Tag{tag} 不存在");
            }

            List<object> taggedInstance = new List<object>();

            for (int i = 0; i < services.Count; i++)
            {
                var instance = Make(services[i]);
                taggedInstance.Add(instance);
            }

            return taggedInstance;
        }

        public object Instance(Type serviceType, object instance)
        {
            if (flushing)
            {
                throw new Exception("容器正在重置");
            }

            if (instance == null)
            {
                throw new Exception($"实例为空");
            }

            var bindable = GetBind(serviceType);
            if (bindable != null && !bindable.IsStatic)
            {
                throw new Exception($"服务{serviceType.FullName} 不是静态单例");
            }

            if (!instances.TryAdd(serviceType, instance))
            {
                throw new Exception($"实例已被注册为单例 服务名：{serviceType.FullName}");
            }

            return instance;
        }

        public bool Release(Type serviceType)
        {
            if (serviceType == null)
            {
                return false;
            }

            if (!instances.TryGetValue(serviceType, out var instance))
            {
                return false;
            }

            DisposeInstance(instance);
            instances.Remove(serviceType);
            return true;
        }

        public void Flush()
        {
            try
            {
                flushing = true;
                foreach (var kvp in instances)
                {
                    Release(kvp.Key);
                }

                tags.Clear();
                instances.Clear();
                bindings.Clear();
                resolved.Clear();
                BuildStack.Clear();
            }
            finally

            {
                flushing = false;
            }
        }

        public object Make(Type serviceType, params object[] userParams)
        {
            if (instances.TryGetValue(serviceType, out var instance))
            {
                return instance;
            }

            if (BuildStack.Contains(serviceType))
            {
                throw new Exception($"构建服务时产生循环依赖{serviceType.FullName}");
            }

            BuildStack.Push(serviceType);

            try
            {
                var bindable = GetBindFilled(serviceType);
                instance = Build(bindable, userParams);

                if (bindable.IsStatic)
                {
                    Instance(bindable.ServiceType, instance);
                }

                resolved.Add(bindable.ServiceType);
                return instance;
            }
            finally
            {
                BuildStack.Pop();
            }
        }

        #region 非公开

        private object Build(Bindable makeServiceBindable, object[] userParams)
        {
            object instance;
            if (makeServiceBindable.Concrete != null)
            {
                instance = makeServiceBindable.Concrete(userParams);
            }
            else
            {
                instance = CreateInstance(makeServiceBindable, makeServiceBindable.ServiceType,
                    new List<object>(userParams));
            }

            // 属性注入
            AttributeInject(makeServiceBindable, instance);

            return instance;
        }

        private void AttributeInject(Bindable makeBindableService, object instance)
        {
            if (makeBindableService == null)
            {
                return;
            }

            // 获取该实例的所有属性
            var properties = makeBindableService.ServiceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                // 如果属性不可写入或者属性没有使用注入标签则不进行注入
                if (!property.CanWrite || !property.IsDefined(typeof(InjectAttribute), false))
                {
                    continue;
                }

                // 属性需要注入的服务类型
                var needServiceType = property.PropertyType;

                object needInstance;
                if (needServiceType.IsClass || needServiceType.IsInterface)
                {
                    needInstance = ResolveAttrClass(needServiceType);
                }
                else
                {
                    throw new Exception($"{needServiceType.FullName} 不可生成");
                }

                if (!CanInject(needServiceType, needInstance))
                {
                    throw new Exception(
                        $"{makeBindableService.ServiceType.FullName} 所依赖的注入属性类型：{needServiceType} 不是可注入类型");
                }

                // 属性依赖注入
                property.SetValue(instance, needInstance, null);
            }
        }

        private object ResolveAttrClass(Type needService)
        {
            if (ResolveInstance(needService, out var instance))
            {
                return instance;
            }

            throw new Exception($"属性注入时 {needService.FullName} 不可生成注入");
        }

        private object ResolveClass(Type needService, ParameterInfo baseParam)
        {
            if (ResolveInstance(needService, out var instance))
            {
                return instance;
            }

            if (baseParam.IsOptional)
            {
                return baseParam.DefaultValue;
            }

            throw new Exception($"构造函数注入时 {needService.FullName} 不可生成注入");
        }

        private bool ResolveInstance(Type needServiceType, out object instance)
        {
            instance = null;
            if (!CanMake(needServiceType))
            {
                return false;
            }

            instance = Make(needServiceType);
            return ChangeTypeByInstance(ref instance, needServiceType);
        }

        private bool CanInject(Type type, object instance)
        {
            // 实例是否是对应的类型或对应类型的派生类
            return instance == null || type.IsInstanceOfType(instance);
        }

        private Func<object[], object> WrapperTypeBuilder(Type serviceType, Type concrete)
        {
            var filledBindable = GetBindFilled(serviceType);
            return userParams => CreateInstance(filledBindable, concrete, new List<object>(userParams));
        }

        private object CreateInstance(Bindable makeServiceBindable, Type makeServiceType, List<object> userParams)
        {
            if (IsUnableType(makeServiceType))
            {
                return null;
            }

            userParams = GetConstructorsInjectParams(makeServiceBindable, makeServiceType, userParams);

            // 如果参数不存在，那么在反射时无需写入参数  可获得更好的性能。
            if (userParams == null || userParams.Count <= 0)
            {
                return Activator.CreateInstance(makeServiceType);
            }

            return Activator.CreateInstance(makeServiceType, userParams.ToArray());
        }

        private List<object> GetConstructorsInjectParams(Bindable makeServiceBindable, Type makeServiceType,
            List<object> userParams)
        {
            var constructors = makeServiceType.GetConstructors();
            if (constructors.Length <= 0)
            {
                return new List<object>();
            }

            foreach (var con in constructors)
            {
                return GetDependencies(makeServiceBindable, con.GetParameters(), userParams);
            }

            throw new Exception($"进行构造函数的注入时失败");
        }

        private List<object> GetDependencies(Bindable makeServiceBindable, ParameterInfo[] baseParams,
            List<object> userParams)
        {
            List<object> result = new List<object>();

            if (baseParams.Length <= 0)
            {
                return result;
            }

            for (int i = 0; i < baseParams.Length; i++)
            {
                var baseParam = baseParams[i];
                // 如果依赖项为 object 或 object[] 则压缩注入用户参数
                var param = GetCompactInjectUserParams(baseParam, userParams);

                // 选择合适的用户参数进行注入
                param ??= GetDependenciesFromUserParams(baseParam, userParams);

                var needServiceType = baseParam.ParameterType;
                if (param == null)
                {
                    // 尝试从容器中将参数做注入
                    param = ResolveClass(needServiceType, baseParam);
                }

                if (!CanInject(needServiceType, param))
                {
                    var error =
                        $"服务{makeServiceBindable.ServiceType} 参数的注入类型必须是{baseParam.ParameterType}, 但是实例是{param?.GetType()} 类型.";
                    error += $" 构建的服务是{needServiceType}";
                    throw new Exception(error);
                }

                result.Add(param);
            }

            return result;
        }

        private object GetDependenciesFromUserParams(ParameterInfo baseParam, List<object> userParams)
        {
            if (userParams == null)
            {
                return null;
            }

            if (userParams.Count > 255)
            {
                throw new Exception("用户参数的数量必须小于等于255");
            }

            var paramType = baseParam.ParameterType;

            for (int i = 0; i < userParams.Count; i++)
            {
                var userParam = userParams[i];
                // 用户参数的实例能否转换为形参类型
                if (ChangeTypeByInstance(ref userParam, paramType))
                {
                    userParams.RemoveAt(i);
                    return userParam;
                }
            }

            return null;
        }

        private bool ChangeTypeByInstance(ref object instance, Type conversionType)
        {
            if (instance == null || conversionType.IsInstanceOfType(instance))
            {
                return true;
            }

            if (instance is IConvertible && typeof(IConvertible).IsAssignableFrom(conversionType))
            {
                instance = Convert.ChangeType(instance, conversionType);
                return true;
            }

            return false;
        }

        private object GetCompactInjectUserParams(ParameterInfo baseParams, List<object> userParams)
        {
            if (!IsCanCompactInjectUserParams(baseParams, userParams))
            {
                return null;
            }

            var paramType = baseParams.ParameterType;
            try
            {
                if (paramType == typeof(object) && userParams != null && userParams.Count == 1)
                {
                    return userParams[0];
                }

                return userParams;
            }
            finally
            {
                userParams.Clear();
            }
        }

        /// <summary>
        /// 是否可以压缩用户参数
        /// </summary>
        private bool IsCanCompactInjectUserParams(ParameterInfo baseParam, List<object> userParams)
        {
            if (userParams == null || userParams.Count <= 0)
            {
                return false;
            }

            var paramType = baseParam.ParameterType;
            if (paramType == typeof(object[]) || paramType == typeof(object))
            {
                return true;
            }

            return false;
        }


        private Bindable GetBindFilled(Type serviceType)
        {
            if (bindings.TryGetValue(serviceType, out var bindable))
            {
                return bindable;
            }

            return MakeEmptyBindable(serviceType);
        }

        private Bindable MakeEmptyBindable(Type serviceType)
        {
            return new Bindable(serviceType, null, false);
        }

        private void DisposeInstance(object instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// 确定指定的类型是否是容器的默认基本类型。
        /// </summary>
        protected virtual bool IsBasicType(Type type)
        {
            // IsPrimitive 判断是否为基元类型
            return type == null || type.IsPrimitive || type == typeof(string);
        }

        /// <summary>
        /// 确定指定的类型是否是无法生成的类型。
        /// </summary>
        protected virtual bool IsUnableType(Type type)
        {
            return type == null || type.IsAbstract || type.IsInterface || type.IsArray || type.IsEnum ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        #endregion
    }
}