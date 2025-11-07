using System;
using System.Collections;

namespace LFramework
{
    public class Coroutine : YieldInstruction, ICoroutine
    {
        private Coroutine _innerAction;
        private CoroutineState _state;
        internal IEnumerator _routine;
        public object Binder { get; private set; }
        internal event Action onCompleted;

        public CoroutineState State => _state;

        /// <summary>
        /// 协程完成时回调
        /// </summary>
        public void Complete()
        {
            if (_innerAction != null)
            {
                _innerAction.Complete();
            }

            if (onCompleted != null)
            {
                onCompleted();
            }

            onCompleted = null;
            _innerAction = null;
            _routine = null;
            _state = CoroutineState.Reset;
            CoroutineManager.I.SetRecycle(this);
        }

        public CoroutineAwaiter GetAwaiter()
        {
            return new CoroutineAwaiter(this);
        }

        protected override bool IsCompleted()
        {
            if (_state != CoroutineState.Working)
            {
                return false;
            }

            if (_innerAction == null)
            {
                if (!_routine.MoveNext())
                {
                    return true;
                }

                if (_routine.Current != null)
                {
                    IEnumerator enumerator = null;
                    if (_routine.Current is YieldInstruction instruction)
                    {
                        enumerator = instruction.AsEnumerator();
                    }
                    else if (_routine.Current is IEnumerator)
                    {
                        enumerator = _routine.Current as IEnumerator;
                    }

                    if (enumerator != null)
                    {
                        _innerAction = CoroutineManager.I.CreateCoroutine(enumerator) as Coroutine;
                        _innerAction.Resume();
                    }
                }
            }

            if (_innerAction != null && _innerAction.IsDone)
            {
                _innerAction.Complete();
                _innerAction = null;
            }

            return false;
        }

        public void Pause()
        {
            _state = CoroutineState.Yield;
        }

        public void Resume()
        {
            _state = CoroutineState.Working;
        }

        public void Bind(object target)
        {
            Binder = target;
        }
    }
}