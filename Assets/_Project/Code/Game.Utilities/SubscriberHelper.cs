using System;

namespace Game.Utilities
{
    public interface IEventSubscriber
    {
        IDisposable Subscribe(Action handler);
    }

    public interface IEventSubscriber<out T1>
    {
        IDisposable Subscribe(Action<T1> handler);
    }

    public interface IEventSubscriber<out T1, out T2>
    {
        IDisposable Subscribe(Action<T1, T2> handler);
    }

    public interface IEventSubscriber<out T1, out T2, out T3>
    {
        IDisposable Subscribe(Action<T1, T2, T3> handler);
    }

    public interface IEventSubscriber<out T1, out T2, out T3, out T4>
    {
        IDisposable Subscribe(Action<T1, T2, T3, T4> handler);
    }

    public interface IEventSubscriber<out T1, out T2, out T3, out T4, out T5>
    {
        IDisposable Subscribe(Action<T1, T2, T3, T4, T5> handler);
    }

    public static class SubscriberHelper
    {
        public static IEventSubscriber FromEvent(
            Action<Action> add, Action<Action> remove)
            => new EventBridge(add, remove);

        public static IEventSubscriber<T1> FromEvent<T1>(
            Action<Action<T1>> add, Action<Action<T1>> remove)
            => new EventBridge<T1>(add, remove);

        public static IEventSubscriber<T1, T2> FromEvent<T1, T2>(
            Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove)
            => new EventBridge<T1, T2>(add, remove);

        public static IEventSubscriber<T1, T2, T3> FromEvent<T1, T2, T3>(
            Action<Action<T1, T2, T3>> add, Action<Action<T1, T2, T3>> remove)
            => new EventBridge<T1, T2, T3>(add, remove);

        public static IEventSubscriber<T1, T2, T3, T4> FromEvent<T1, T2, T3, T4>(
            Action<Action<T1, T2, T3, T4>> add, Action<Action<T1, T2, T3, T4>> remove)
            => new EventBridge<T1, T2, T3, T4>(add, remove);

        public static IEventSubscriber<T1, T2, T3, T4, T5> FromEvent<T1, T2, T3, T4, T5>(
            Action<Action<T1, T2, T3, T4, T5>> add, Action<Action<T1, T2, T3, T4, T5>> remove)
            => new EventBridge<T1, T2, T3, T4, T5>(add, remove);

        private abstract class EventBridgeCore<TDelegate>
        {
            private readonly Action<TDelegate> add;
            private readonly Action<TDelegate> remove;

            protected EventBridgeCore(Action<TDelegate> add, Action<TDelegate> remove)
            {
                this.add = add;
                this.remove = remove;
            }

            protected IDisposable SubscribeCore(TDelegate handler)
            {
                add(handler);
                return new Unsubscriber<TDelegate>(remove, handler);
            }
        }

        private sealed class EventBridge
            : EventBridgeCore<Action>, IEventSubscriber
        {
            public EventBridge(Action<Action> add, Action<Action> remove)
                : base(add, remove)
            {
            }

            public IDisposable Subscribe(Action handler) => SubscribeCore(handler);
        }

        private sealed class EventBridge<T1>
            : EventBridgeCore<Action<T1>>, IEventSubscriber<T1>
        {
            public EventBridge(Action<Action<T1>> add, Action<Action<T1>> remove)
                : base(add, remove)
            {
            }

            public IDisposable Subscribe(Action<T1> handler) => SubscribeCore(handler);
        }

        private sealed class EventBridge<T1, T2>
            : EventBridgeCore<Action<T1, T2>>, IEventSubscriber<T1, T2>
        {
            public EventBridge(Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove)
                : base(add, remove)
            {
            }

            public IDisposable Subscribe(Action<T1, T2> handler) => SubscribeCore(handler);
        }

        private sealed class EventBridge<T1, T2, T3>
            : EventBridgeCore<Action<T1, T2, T3>>, IEventSubscriber<T1, T2, T3>
        {
            public EventBridge(Action<Action<T1, T2, T3>> add, Action<Action<T1, T2, T3>> remove)
                : base(add, remove)
            {
            }

            public IDisposable Subscribe(Action<T1, T2, T3> handler) => SubscribeCore(handler);
        }

        private sealed class EventBridge<T1, T2, T3, T4>
            : EventBridgeCore<Action<T1, T2, T3, T4>>, IEventSubscriber<T1, T2, T3, T4>
        {
            public EventBridge(
                Action<Action<T1, T2, T3, T4>> add,
                Action<Action<T1, T2, T3, T4>> remove)
                : base(add, remove)
            {
            }

            public IDisposable Subscribe(Action<T1, T2, T3, T4> handler) => SubscribeCore(handler);
        }

        private sealed class EventBridge<T1, T2, T3, T4, T5>
            : EventBridgeCore<Action<T1, T2, T3, T4, T5>>,
              IEventSubscriber<T1, T2, T3, T4, T5>
        {
            public EventBridge(
                Action<Action<T1, T2, T3, T4, T5>> add,
                Action<Action<T1, T2, T3, T4, T5>> remove)
                : base(add, remove)
            {
            }

            public IDisposable Subscribe(Action<T1, T2, T3, T4, T5> handler)
                => SubscribeCore(handler);
        }
    }
}