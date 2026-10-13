using System;

namespace Game.Utilities
{
    internal sealed class Unsubscriber<TDelegate> : IDisposable
    {
        private Action<TDelegate> remove;
        private TDelegate handler;

        public Unsubscriber(Action<TDelegate> remove, TDelegate handler)
        {
            this.remove = remove;
            this.handler = handler;
        }

        public void Dispose()
        {
            if (remove == null) return;

            remove(handler);
            remove = null;
            handler = default;
        }
    }

    public abstract class EventSubscriberBase<TDelegate> where TDelegate : Delegate
    {
        private readonly TDelegate dummyEvent;

        protected EventSubscriberBase(TDelegate dummyEvent)
        {
            this.dummyEvent = dummyEvent;

            Event = dummyEvent;
        }

        protected TDelegate Event { get; private set; }

        public IDisposable Subscribe(TDelegate handler)
        {
            Event = ReferenceEquals(Event, dummyEvent)
                ? handler
                : (TDelegate)Delegate.Combine(Event, handler);

            return new Unsubscriber<TDelegate>(Unsubscribe, handler);
        }

        private void Unsubscribe(TDelegate handler)
        {
            Event = (TDelegate)Delegate.Remove(Event, handler) ?? dummyEvent;
        }
    }

    public sealed class EventSubscriber : EventSubscriberBase<Action>, IEventSubscriber
    {
        private static readonly Action dummyEvent = () => {};

        public EventSubscriber() : base(dummyEvent)
        {
        }

        public void Fire() => Event();
    }

    public sealed class EventSubscriber<T1>
        : EventSubscriberBase<Action<T1>>, IEventSubscriber<T1>
    {
        private static readonly Action<T1> dummyEvent = _ => {};

        public EventSubscriber() : base(dummyEvent)
        {
        }

        public void Fire(T1 arg1) => Event(arg1);
    }

    public sealed class EventSubscriber<T1, T2>
        : EventSubscriberBase<Action<T1, T2>>, IEventSubscriber<T1, T2>
    {
        private static readonly Action<T1, T2> dummyEvent = (_, _) => {};

        public EventSubscriber() : base(dummyEvent)
        {
        }

        public void Fire(T1 arg1, T2 arg2) => Event(arg1, arg2);
    }

    public sealed class EventSubscriber<T1, T2, T3>
        : EventSubscriberBase<Action<T1, T2, T3>>, IEventSubscriber<T1, T2, T3>
    {
        private static readonly Action<T1, T2, T3> dummyEvent = (_, _, _) => {};

        public EventSubscriber() : base(dummyEvent)
        {
        }

        public void Fire(T1 arg1, T2 arg2, T3 arg3) => Event(arg1, arg2, arg3);
    }

    public sealed class EventSubscriber<T1, T2, T3, T4>
        : EventSubscriberBase<Action<T1, T2, T3, T4>>, IEventSubscriber<T1, T2, T3, T4>
    {
        private static readonly Action<T1, T2, T3, T4> dummyEvent = (_, _, _, _) => {};

        public EventSubscriber() : base(dummyEvent)
        {
        }

        public void Fire(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Event(arg1, arg2, arg3, arg4);
    }

    public sealed class EventSubscriber<T1, T2, T3, T4, T5>
        : EventSubscriberBase<Action<T1, T2, T3, T4, T5>>, IEventSubscriber<T1, T2, T3, T4, T5>
    {
        private static readonly Action<T1, T2, T3, T4, T5> dummyEvent = (_, _, _, _, _) => {};

        public EventSubscriber() : base(dummyEvent)
        {
        }

        public void Fire(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => Event(arg1, arg2, arg3, arg4, arg5);
    }
}
