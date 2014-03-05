﻿using MonoTorrent.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using zombiesnu.DayZeroLauncher.App.Core;
using SharpCompress.Reader;
using SharpCompress.Common;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;

namespace zombiesnu.DayZeroLauncher.App.Ui
{
	public class LaunchProgressViewModel : BindableBase
	{
		private class InstallersMeta
		{
			public class InstallerInfo
			{
				[JsonProperty("version")]
				public string Version = null;

				[JsonProperty("archive")]
				public HashWebClient.RemoteFileInfo Archive = null;

				public class ExecutableInfo
				{
					[JsonProperty("executable")]
					public string FileName = null;

					[JsonProperty("sha1")]
					public string Sha1Hash = null;

					[JsonProperty("runasCode")]
					public int? RunAsCode = new Nullable<int>();
				}

				[JsonProperty("verifier")]
				public ExecutableInfo Verifier = null;

				[JsonProperty("copier")]
				public ExecutableInfo Copier = null;

				public static string GetArchiveFileName(string versionString)
				{
					return Path.Combine(UserSettings.InstallersPath, versionString + ".zip");
				}
				public string GetArchiveFileName()
				{
					return GetArchiveFileName(Version);
				}
				public static string GetDirectoryName(string versionString)
				{
					return Path.Combine(UserSettings.InstallersPath, versionString);
				}
				public string GetDirectoryName()
				{
					return GetDirectoryName(Version);
				}
			}

			[JsonProperty("installers")]
			public List<InstallerInfo> Installers = null;

			public static string GetFileName()
			{
				string installersFileName = Path.Combine(UserSettings.InstallersPath, "index.json");
				return installersFileName;
			}

			public static InstallersMeta LoadFromFile(string fileFullPath)
			{
				InstallersMeta modsInfo = JsonConvert.DeserializeObject<InstallersMeta>(File.ReadAllText(fileFullPath));
				return modsInfo;
			}
		}

		private MetaGameType gameType = null;
		private IEnumerable<MetaAddon> addOns = null;
		private IEnumerable<InstallersMeta.InstallerInfo> installers = null;

		protected class InstallerNotFound : Exception
		{
			public InstallerNotFound(string installer) : base("Could not find installer '" + installer + "'")
			{
				InstallerName = installer;
			}

			public string InstallerName { get; set; }
		}

		protected void HandleException(string topText, string txtMsg=null)
		{
			UpperProgressText = topText;
			UpperProgressLimit = UpperProgressValue = 0;
			LowerProgressLimit = LowerProgressValue = 0;
			LowerProgressText = txtMsg;

			Closeable = true;
		}

		protected void HandleException(string topText, Exception ex)
		{			
			if (ex != null)
				HandleException(topText,ex.Message);
			else
				HandleException(topText);
		}

		protected bool HandlePossibleError(string topText, AsyncCompletedEventArgs args)
		{
			string errorMsg = null;
			if (args.Cancelled)
				errorMsg = "Async operation cancelled";
			else if (args.Error != null)
				errorMsg = args.Error.Message;

			if (errorMsg == null)
				return false;

			if (args.Error != null)
				HandleException(topText, args.Error);
			else
				HandleException(topText, errorMsg);

			return true;
		}

		public event EventHandler OnRequestClose;

