using System.Collections.Generic;
using CoreTest;
using UnityEngine;
using UnityEngine.Serialization;

namespace LFramework
{
    public class SystemBootstrap : MonoBehaviour, IBootstrap
    {
        private readonly List<IProvideService> providerList = new List<IProvideService>()
        {
            new ProviderService1(),
            new ProviderService2()
        };

        [SerializeField] private List<GameObject> injectMonoInstances;

        public void Bootstrap()
        {
            for (int i = 0; i < injectMonoInstances.Count; i++)
            {
                var instance = injectMonoInstances[i];
                providerList.Add(instance.GetComponent<IProvideService>());
            }

            for (int i = 0; i < providerList.Count; i++)
            {
                var provider = providerList[i];
                if (provider == null)
                {
                    continue;
                }

                if (App.IsRegistered(provider))
                {
                    continue;
                }

                App.Register(provider);
            }
        }
    }
}