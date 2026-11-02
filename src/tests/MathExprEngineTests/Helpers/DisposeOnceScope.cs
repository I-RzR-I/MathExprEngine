using System;
using System.Threading;

namespace MathExprEngineTests.Helpers
{
    internal sealed class DisposeOnceScope : IDisposable
    {
        private Action _onDispose;

        public DisposeOnceScope(Action onDispose) => _onDispose = onDispose;

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _onDispose, null);
            action?.Invoke();
        }
    }
}
