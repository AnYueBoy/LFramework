using System;
using System.Collections.Generic;

namespace InjectionCore
{
    public class LApplication : Container, ILApplication
    {
        private readonly List<IProvideService> loadedProviders;
        private StartProcess process;

        public LApplication()
        {
            loadedProviders = new List<IProvideService>();
            process = StartProcess.Construct;
        }

        public void Bootstrap(params IBootstrap[] bootstraps)
        {
            if (process != StartProcess.Construct)
            {
                throw new Exception("不允许在非引导阶段启动引导程序");
            }

            process = StartProcess.Bootstrap;
            HashSet<IBootstrap> exitedBootstraps = new HashSet<IBootstrap>();
            for (int i = 0; i < bootstraps.Length; i++)
            {
                var bootstrap = bootstraps[i];
                if (bootstrap == null)
                {
                    continue;
                }

                if (exitedBootstraps.Contains(bootstrap))
                {
                    throw new Exception($"引导程序已存在 {bootstrap}");
                }

                exitedBootstraps.Add(bootstrap);

                bootstrap.Bootstrap();
            }
        }

        public void Register(IProvideService provider, bool force = false)
        {
            if (process != StartProcess.Bootstrap)
            {
                throw new Exception("无法在非注册阶段进行注册服务");
            }

            if (IsRegistered(provider))
            {
                if (!force)
                {
                    throw new Exception($"服务提供者 {provider.GetType().FullName} 已被注册");
                }

                loadedProviders.Remove(provider);
            }

            provider.Register();
            loadedProviders.Add(provider);
        }

        public void Init()
        {
            if (process != StartProcess.Bootstrap)
            {
                throw new Exception("无法在非初始化阶段进行初始化服务");
            }

            process = StartProcess.Init;
            for (int i = 0; i < loadedProviders.Count; i++)
            {
                var provider = loadedProviders[i];
                provider.Init();
            }

            process = StartProcess.Running;
        }

        public void Terminate()
        {
            process = StartProcess.Terminate;
            Flush();
            App.That = null;
            process = StartProcess.Terminated;
        }

        public bool IsRegistered(IProvideService provider)
        {
            return loadedProviders.Contains(provider);
        }
    }

    public enum StartProcess
    {
        Construct,
        Bootstrap,
        Init,
        Running,
        Terminate,
        Terminated
    }
}