using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using zombiesnu.DayZeroLauncher.App.Core;
using zombiesnu.DayZeroLauncher.App.Ui.Controls;
using Microsoft.Win32;
using System.IO.Pipes;
using System.Deployment.Application;
using System.Text;
using System.Collections.Generic;

namespace zombiesnu.DayZeroLauncher.App
{
	public partial class App : Application
	{
		public static EventAggregator Events = new EventAggregator();

		public sealed class LaunchCommandString
		{
			public LaunchCommandString(string queryString)
			{
				QueryString = queryString;
			}

			public string QueryString;
		}

		private string _pipeName;
		private void StartPipeServer()
		{
			var pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 2, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			pipeServer.BeginWaitForConnection(new AsyncCallback(NewPipeConnection), pipeServer);
		}

		private class ReadParams
		{
			public ReadParams(NamedPipeServerStream pipeSvr, MemoryStream msgStr, byte[] readBuffer)
			{
				this.PipeSvr = pipeSvr;
				this.MessageStr = msgStr;
				this.ReadBuffer = readBuffer;
			}

			public NamedPipeServerStream PipeSvr;
			public MemoryStream MessageStr;
			public byte[] ReadBuffer;
		}

		private void PipeReadCallback(IAsyncResult iar)
		{
			var details = (ReadParams)iar.AsyncState;
			var pipeSvr = details.PipeSvr;
			var memStr = details.MessageStr;
			var readBuffer = details.ReadBuffer;

			var numBytes = pipeSvr.EndRead(iar);

			if (numBytes > 0)
				memStr.Write(readBuffer,0,numBytes);

			if (pipeSvr.IsMessageComplete)
			{
				var memBytes = memStr.ToArray();
				memStr.Dispose();

				string recvStr = Encoding.UTF8.GetString(memBytes, 0, memBytes.Length);
				Events.Publish(new LaunchCommandString(recvStr));

				pipeSvr.Close();
			}
			else
				pipeSvr.BeginRead(readBuffer,0,readBuffer.Length,new AsyncCallback(PipeReadCallback),details);
		}

		private void NewPipeConnection(IAsyncResult iar)
		{
			var pipSvr = (NamedPipeServerStream)iar.AsyncState;
			try
			{
				pipSvr.EndWaitForConnection(iar);

				var messageStr = new MemoryStream(4096);
				var readBuffer = new byte[4096];
				pipSvr.BeginRead(readBuffer, 0, readBuffer.Length,
					new AsyncCallback(PipeReadCallback), new ReadParams(pipSvr, messageStr, readBuffer));

				StartPipeServer();
			}
			catch (ObjectDisposedException) { return; } //happens if no connection happened
		}

		private static string[] _exeArguments = null;
		private static string QueryParamsFromArgs(string[] whichArgs)
		{
			var args = new List<string>();
			foreach (string arg in whichArgs)
			{
				string[] tokens = arg.Split('=');
				for (int i = 0; i < tokens.Length; i++)
					tokens[i] = Uri.EscapeDataString(tokens[i]);

				args.Add(string.Join("=", tokens));
			}

			return string.Join("&", args);
		}
		public static string GetQueryParams()
		{
			string queryString = null;
			if (ApplicationDeployment.IsNetworkDeployed)
			{
				var actUri = ApplicationDeployment.CurrentDeployment.ActivationUri;
				if (actUri != null)
					queryString = actUri.Query;
				else
				{
					try
					{
						var actData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
						queryString = QueryParamsFromArgs(actData);
					}
					catch (Exception) { queryString = null; }
				}
			}
			else
				queryString = QueryParamsFromArgs(_exeArguments);

			if (queryString == null)
				return "";
			else
				return queryString;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			if (e.Args == null)
				_exeArguments = new string[0];
			else
				_exeArguments = e.Args;

			AppDomain.CurrentDomain.UnhandledException += UncaughtThreadException;
			DispatcherUnhandledException += UncaughtUiThreadException;

			using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
				_pipeName = String.Format("DayZeroLauncher_{{{0}}}_Instance",identity.User.ToString());

			bool secondInstance = false;
			using (var pipeConn = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out))
			{
				try
				{
					const int timeoutMs = 100;
					pipeConn.Connect(timeoutMs);
					pipeConn.ReadMode = PipeTransmissionMode.Message;

					string queryString = GetQueryParams();
					if (!string.IsNullOrEmpty(queryString))
					{
						var bytesToWrite = Encoding.UTF8.GetBytes(queryString);
						pipeConn.Write(bytesToWrite, 0, bytesToWrite.Length);
						pipeConn.WaitForPipeDrain();
					}					
					secondInstance = true;

					pipeConn.Close();
				}
				catch (TimeoutException) {}
			}

			if (secondInstance) //already sent message to pipe
			{
				Shutdown();
				return;
			}
			
			//we are the only app, start the server
			StartPipeServer();

			LocalMachineInfo.Current.Update();
			base.OnStartup(e);
		}

		private void UncaughtException(Exception ex)
		{
			MessageBox.Show("It wasn't your fault, but something went really wrong.\r\nThe application will now exit\r\nException Details:\r\n" + ex.ToString(),
							"Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private bool _isUncaughtUiThreadException;
		private void UncaughtUiThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			_isUncaughtUiThreadException = true;
			UncaughtException(e.Exception);
		}

		private void UncaughtThreadException(object sender, UnhandledExceptionEventArgs e)
		{
			if(!_isUncaughtUiThreadException)
				UncaughtException(e.ExceptionObject as Exception);
		}
	}
}
