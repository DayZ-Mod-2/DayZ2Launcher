using MonoTorrent.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DayZ2.DayZ2Launcher.App.Core;

public class CrashLogUploader
{
	private readonly FileUploader m_fileUploader;
	private readonly CancellationToken m_cancellationToken;
	private const string LogFileName = "ArmA2OA.RPT";
	private const string CrashFileName = "ArmA2OA.bidmp";
	private readonly string m_logFile;
	private readonly string m_crashFile;
	private long m_logFileStart;
	private const long MaxLogSize = 2 * 1024 * 1024;

	public CrashLogUploader(AppCancellation cancellation, HttpClient httpClient)
	{
		m_cancellationToken = cancellation.Token;
		m_fileUploader = new("https://www.perry-swift.de/dayz2/cases/", "pzmBtlVvgPd3DwfesBdpwUupiTwMPzFAGuIVA/Wq", httpClient);
		string a2OaPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArmA 2 OA");
		m_logFile = Path.Join(a2OaPath, LogFileName);
		m_crashFile = Path.Join(a2OaPath, CrashFileName);
	}

	public void GameStarting()
	{
		long currentLength = new FileInfo(m_logFile).Length;
		if (currentLength > MaxLogSize)
		{
			// the game will shrink the log file on startup if the size is too large
			// it will take MaxLogSize bytes from the end from the old log and discard the top line

			using (MemoryMappedFile file = MemoryMappedFile.CreateFromFile(m_logFile, FileMode.Open))
			{
				long start = currentLength - MaxLogSize;
				using (MemoryMappedViewStream stream = file.CreateViewStream(start, 64 * 1024, MemoryMappedFileAccess.Read))
				{
					using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
					{
						string line = sr.ReadLine();
						long trunc = start + line?.Length ?? 0 + 2;  // 2 because of CRLF
						m_logFileStart = currentLength - trunc;
					}
				}
			}
		}
		else
		{
			m_logFileStart = currentLength;
		}
	}

	private static bool LineContainsSqfError(string line)
	{
		if (line == null) return false;
		return
			line.Contains("error: sqf:", StringComparison.OrdinalIgnoreCase) ||
			line.Contains("error position", StringComparison.OrdinalIgnoreCase);
	}

	public async void GameClosed(string dayzVersion)
	{
		if (!UserSettings.Current.PrivacyOptions.AllowSendingCrashLogs)
		{
			return;
		}

		try
		{
			long logFileSize = new FileInfo(m_logFile).Length - m_logFileStart;
			if (logFileSize > 0)
			{
				MemoryMappedFile file = MemoryMappedFile.CreateFromFile(m_logFile, FileMode.Open);
				MemoryMappedViewStream stream = file.CreateViewStream(m_logFileStart, logFileSize, MemoryMappedFileAccess.Read);
					
				using (StreamReader sr = new StreamReader(stream))
				{
					while (sr.Peek() >= 0)
					{
						string? line = await sr.ReadLineAsync(m_cancellationToken);
						if (LineContainsSqfError(line))
						{
							stream.Seek(0, SeekOrigin.Begin);
							await m_fileUploader.Upload(new[]
							{
								new FileUploader.UploadFileInfo() { FileName = LogFileName, FileStream = stream }
							}, dayzVersion, m_cancellationToken);
							break;
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	public async void GameCrashed(string dayzVersion)
	{
		if (!UserSettings.Current.PrivacyOptions.AllowSendingCrashLogs)
		{
			return;
		}

		try
		{
			var files = new List<FileUploader.UploadFileInfo>();

			long logFileLength = new FileInfo(m_logFile).Length - m_logFileStart;
			if (logFileLength > 0)
			{
				MemoryMappedFile logFile = MemoryMappedFile.CreateFromFile(m_logFile, FileMode.Open);
				MemoryMappedViewStream logStream = logFile.CreateViewStream(m_logFileStart,
					new FileInfo(m_logFile).Length - m_logFileStart, MemoryMappedFileAccess.Read);
				files.Add(new FileUploader.UploadFileInfo() { FileName = LogFileName, FileStream = logStream });
			}

			long crashFileLength = new FileInfo(m_crashFile).Length;
			if (crashFileLength > 0)
			{
				MemoryMappedFile crashFile = MemoryMappedFile.CreateFromFile(m_crashFile, FileMode.Open);
				MemoryMappedViewStream crashStream =
					crashFile.CreateViewStream(0, crashFileLength, MemoryMappedFileAccess.Read);
				files.Add(new FileUploader.UploadFileInfo() { FileName = CrashFileName, FileStream = crashStream });
			}

			if (files.Any())
			{
				await m_fileUploader.Upload(files, dayzVersion, m_cancellationToken);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}
}
