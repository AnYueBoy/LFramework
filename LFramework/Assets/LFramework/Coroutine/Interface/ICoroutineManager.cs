using System.Collections;

namespace LFramework
{
    public interface ICoroutineManager
    {
        public bool IsInitialized { get; }
        void Initialize();

        void Tick(float dt);

        void Terminate();

        void SetRecycle(ICoroutine routine);

        /// <summary>
        /// 创建协程(不运行)
        /// </summary>
        ICoroutine CreateCoroutine(IEnumerator routine);

        /// <summary>
        /// 开启协程
        /// </summary>
        ICoroutine StartCoroutine(IEnumerator routine);

        /// <summary>
        /// 暂停协程
        /// </summary>
        void PauseCoroutine(ICoroutine routine);

        /// <summary>
        /// 恢复协程
        /// </summary>
        void ResumeCoroutine(ICoroutine coroutine);

        /// <summary>
        /// 关闭协程
        /// </summary>
        void StopCoroutine(ICoroutine coroutine);

        void StopCoroutineByTarget(object target);
    }
}