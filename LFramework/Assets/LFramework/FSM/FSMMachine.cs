using System;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.FSM
{
    public class FSMMachine<TOwner> : IFSM<TOwner> where TOwner : IOwner<TOwner>
    {
        private IFSMState<TOwner> currentState;

        private readonly Dictionary<Type, IFSMState<TOwner>> states = new Dictionary<Type, IFSMState<TOwner>>();

        private TOwner owner;

        public FSMMachine(TOwner owner, IEnumerable<IFSMState<TOwner>> iEnumerator)
        {
            this.owner = owner;
            states = new Dictionary<Type, IFSMState<TOwner>>();
            foreach (var state in iEnumerator)
            {
                var type = state.GetType();
                states.Add(type, state);
            }
        }

        public void OnUpdate(float dt)
        {
            if (currentState == null)
            {
                return;
            }

            currentState.OnUpdate(owner, dt);
        }

        public void ChangeState<TState>(bool isForce = false) where TState : class, IFSMState<TOwner>
        {
            var state = GetState<TState>();
            ChangeState(state, isForce);
        }

        private void ChangeState(IFSMState<TOwner> state, bool isForce = false)
        {
            if (currentState != null)
            {
                if (!isForce && state == currentState)
                {
                    return;
                }

                currentState.OnExit(owner);
            }

            currentState = state;
            currentState.OnEnter(owner);
        }

        public bool IsInState<TState>() where TState : class, IFSMState<TOwner>
        {
            if (currentState == null)
            {
                return false;
            }

            var stateType = typeof(TState);
            var curStateType = currentState.GetType();
            return stateType == curStateType;
        }

        public TState GetState<TState>() where TState : class, IFSMState<TOwner>
        {
            var stateType = typeof(TState);
            if (!states.TryGetValue(stateType, out var toState))
            {
                Debug.LogError($"对应状态不存在: {stateType}");
                return null;
            }

            return toState as TState;
        }
    }
}