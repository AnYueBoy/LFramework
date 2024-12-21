﻿using System.Collections.Generic;
using UnityEngine;

namespace LFramework
{
    public class SystemBootstrap : MonoBehaviour, IBootstrap
    {
        private readonly List<IProvideService> providerList = new List<IProvideService>();

        public void Bootstrap()
        {
            providerList.AddRange(GetComponentsInChildren<IProvideService>());

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