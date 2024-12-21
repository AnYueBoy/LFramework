using System;
using UnityEngine;

namespace LFramework
{
    public abstract class FrameLauncher : MonoBehaviour
    {
        protected virtual void Awake()
        {
            var application = new LApplication();
            App.That = application;
            var bootstrap = GetComponent<IBootstrap>();
            bootstrap.Bootstrap();
            application.Init();
        }
    }
}