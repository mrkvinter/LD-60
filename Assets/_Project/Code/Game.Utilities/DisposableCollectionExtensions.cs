using System;

namespace Game.Utilities
{
    public static class DisposableCollectionExtensions
    {
        public static void AddTo(this IDisposable disposable, DisposableCollection collection)
        {
            collection.Add(disposable);
        }
    }
}