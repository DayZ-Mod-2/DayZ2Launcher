using System;
using System.Threading;

namespace Mono.Nat
{
    internal class AsyncResult : IAsyncResult
    {
        private readonly object asyncState;
        private readonly AsyncCallback callback;
        private readonly ManualResetEvent waitHandle;
        private bool isCompleted;
        private Exception storedException;

        public AsyncResult(AsyncCallback callback, object asyncState)
        {
            this.callback = callback;
            this.asyncState = asyncState;
            waitHandle = new ManualResetEvent(false);
        }

        public ManualResetEvent AsyncWaitHandle
        {
            get { return waitHandle; }
        }

        public Exception StoredException
        {
            get { return storedException; }
        }

        public object AsyncState
        {
            get { return asyncState; }
        }

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get { return waitHandle; }
        }

        public bool CompletedSynchronously { get; protected internal set; }

        public bool IsCompleted
        {
            get { return isCompleted; }
            protected internal set { isCompleted = value; }
        }

        public void Complete()
        {
            Complete(storedException);
        }

        public void Complete(Exception ex)
        {
            storedException = ex;
            isCompleted = true;
            waitHandle.Set();

            if (callback != null)
                callback(this);
        }
    }
}