using System;

namespace ImageResize
{
    public class DisponseDelegate : IDisposable
    {
        private readonly Action _disposeDelegate;

        public DisponseDelegate(Action disposeDelegate)
        {
            _disposeDelegate = disposeDelegate;
        }
        public void Dispose()
        {
            _disposeDelegate();
        }
    }
}
