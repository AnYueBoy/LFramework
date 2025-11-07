using System.Collections;
using System.Collections.Generic;

namespace LFramework
{
    public class CoroutineManager : BaseManager<ICoroutineManager, CoroutineManager>, ICoroutineManager
    {
        private List<ICoroutine> _coroutines;
        private Queue<ICoroutine> _recycle;
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            _coroutines = new List<ICoroutine>();
            _recycle = new Queue<ICoroutine>();
            IsInitialized = true;
        }

        public void Tick(float dt)
        {
            var count = _recycle.Count;
            for (int i = 0; i < count; i++)
            {
                var _coroutine = _recycle.Dequeue();
                _coroutines.Remove(_coroutine);
            }

            count = _coroutines.Count;
            for (int i = 0; i < count; i++)
            {
                var _coroutine = _coroutines[i];
                if (_coroutine.State == CoroutineState.Working)
                {
                    if ((_coroutine as Coroutine).IsDone)
                    {
                        _coroutine.Complete();
                    }
                }
            }
        }

        public void SetRecycle(ICoroutine routine)
        {
            _recycle.Enqueue(routine);
        }

        public void Terminate()
        {
            _coroutines.Clear();
            _recycle.Clear();
        }

        public ICoroutine CreateCoroutine(IEnumerator routine)
        {
            Coroutine coroutine = new Coroutine();
            coroutine._routine = routine;
            PauseCoroutine(coroutine);
            return coroutine;
        }

        public ICoroutine StartCoroutine(IEnumerator routine)
        {
            var coroutine = CreateCoroutine(routine);
            _coroutines.Add(coroutine);
            ResumeCoroutine(coroutine);
            return coroutine;
        }

        public void PauseCoroutine(ICoroutine routine)
        {
            routine.Pause();
        }

        public void ResumeCoroutine(ICoroutine coroutine)
        {
            coroutine.Resume();
        }

        public void StopCoroutine(ICoroutine coroutine)
        {
            coroutine.Complete();
        }

        public void StopCoroutineByTarget(object target)
        {
            if (target == null)
            {
                return;
            }

            for (int i = _coroutines.Count - 1; i >= 0; i--)
            {
                var coroutine = _coroutines[i] as Coroutine;
                if (coroutine == null)
                {
                    continue;
                }

                if (coroutine.Binder != null && coroutine.Binder == target)
                {
                    StopCoroutine(coroutine);
                }
            }
        }
    }
}