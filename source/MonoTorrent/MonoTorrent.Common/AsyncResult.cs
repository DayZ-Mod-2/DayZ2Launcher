//
// AsyncResult.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2008 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Threading;

namespace MonoTorrent.Common
{
	public class AsyncResult : IAsyncResult
	{
		#region Member Variables

		private readonly object asyncState;
		private readonly AsyncCallback callback;
		private readonly ManualResetEvent waitHandle;
		private bool completedSyncronously;
		private bool isCompleted;
		private Exception savedException;

		#endregion Member Variables

		#region Properties

		protected internal ManualResetEvent AsyncWaitHandle
		{
			get { return waitHandle; }
		}

		internal AsyncCallback Callback
		{
			get { return callback; }
		}

		protected internal Exception SavedException
		{
			get { return savedException; }
			set { savedException = value; }
		}

		public object AsyncState
		{
			get { return asyncState; }
		}

		WaitHandle IAsyncResult.AsyncWaitHandle
		{
			get { return waitHandle; }
		}

		public bool CompletedSynchronously
		{
			get { return completedSyncronously; }
			protected internal set { completedSyncronously = value; }
		}

		public bool IsCompleted
		{
			get { return isCompleted; }
			protected internal set { isCompleted = value; }
		}

		#endregion Properties

		#region Constructors

		public AsyncResult(AsyncCallback callback, object asyncState)
		{
			this.asyncState = asyncState;
			this.callback = callback;
			waitHandle = new ManualResetEvent(false);
		}

		#endregion Constructors

		#region Methods

		protected internal void Complete()
		{
			Complete(savedException);
		}

		protected internal void Complete(Exception ex)
		{
			// Ensure we only complete once - Needed because in encryption there could be
			// both a pending send and pending receive so if there is an error, both will
			// attempt to complete the encryption handshake meaning this is called twice.
			if (isCompleted)
				return;

			savedException = ex;
			completedSyncronously = false;
			isCompleted = true;
			waitHandle.Set();

			if (callback != null)
				callback(this);
		}

		#endregion Methods
	}
}