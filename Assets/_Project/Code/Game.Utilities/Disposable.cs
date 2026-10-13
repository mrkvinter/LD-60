using System;

namespace Game.Utilities
{
    public static class Disposable
    {
        public static IDisposable Empty { get; } = new EmptyDisposable();

        public static IDisposable Create(Action onDispose) => new AnonymousDisposable(onDispose);

        private sealed class AnonymousDisposable : IDisposable
        {
            private Action onDispose;

            public AnonymousDisposable(Action onDispose)
            {
                this.onDispose = onDispose;
            }

            public void Dispose()
            {
                var action = onDispose;
                onDispose = null;
                action?.Invoke();
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
