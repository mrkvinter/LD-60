using System;
using System.Collections.Generic;

namespace StateMachine
{
    public interface IState
    {
        void Enter();
        void Exit();
        void Tick(float dt);
    }

    public interface IStateMachine
    {
        void Enter<T>() where T : IState;
    }

    public class StateMachine : IStateMachine
    {
        private Dictionary<Type, IState> states = new();
        private IState currentState;

        public IState Current => currentState;

        public void Add<T>(IState state) where T : IState
        {
            states[typeof(T)] = state;
        }

        public void Enter<T>() where T : IState
        {
            currentState?.Exit();
            currentState = states[typeof(T)];
            currentState.Enter();
        }

        public void Exit()
        {
            var state = currentState;
            currentState = null;
            state?.Exit();
        }

        public void Tick(float dt)
        {
            currentState?.Tick(dt);
        }
    }
}