		protected void GetInstallersMeta()
		{
			Closeable = false;
			UpperProgressValue = 0;
			UpperProgressLimit = 1;

			var locator = CalculatedGameSettings.Current.Locator;
			if (locator == null)
			{
				UpperProgressText = "Please check for updates first.";
				UpperProgressLimit = 0;
				Closeable = true;
				return;
			}

			UpperProgressText = "Getting installer info...";
			string installersFileName = InstallersMeta.GetFileName();
			HashWebClient wc = new HashWebClient();
			wc.DownloadProgressChanged += (sender, args) =>
				{
					UpperProgressLimit = 100;
					UpperProgressValue = args.ProgressPercentage;
				};
			wc.DownloadFileCompleted += (sender, args) =>
				{
					if (HandlePossibleError("Installer index download failed", args))
						return;
					
					UpperProgressLimit = 100;
					UpperProgressValue = 100;

					UpperProgressText = "Parsing installer info...";
					InstallersMeta newInsts = null;
					try
					{
						newInsts = InstallersMeta.LoadFromFile(installersFileName);
					}
					catch (Exception ex)
					{
						HandleException("Error parsing installer info",ex);
						return;
					}

					var wntInsts = new List<InstallersMeta.InstallerInfo>();
					try
					{
						foreach (var addon in addOns)
						{
							var instMatch = wntInsts.FirstOrDefault(x => { return String.Equals(x.Version, addon.InstallerName); });
							if (instMatch == null)
								instMatch = newInsts.Installers.FirstOrDefault(x => { return String.Equals(x.Version, addon.InstallerName); });

							if (instMatch == null)
								throw new InstallerNotFound(addon.InstallerName);
							else
								wntInsts.Add(instMatch);
						}							
					}
					catch (Exception ex)
					{
						HandleException("Error matching installers",ex);
						return;
					}

					installers = wntInsts;
					DownloadInstallerPackages();					
				};
			wc.BeginDownload(locator.Installers, installersFileName);
		}

		private void StartInstallerDownload(int arrIdx)
		{
			var inst = installers.ElementAt(arrIdx);

			LowerProgressText = "Downloading " + inst.Archive.Url;
			LowerProgressValue = 0;
			LowerProgressLimit = 100;

			var wc = new HashWebClient();
			wc.DownloadProgressChanged += (sender, args) =>
			{
				LowerProgressValue = args.ProgressPercentage;
			};
			wc.DownloadFileCompleted += (sender, args) =>
			{
				if (HandlePossibleError("Error " + UpperProgressText, args))
					return;

				LowerProgressLimit = 100;
				LowerProgressValue = 100;

				UpperProgressValue = UpperProgressValue + 1;
				if (UpperProgressValue == UpperProgressLimit) //downloaded all installers
					VerifyAndInstallPackages();
				else
					StartInstallerDownload(arrIdx + 1);
			};
			wc.BeginDownload(inst.Archive, inst.GetArchiveFileName());
		}

		protected void DownloadInstallerPackages()
		{
			UpperProgressText = "Downloading installers...";
			UpperProgressValue = 0;
			UpperProgressLimit = installers.Count();

			if (UpperProgressLimit < 1) //no installers ?
			{
				HandleException(UpperProgressText,"Error: no installers to download");
				return;
			}

			StartInstallerDownload(0);
		}

		private List<string> executableInput = null;
		private DataReceivedEventHandler stdoutLineReceived = null;
		private Dictionary<string, List<string>> executableOutput = new Dictionary<string, List<string>>();

		private struct RunStatus
		{
			public int ExitCode;
			public string ErrorString;
		}

		private static void WaitForProcessEOF(Process process, string field)
		{
			FieldInfo asyncStreamReaderField = typeof(Process).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
			object asyncStreamReader = asyncStreamReaderField.GetValue(process);

			Type asyncStreamReaderType = asyncStreamReader.GetType();

			MethodInfo waitUtilEofMethod = asyncStreamReaderType.GetMethod(@"WaitUtilEOF", BindingFlags.NonPublic | BindingFlags.Instance);

			object[] empty = new object[] { };

			waitUtilEofMethod.Invoke(asyncStreamReader, empty);
		}

