using System;

namespace MonoTorrent.Client
{
    public class CriticalExceptionEventArgs : EventArgs
    {
        private readonly ClientEngine engine;
        private readonly Exception ex;

        public CriticalExceptionEventArgs(Exception ex, ClientEngine engine)
        {
            if (ex == null)
                throw new ArgumentNullException("ex");
            if (engine == null)
                throw new ArgumentNullException("engine");

            this.engine = engine;
            this.ex = ex;
        }


        public ClientEngine Engine
        {
            get { return engine; }
        }

        public Exception Exception
        {
            get { return ex; }
        }
    }
}