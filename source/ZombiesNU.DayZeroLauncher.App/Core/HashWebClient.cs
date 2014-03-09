using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class HashWebClient : IDisposable
	{
		public class RemoteFileInfo
		{
			[JsonProperty("url")]
			public string Url { get; set; }

			[JsonProperty("sha1")]
			public string Sha1Hash { get; set; }
		}

		public event AsyncCompletedEventHandler DownloadFileCompleted;
		public event DownloadProgressChangedEventHandler DownloadProgressChanged = delegate {};

		private WebClient _wc;

		public void Dispose()
		{
			if (_wc != null)
			{
				_wc.Dispose();
				_wc = null;
			}
		}
		public static bool Sha1VerifyFile(string fileFullPath, string expectedSha1)
		{
			using (Stream fileStream = File.OpenRead(fileFullPath))
			{
				using (var shaHasher = SHA1.Create())
				{
					var hash = shaHasher.ComputeHash(fileStream);
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

			_wc = new WebClient();
			_wc.DownloadProgressChanged += (sender, evt) =>
				{
					DownloadProgressChanged(sender, evt);
				};
			_wc.DownloadFileCompleted += (sender, evt) =>
				{
					using (var wc = (WebClient)sender)
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

			_wc.DownloadFileAsync(new Uri(fileInfo.Url),destPath);
		}

		public delegate void StatusChangeString(string newStatus);
		public delegate void StatusDownloadComplete(HashWebClient wc, RemoteFileInfo fileInfo, string destPath);
		public static void DownloadWithStatusDots(RemoteFileInfo fileInfo, string destPath, string initialStatus, StatusChangeString statusCb, StatusDownloadComplete finishCb)
		{
			using (var downloadingEvt = new System.Threading.ManualResetEvent(false))
			{
				string updateInfoString = initialStatus.Replace("...", String.Empty);
				statusCb(updateInfoString);

				HashWebClient wc = new HashWebClient();
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
						catch (Exception ex) { statusCb(ex.Message); }
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
						numDots = (numDots + 1) % 3;
						statusCb(updateInfoString + new String('.', numDots));
						loops = 0;
					}

					loops++;
				}
				wc.Dispose();
				wc = null;
			}
		}
	}
}
