using System.Collections;
using System.Collections.Generic;
using LFramework;
using UnityEngine;

[InjectAnalysis]
public class Service1
{
    public Service1()
    {
        Debug.LogError("服务1被构建");
    }

    public void DebugService1()
    {
        Debug.LogError("服务1方法");
    }
}