		private RunStatus RunExecutable(string fullExePath, string args=null, bool runAsAdmin=false)
		{
			RunStatus outStatus = new RunStatus();
			using (var proc = new Process())
			{
				var startInfo = new ProcessStartInfo(fullExePath);
				if (!String.IsNullOrEmpty(args))
					startInfo.Arguments = args;

				startInfo.CreateNoWindow = true;
				startInfo.UseShellExecute = false;

				if (runAsAdmin)
					startInfo.Verb = "runas";
				else
					startInfo.Verb = "run";

				if (executableInput != null)
					startInfo.RedirectStandardInput = true;
				else
					startInfo.RedirectStandardInput = false;

				if (stdoutLineReceived != null)
				{
					proc.OutputDataReceived += stdoutLineReceived;
					startInfo.RedirectStandardOutput = true;
				}
				else
					startInfo.RedirectStandardOutput = false;

				startInfo.RedirectStandardError = true;
				proc.ErrorDataReceived += (sender, evt) =>
					{
						if (evt.Data == null)
							return;

						if (outStatus.ErrorString == null)
							outStatus.ErrorString = "";

						outStatus.ErrorString += evt.Data;
					};

				proc.StartInfo = startInfo;
				proc.Start();

				if (startInfo.RedirectStandardOutput)
					proc.BeginOutputReadLine();
				if (startInfo.RedirectStandardError)
					proc.BeginErrorReadLine();

				if (startInfo.RedirectStandardInput)
				{
					foreach (var line in executableInput)
						proc.StandardInput.WriteLine(line);

					//last one to indicate no more input
					proc.StandardInput.WriteLine();
				}

				proc.WaitForExit();

				if (startInfo.RedirectStandardError)
					WaitForProcessEOF(proc,"error");
				if (startInfo.RedirectStandardOutput)
					WaitForProcessEOF(proc, "output");

				outStatus.ExitCode = proc.ExitCode;
			}
			return outStatus;
		}

		private string ExtractAndVerifyExe(string zipFilename, string outputDir, InstallersMeta.InstallerInfo.ExecutableInfo exeInfo)
		{
			var zipShortName = Path.GetFileName(zipFilename);

			string outputFileName = Path.Combine(outputDir,exeInfo.FileName);
			bool verified = false;
			try
			{
				if (!HashWebClient.Sha1VerifyFile(outputFileName, exeInfo.Sha1Hash))
					throw new Exception("Invalid hash");

				verified = true;
			}
			catch (Exception) { outputFileName = null; } //cannot use the original file

			if (outputFileName == null)
			{
				try
				{
					using (var stream = File.OpenRead(zipFilename))
					{
						using (var reader = ReaderFactory.Open(stream))
						{
							while (reader.MoveToNextEntry())
							{
								if (reader.Entry.IsDirectory)
									continue;

								var fileName = Path.GetFileName(reader.Entry.FilePath);
								if (!fileName.Equals(exeInfo.FileName, StringComparison.OrdinalIgnoreCase))
									continue;

								if (!Directory.Exists(outputDir))
									Directory.CreateDirectory(outputDir);

								reader.WriteEntryToDirectory(outputDir, ExtractOptions.Overwrite);
								outputFileName = Path.Combine(outputDir, fileName);
								break;
							}
						}
					}
				}
				catch (Exception ex)
				{
					HandleException("Error extracting '" + zipShortName + "'", ex);
					return null;
				}
			}

			try
			{
				if (String.IsNullOrEmpty(outputFileName))
					throw new Exception(String.Format("File not found in archive '{1}",zipShortName));

				if (!verified)
				{
					if (!HashWebClient.Sha1VerifyFile(outputFileName, exeInfo.Sha1Hash))
						throw new Exception("File has invalid hash after extract");
				}				
			}
			catch (Exception ex) 
			{ 
				HandleException("Error verifying '" + exeInfo.FileName + "'", ex);
				return null;
			}

			return outputFileName;
		}

