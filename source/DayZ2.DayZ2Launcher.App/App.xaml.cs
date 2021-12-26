using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App
{
    public partial class App : Application
    {
        public static EventAggregator Events = new EventAggregator();
        private static string[] _exeArguments;
        private bool _isUncaughtUiThreadException;

        private string _pipeName;

        private void StartPipeServer()
        {
            var pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 2, PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);
            pipeServer.BeginWaitForConnection(NewPipeConnection, pipeServer);
        }

        private void PipeReadCallback(IAsyncResult iar)
        {
            var details = (ReadParams)iar.AsyncState;
            NamedPipeServerStream pipeSvr = details.PipeSvr;
            MemoryStream memStr = details.MessageStr;
            byte[] readBuffer = details.ReadBuffer;

            int numBytes = pipeSvr.EndRead(iar);

            if (numBytes > 0)
                memStr.Write(readBuffer, 0, numBytes);

            if (pipeSvr.IsMessageComplete)
            {
                byte[] memBytes = memStr.ToArray();
                memStr.Dispose();

                string recvStr = Encoding.UTF8.GetString(memBytes, 0, memBytes.Length);
                Events.Publish(new LaunchCommandString(recvStr));

                pipeSvr.Close();
            }
            else
                pipeSvr.BeginRead(readBuffer, 0, readBuffer.Length, PipeReadCallback, details);
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
                    PipeReadCallback, new ReadParams(pipSvr, messageStr, readBuffer));

                StartPipeServer();
            }
            catch (ObjectDisposedException)
            {
            } //happens if no connection happened
        }

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
                Uri actUri = ApplicationDeployment.CurrentDeployment.ActivationUri;
                if (actUri != null)
                    queryString = actUri.Query;
                else
                {
                    try
                    {
                        string[] actData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                        queryString = QueryParamsFromArgs(actData);
                    }
                    catch (Exception)
                    {
                        queryString = null;
                    }
                }
            }
            else
                queryString = QueryParamsFromArgs(_exeArguments);

            if (queryString == null)
                return "";
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

            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                _pipeName = String.Format("DayZLauncher_{{{0}}}_Instance", identity.User);

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
                        byte[] bytesToWrite = Encoding.UTF8.GetBytes(queryString);
                        pipeConn.Write(bytesToWrite, 0, bytesToWrite.Length);
                        pipeConn.WaitForPipeDrain();
                    }
                    secondInstance = true;

                    pipeConn.Close();
                }
                catch (TimeoutException)
                {
                }
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
            MessageBox.Show(
                "It wasn't your fault, but something went really wrong.\r\nThe application will now exit\r\nException Details:\r\n" +
                ex,
                "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void UncaughtUiThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _isUncaughtUiThreadException = true;
            UncaughtException(e.Exception);
        }

        private void UncaughtThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!_isUncaughtUiThreadException)
                UncaughtException(e.ExceptionObject as Exception);
        }

        public sealed class LaunchCommandString
        {
            public string QueryString;

            public LaunchCommandString(string queryString)
            {
                QueryString = queryString;
            }
        }

        private class ReadParams
        {
            public readonly MemoryStream MessageStr;
            public readonly NamedPipeServerStream PipeSvr;
            public readonly byte[] ReadBuffer;

            public ReadParams(NamedPipeServerStream pipeSvr, MemoryStream msgStr, byte[] readBuffer)
            {
                PipeSvr = pipeSvr;
                MessageStr = msgStr;
                ReadBuffer = readBuffer;
            }
        }
    }
}