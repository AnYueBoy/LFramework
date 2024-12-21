﻿using System;
using UnityEngine;

namespace LFramework
{
    public class FrameLauncher : MonoBehaviour
    {
        protected virtual void Awake()
        {
            var application = new LApplication();
            App.That = application;
            var bootstrap = GetComponent<IBootstrap>();
            application.Bootstrap(bootstrap);
            application.Init();
        }
    }
}