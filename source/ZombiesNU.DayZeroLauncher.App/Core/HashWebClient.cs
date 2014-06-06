using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class HashWebClient : IDisposable
	{
		public delegate void StatusChangeString(string newStatus);

		public delegate void StatusDownloadComplete(HashWebClient wc, RemoteFileInfo fileInfo, string destPath);

		private CustomWebClient _wc;

		public HashWebClient(int timeout = -1)
		{
			Timeout = timeout;
		}

		public int Timeout { get; set; }

		public void Dispose()
		{
			if (_wc != null)
			{
				_wc.Dispose();
				_wc = null;
			}
		}

		public event AsyncCompletedEventHandler DownloadFileCompleted;
		public event DownloadProgressChangedEventHandler DownloadProgressChanged = delegate { };

		public static bool Sha1VerifyFile(string fileFullPath, string expectedSha1)
		{
			using (Stream fileStream = File.OpenRead(fileFullPath))
			{
				using (SHA1 shaHasher = SHA1.Create())
				{
					byte[] hash = shaHasher.ComputeHash(fileStream);
					fileStream.Close();

					string hexHash = BitConverter.ToString(hash).Replace("-", string.Empty).ToUpperInvariant();
					return hexHash.Equals(expectedSha1, StringComparison.OrdinalIgnoreCase);
				}
			}
		}

		public void BeginDownload(RemoteFileInfo fileInfo, string destPath)
		{
			try
			{
				var localFileInfo = new FileInfo(destPath);
				if (localFileInfo.Exists)
				{
					if (Sha1VerifyFile(destPath, fileInfo.Sha1Hash))
					{
						var newEvt = new AsyncCompletedEventArgs(null, false, null);
						DownloadFileCompleted(this, newEvt);
						return; //already have the file with correct contents on disk
					}
				}
			}
			catch (Exception ex)
			{
				var newEvt = new AsyncCompletedEventArgs(ex, false, null);
				DownloadFileCompleted(this, newEvt);
				return; //something failed when trying to hash file
			}

			if (_wc != null)
			{
				_wc.CancelAsync();
				_wc.Dispose();
				_wc = null;
			}

			_wc = new CustomWebClient(Timeout);
			_wc.DownloadProgressChanged += (sender, evt) => { DownloadProgressChanged(sender, evt); };
			_wc.DownloadFileCompleted += (sender, evt) =>
			{
				using (var wc = (WebClient) sender)
				{
					if (evt.Cancelled || evt.Error != null)
					{
						DownloadFileCompleted(sender, evt);
						return;
					}

					try
					{
						if (!Sha1VerifyFile(destPath, fileInfo.Sha1Hash))
							throw new Exception("Hash mismatch after download");
					}
					catch (Exception ex)
					{
						var newEvt = new AsyncCompletedEventArgs(ex, false, evt.UserState);
						DownloadFileCompleted(sender, newEvt);
						return;
					}

					DownloadFileCompleted(sender, evt);
				}
				_wc = null;
			};

			_wc.DownloadFileAsync(new Uri(fileInfo.Url), destPath);
		}

		public static void DownloadWithStatusDots(RemoteFileInfo fileInfo, string destPath, string initialStatus,
			StatusChangeString statusCb, StatusDownloadComplete finishCb)
		{
			using (var downloadingEvt = new ManualResetEvent(false))
			{
				string updateInfoString = initialStatus.Replace("...", String.Empty);
				statusCb(updateInfoString);

				var wc = new HashWebClient();
				wc.DownloadFileCompleted += (source, evt) =>
				{
					if (evt.Cancelled)
						statusCb("Async operation cancelled");
					else if (evt.Error != null)
						statusCb(evt.Error.Message);
					else
					{
						try
						{
							if (finishCb != null)
								finishCb(wc, fileInfo, destPath);
						}
						catch (Exception ex)
						{
							statusCb(ex.Message);
						}
					}
					downloadingEvt.Set();
				};
				wc.BeginDownload(fileInfo, destPath);

				int numDots = 0;
				uint loops = 0;
				while (downloadingEvt.WaitOne(10) == false)
				{
					if (loops >= 10)
					{
						numDots = (numDots + 1)%3;
						statusCb(updateInfoString + new String('.', numDots));
						loops = 0;
					}

					loops++;
				}
				if (wc != null)
				{
					wc.Dispose();
					wc = null;
				}
			}
		}

		private class CustomWebClient : WebClient
		{
			public CustomWebClient(int timeout = -1)
			{
				Timeout = timeout;
			}

			public int Timeout { get; set; }

			protected override WebRequest GetWebRequest(Uri uri)
			{
				WebRequest req = base.GetWebRequest(uri);
				if (Timeout >= 0)
					req.Timeout = Timeout;

				string userName = UserSettings.Current.GameOptions.CustomBranchName;
				if (!string.IsNullOrWhiteSpace(userName))
				{
					string password = UserSettings.Current.GameOptions.CustomBranchPass;
					if (!string.IsNullOrEmpty(password))
					{
						string authInfo = userName.Trim() + ":" + password;
						authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo));
						req.Headers["Authorization"] = "Basic " + authInfo;
					}
				}

				return req;
			}
		}

		public class RemoteFileInfo
		{
			[JsonProperty("url")]
			public string Url { get; set; }

			[JsonProperty("sha1")]
			public string Sha1Hash { get; set; }
		}
	}
}