		protected void VerifyAndInstallPackages()
		{
			var thrd = new Thread(() =>
				{
					UpperProgressValue = 0;
					UpperProgressLimit = addOns.Count();

					if (String.IsNullOrWhiteSpace(CalculatedGameSettings.Current.AddonsPath))
					{
						HandleException("Verifying addons...", "Error: Output path is empty");
						return;
					}

					foreach (var addon in addOns)
					{
						UpperProgressText = "Verifying " + addon.Description;

						string addonSourceDir = Path.Combine(UserSettings.ContentDataPath, addon.Name);
						string addonDestDir = Path.Combine(CalculatedGameSettings.Current.AddonsPath, addon.Name);
						int numAddonFiles = Directory.GetFiles(addonSourceDir, "*.*", SearchOption.AllDirectories).Length;

						LowerProgressValue = 0;
						LowerProgressLimit = numAddonFiles;

						var inst = installers.First(x => { return String.Equals(x.Version, addon.InstallerName, StringComparison.OrdinalIgnoreCase); });
						var verifierExeFileName = ExtractAndVerifyExe(inst.GetArchiveFileName(), inst.GetDirectoryName(), inst.Verifier);

						if (verifierExeFileName == null) //failed to extract exe and already printed error
							return;

						stdoutLineReceived = (sender, args) =>
						{
							string theLine = args.Data;
							if (theLine == null)
								return;

							if (!theLine.Contains('|')) //status line
							{
								LowerProgressText = "Processing " + theLine;
							}
							else //data line
							{
								List<string> listOut = null;
								if (executableOutput.ContainsKey(inst.Version))
									listOut = executableOutput[inst.Version];
								else
								{
									listOut = new List<string>();
									executableOutput.Add(inst.Version, listOut);
								}

								listOut.Add(theLine);
								LowerProgressValue = LowerProgressValue + 1;
							}
						};

						var exeArguments = "\"" + addonSourceDir + "\" \"" + addonDestDir + "\"";
						var execRes = RunExecutable(verifierExeFileName, exeArguments);
						if (execRes.ExitCode < 0) //bad result
						{
							HandleException(UpperProgressText, execRes.ErrorString);
							return;
						}

						UpperProgressValue = UpperProgressValue + 1;
					}

					UpperProgressValue = 0;
					UpperProgressLimit = 0;
					foreach (var list4Inst in executableOutput)
						UpperProgressLimit += list4Inst.Value.Count;

					foreach (var instList in executableOutput)
					{
						var instName = instList.Key;
						var list = instList.Value;

						UpperProgressText = "Installing using '" + instName + "'";
						var inst = installers.First(x => { return String.Equals(x.Version, instName, StringComparison.OrdinalIgnoreCase); });
						var copierExeFileName = ExtractAndVerifyExe(inst.GetArchiveFileName(), inst.GetDirectoryName(), inst.Copier);

						if (copierExeFileName == null) //failed to extract exe and already printed error
							return;

						LowerProgressText = null;
						LowerProgressValue = 0;
						LowerProgressLimit = 0;

						bool isFirstLine = true;
						bool isFileNameLine = true;
						stdoutLineReceived = (sender, args) =>
						{
							string theLine = args.Data;
							if (theLine == null)
								return;

							if (isFirstLine)
							{
								int actualEntries = int.Parse(theLine);
								UpperProgressLimit -= list.Count;
								UpperProgressLimit += actualEntries;

								isFirstLine = false;
								return;
							}

							if (isFileNameLine)
							{
								var pipeIndex = theLine.LastIndexOf('|');
								var fileName = theLine.Substring(0, pipeIndex);
								var fileSize = theLine.Substring(pipeIndex + 1);

								if (fileSize.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
								{
									LowerProgressText = "Deleting " + fileName;
									LowerProgressValue = 0;
									LowerProgressLimit = 1;
								}
								else
								{
									LowerProgressText = "Writing " + fileName;
									LowerProgressValue = 0;
									LowerProgressLimit = int.Parse(fileSize);
								}

								isFileNameLine = false;
							}
							else
							{
								if (theLine.Length > 0) //is it a dot (multiple dots?)
								{
									LowerProgressValue += theLine.Length;
								}
								else //blank newline, means expect another filename
								{
									UpperProgressValue = UpperProgressValue + 1;
									isFileNameLine = true;
								}
							}
						};

						executableInput = list;

						var execRes = RunExecutable(copierExeFileName, null, false);
						if (inst.Copier.RunAsCode.HasValue && execRes.ExitCode == inst.Copier.RunAsCode.GetValueOrDefault()) //needs elevation
							execRes = RunExecutable(copierExeFileName, null, true);

						if (execRes.ExitCode < 0) //bad return code
						{
							HandleException(UpperProgressText, execRes.ErrorString);
							return;
						}
					}

					UpperProgressText = "Done.";
					Closeable = true;
					Execute.OnUiThread(() => { OnRequestClose(this, null); }, Dispatcher);
				});

			thrd.IsBackground = true;
			thrd.Start();
		}

		protected void TorrentStatusUpdate(TorrentState newState, double newProgress)
		{
			UpperProgressValue = (int)(newProgress * 100.0);

			if (newState == TorrentState.Hashing)
				UpperProgressText = "Verifying...";
			else if (newState == TorrentState.Downloading)
				UpperProgressText = "Downloading...";
			else if (newState == TorrentState.Seeding || newState == TorrentState.Stopped)
			{
				TorrentUpdater.StatusCallbacks -= TorrentStatusUpdate;
				GetInstallersMeta();
			}
		}

		public Dispatcher Dispatcher = null;

		public LaunchProgressViewModel(MetaGameType gameType, IEnumerable<MetaAddon> addOns)
		{
			this.gameType = gameType;
			this.addOns = addOns;

			UpperProgressValue = UpperProgressLimit = 0;
			LowerProgressValue = LowerProgressLimit = 0;

			if (TorrentUpdater.CurrentState() != TorrentState.Seeding && TorrentUpdater.CurrentState() != TorrentState.Stopped)
			{
				UpperProgressValue = 0;
				UpperProgressLimit = 100;
				Closeable = true;

				TorrentUpdater.StatusCallbacks += TorrentStatusUpdate;
			}
			else
				GetInstallersMeta();
		}

		private string _upperProgressText = null;
		public string UpperProgressText
		{
			get { return _upperProgressText; }
			set
			{
				if (_upperProgressText != value)
				{
					_upperProgressText = value;
					Execute.OnUiThreadSync(() => PropertyHasChanged("UpperProgressText"), Dispatcher, DispatcherPriority.Render);
				}
			}
		}

		private int _upperProgressValue = 0;
		public int UpperProgressValue
		{
			get { return _upperProgressValue; }
			set 
			{
				if (_upperProgressValue != value)
				{
					_upperProgressValue = value;
					Execute.OnUiThread(() => PropertyHasChanged("UpperProgressValue"), Dispatcher, DispatcherPriority.Render);
				}					
			}
		}

		private int _upperProgressLimit = 0;
		public int UpperProgressLimit
		{
			get { return _upperProgressLimit; }
			set
			{
				if (_upperProgressLimit != value)
				{
					_upperProgressLimit = value;
					Execute.OnUiThread(() => PropertyHasChanged("UpperProgressLimit"), Dispatcher, DispatcherPriority.Render);
				}
			}
		}

		private string _lowerProgressText = null;
		public string LowerProgressText
		{
			get { return _lowerProgressText; }
			set
			{
				if (_lowerProgressText != value)
				{
					_lowerProgressText = value;
					Execute.OnUiThread(() => PropertyHasChanged("LowerProgressText"), Dispatcher, DispatcherPriority.Render);
				}
			}
		}

		private int _lowerProgressValue = 0;
		public int LowerProgressValue
		{
			get { return _lowerProgressValue; }
			set
			{
				if (_lowerProgressValue != value)
				{
					_lowerProgressValue = value;
					Execute.OnUiThread(() => PropertyHasChanged("LowerProgressValue"), Dispatcher, DispatcherPriority.Render);
				}
			}
		}

		private int _lowerProgressLimit = 0;
		public int LowerProgressLimit
		{
			get { return _lowerProgressLimit; }
			set
			{
				if (_lowerProgressLimit != value)
				{
					_lowerProgressLimit = value;
					Execute.OnUiThread(() => PropertyHasChanged("LowerProgressLimit"), Dispatcher, DispatcherPriority.Render);
				}
			}
		}

		private bool _closeable = true;
		public bool Closeable
		{
			get { return _closeable; }
			set
			{
				if (_closeable != value)
				{
					_closeable = value;
					Execute.OnUiThread(() => PropertyHasChanged("Closeable"), Dispatcher, DispatcherPriority.DataBind);
				}
			}
		}
	}
}
