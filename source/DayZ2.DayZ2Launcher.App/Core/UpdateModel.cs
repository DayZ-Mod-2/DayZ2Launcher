using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace DayZ2.DayZ2Launcher.App.Core
{
	[JsonConverter(typeof(SemanticVersionConverter))]
	public readonly struct SemanticVersion
	{
		readonly uint m_value;

		SemanticVersion(uint value)
		{
			m_value = value;
		}

		public int Major => (int)(m_value >> 0x18 & 0xffu);
		public int Minor => (int)(m_value >> 0x10 & 0xffu);
		public int Build => (int)(m_value >> 0x08 & 0xffu);
		public int Patch => (int)(m_value >> 0x00 & 0xffu);

		public static SemanticVersion Parse(string s)
		{
			var match = s_regex.Match(s);
			if (!match.Success) throw new FormatException($"Invalid semantic version format.\n'{s}'");

			uint value = 0;
			value |= uint.Parse(match.Groups[1].Value) << 0x18;
			value |= uint.Parse(match.Groups[2].Value) << 0x10;
			value |= uint.Parse(match.Groups[3].Value) << 0x08;
			value |= uint.Parse(match.Groups[4].Value) << 0x00;
			return new SemanticVersion(value);
		}

		public override string ToString() => $"{Major}.{Minor}.{Build}.{Patch}";

		public override bool Equals(object other) => other is SemanticVersion v && v.m_value == m_value;
		public override int GetHashCode() => m_value.GetHashCode();

		public static bool operator==(SemanticVersion a, SemanticVersion b) => a.m_value == b.m_value;
		public static bool operator!=(SemanticVersion a, SemanticVersion b) => a.m_value != b.m_value;

		static readonly Regex s_regex = new Regex(@"(\d+).(\d+).(\d+).(\d+)");
	}

	public class SemanticVersionConverter : JsonConverter<SemanticVersion>
	{
		public override SemanticVersion Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			return SemanticVersion.Parse(reader.GetString());
		}

		public override void Write(
			Utf8JsonWriter writer,
			SemanticVersion version,
			JsonSerializerOptions options)
		{
			writer.WriteStringValue(version.ToString());
		}
	}

	public enum UpdateStatus
	{
		UpToDate,
		OutOfDate,
		Checking,
		Error
	}

	public struct Resource
	{
		[JsonPropertyName("url")]
		public Uri Uri { get; set; }
		[JsonPropertyName("sha256")]
		public string Sha256 { get; set; }

		Resource(JsonElement json) : this()
		{
			Uri = new Uri(json.GetProperty("url").GetString());

			if (json.TryGetProperty("sha256", out JsonElement hashJson))
				Sha256 = hashJson.GetString();
		}

		public static Resource FromJson(JsonElement json) => new Resource(json);
	}

	public class ResourceLocator
	{
		static readonly Uri RootLocatorUri = new Uri(@"https://www.perry-swift.de/dayz2/locator.json");

		private readonly HttpClient m_httpClient;

		private readonly Dictionary<string, string> m_stringCache = new Dictionary<string, string>();

		public ResourceLocator(HttpClient httpClient)
		{
			m_httpClient = httpClient;
		}

		async Task<HttpContent> GetAsync(Uri uri, CancellationToken cancellationToken)
		{
			var response = await m_httpClient.GetAsync(uri, cancellationToken);
			if (!response.IsSuccessStatusCode)
				throw new Exception($"Resource locator download failed: {response.StatusCode}\n{uri}");
			return response.Content;
		}

		public async Task<Resource> LocateAsync(string name, CancellationToken cancellationToken)
		{
			using (var content = await GetAsync(RootLocatorUri, cancellationToken))
			{
				var json = await JsonDocument.ParseAsync(await content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
				return Resource.FromJson(json.RootElement.GetProperty(name));
			}
		}

		public async Task<string> GetStringAsync(Resource resource, CancellationToken cancellationToken)
		{
			if (m_stringCache.TryGetValue(resource.Sha256, out string result))
				return result;

			using (var content = await GetAsync(resource.Uri, cancellationToken))
			{
				result = await content.ReadAsStringAsync(cancellationToken);

				if (resource.Sha256 != Hash.HashStringSha256(result))
					throw new Exception($"Resource locator hash did not match the content.\n{resource.Uri}");

				m_stringCache.Add(resource.Sha256, result);

				return result;
			}
		}

		public async Task DownloadAsync(Resource resource, FileInfo file, CancellationToken cancellationToken)
		{
			using (var content = await GetAsync(resource.Uri, cancellationToken))
			using (var fileStream = file.Open(FileMode.Create, FileAccess.Write))
			using (var sha256 = SHA256.Create())
			using (var hashStream = new HashStream(fileStream, sha256))
			{
				await content.CopyToAsync(hashStream, cancellationToken);
				if (resource.Sha256 != hashStream.HashString)
					throw new Exception($"Resource locator hash did not match the content.\n{resource.Uri}");
			}
		}
	}

	public class ModUpdater : IAsyncDisposable
	{
		public bool IsRunning { get; set; }
		public UpdateStatus Status { get; private set; }
		public SemanticVersion CurrentVersion { get; private set; }
		public SemanticVersion LatestVersion { get; private set; }

		private readonly List<Task> m_extractionTasks = new();

		private struct ModInfo
		{
			[JsonPropertyName("name")]
			public string Name { get; set; }
			[JsonPropertyName("latest")]
			public SemanticVersion LatestVersion { get; set; }
			[JsonPropertyName("addons")]
			public Resource ContentResource { get; set; }
		}

		private class Mod
		{
#pragma warning disable CS0649
			public SemanticVersion CurrentVersion;  // TODO: save some version file containing each mod
#pragma warning restore CS0649
			public SemanticVersion LatestVersion;

			public Resource LatestVersionContent;
			public Torrent[] Torrents;

			public DirectoryInfo Directory;
		}

		private class Torrent
		{
			public string Sha256;
			public readonly FileInfo TorrentFile;
			public FileInfo ArchiveFile;
			public int RefCount;

			public Torrent(string sha256, FileInfo torrentFile)
			{
				Sha256 = sha256;
				TorrentFile = torrentFile;
			}

			public Torrent(string sha256, FileInfo torrentFile, FileInfo archiveFile)
			{
				Sha256 = sha256;
				TorrentFile = torrentFile;
				ArchiveFile = archiveFile;
			}
		}

		private readonly Dictionary<string, Mod> m_mods = new();
		private readonly Dictionary<string, Torrent> m_torrents = new();

		private readonly ResourceLocator m_resourceLocator;
		private readonly TorrentClient m_torrentClient;

		private struct ExtractionProgress
		{
			public Dictionary<Torrent, double> ExtractedBytes;
			public double TotalBytes;

			public ExtractionProgress()
			{
				ExtractedBytes = new Dictionary<Torrent, double>();
				TotalBytes = 0;
			}

			public double TotalExtractedBytes()
			{
				return ExtractedBytes.Values.Sum();
			}

			public void Clear()
			{
				ExtractedBytes.Clear();
				TotalBytes = 0;
			}
		}

		private bool IsExtracting => m_extractionTasks.Count > 0;
		private ExtractionProgress m_extractionProgress = new();

		public ModUpdater(ResourceLocator resourceLocator, TorrentClient torrentClient)
		{
			m_resourceLocator = resourceLocator;
			m_torrentClient = torrentClient;

			App.Current.OnShutdown(this);

			ReadCurrentVersion();
		}

		public async Task CheckForUpdateAsync(string modName, CancellationToken cancellationToken)
		{
			/* {
				"dayz2": {
					"latest": "0.0.0.0",
					"addons": {
						"url": "https://foo.bar/mods/dayz2.json",
						"sha256": "abcdef"
					}
				},
				"dayz2-dev": {
					"latest": "0.0.0.0"
					"addons": {
						"url": "https://foo.bar/mods/dayz2-dev.json",
						"sha256": "abcdef"
					}
				}
			} */

			Resource resource = await m_resourceLocator.LocateAsync("mods", cancellationToken);
			var mods = JsonSerializer.Deserialize<IDictionary<string, ModInfo>>(
				await m_resourceLocator.GetStringAsync(resource, cancellationToken));

			/* [
				{
					"url": "https://foo.bar/torrents/baz.torrent",
					"sha256": "abcdef"
				},
				{
					"url": "https://foo.bar/torrents/quux.torrent",
					"sha256": "abcdef"
				},
			] */

			ModInfo info = mods[modName];
			if (m_mods.TryGetValue(modName, out Mod mod))
			{
				if (mod.LatestVersionContent.Sha256 != info.ContentResource.Sha256 || info.LatestVersion != mod.LatestVersion)
				{
					Status = UpdateStatus.OutOfDate;
					mod.LatestVersion = info.LatestVersion;
					mod.LatestVersionContent = info.ContentResource;
				}
			}
			else
			{
				mod = new Mod()
				{
					Directory = new DirectoryInfo(Path.Combine(UserSettings.ContentDataPath, info.Name)),
					Torrents = new Torrent[] { },
					LatestVersion = info.LatestVersion,
					LatestVersionContent = info.ContentResource
				};
				m_mods.Add(modName, mod);
			}
			LatestVersion = mod.LatestVersion;
			Status = (LatestVersion == CurrentVersion) ? UpdateStatus.UpToDate : UpdateStatus.OutOfDate;
		}

		private async Task<Torrent> AddTorrentAsync(Resource resource, CancellationToken cancellationToken)
		{
			if (!m_torrents.TryGetValue(resource.Sha256, out Torrent torrent))
			{
				torrent = new Torrent(resource.Sha256, new FileInfo(Path.Combine(UserSettings.ContentMetaPath, $"{resource.Sha256}.torrent")));
				if (!torrent.TorrentFile.Exists)
				{
					await m_resourceLocator.DownloadAsync(resource, torrent.TorrentFile, cancellationToken);
					m_torrents[resource.Sha256] = torrent;
				}
			}

			await AddTorrentAsync(torrent, cancellationToken);

			string[] torrentFiles = m_torrentClient.GetTorrentFiles(torrent.TorrentFile.FullName);
			if (torrentFiles.Length != 1)
			{
				throw new Exception($"Torrent is empty or contains more than a single archive: {torrent.TorrentFile.FullName}");
			}
			torrent.ArchiveFile = new FileInfo(Path.Combine(UserSettings.ContentPackedDataPath, torrentFiles[0]));

			return torrent;
		}

		private async Task AddTorrentAsync(Torrent torrent, CancellationToken cancellationToken)
		{
			if (++torrent.RefCount == 1)
				await m_torrentClient.AddTorrentAsync(torrent.TorrentFile.FullName, cancellationToken);
		}

		private async Task RemoveTorrentAsync(Torrent torrent, CancellationToken cancellationToken)
		{
			if (--torrent.RefCount == 0)
			{
				await m_torrentClient.RemoveTorrentAsync(torrent.TorrentFile.FullName, cancellationToken);
			}
		}

		private Task ExtractTorrentAsync(Torrent torrent, DirectoryInfo directory, CancellationToken cancellationToken)
		{
			if (!directory.Exists)
			{
				directory.Create();
			}

			void ExtractTorrentBlocking()
			{
				IArchive archive = ArchiveFactory.Open(torrent.ArchiveFile);
				var options = new ExtractionOptions
				{
					ExtractFullPath = true,
					Overwrite = true,
				};

				archive.CompressedBytesRead += (object sender, CompressedBytesReadEventArgs e) =>
				{
					m_extractionProgress.ExtractedBytes[torrent] = (double)e.CompressedBytesRead;
				};

				foreach (IArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
				{
					cancellationToken.ThrowIfCancellationRequested();

					entry.WriteToDirectory(directory.FullName, options);
				}
			}
			return Task.Run(ExtractTorrentBlocking);
		}

		private async Task ExtractModAsync(Mod mod, CancellationToken cancellationToken)
		{
			m_extractionProgress.Clear();
			foreach (Torrent modTorrent in mod.Torrents)
			{
				var archive = ArchiveFactory.Open(modTorrent.ArchiveFile);
				m_extractionProgress.TotalBytes += archive.TotalUncompressSize;
				m_extractionProgress.ExtractedBytes[modTorrent] = archive.TotalUncompressSize;
			}

			var task = Task.WhenAll(mod.Torrents.Select(n => ExtractTorrentAsync(n, mod.Directory, cancellationToken)));
			m_extractionTasks.Add(task);

			try
			{
				await task;
			}
			finally
			{
				m_extractionTasks.Remove(task);
			}
		}

		private static Task ClearDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken)
		{
			void ClearDirectoryBlocking()
			{
				foreach (FileInfo file in directory.EnumerateFiles("*.*", SearchOption.AllDirectories))
				{
					cancellationToken.ThrowIfCancellationRequested();
					file.Delete();
				}

				foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories("*"))
				{
					cancellationToken.ThrowIfCancellationRequested();
					subDirectory.Delete(true);
				}
			}

			return Task.Run(ClearDirectoryBlocking, cancellationToken);
		}

		public async Task UpdateAsync(string modName, CancellationToken cancellationToken)
		{
			Mod mod = m_mods[modName];

			await Task.WhenAll(mod.Torrents.Select(t => RemoveTorrentAsync(t, cancellationToken)));

			Resource[] resources = JsonSerializer.Deserialize<Resource[]>(
				await m_resourceLocator.GetStringAsync(mod.LatestVersionContent, cancellationToken));
			mod.Torrents = await Task.WhenAll(resources.Select(n => AddTorrentAsync(n, cancellationToken)));

			// wait for all torrents to finish and then stop them to close file handles for extraction
			await Task.WhenAll(m_torrentClient.Torrents().Select(t => t.CompletionTask.Task));
			await Task.WhenAll(mod.Torrents.Select(t => RemoveTorrentAsync(t, cancellationToken)));

			await ExtractModAsync(mod, cancellationToken);
			await Task.WhenAll(mod.Torrents.Select(n => AddTorrentAsync(n, cancellationToken)));

			CurrentVersion = mod.LatestVersion;
			Status = UpdateStatus.UpToDate;
			await WriteCurrentVersion();
		}

		public async Task StartAsync(string modName, CancellationToken cancellationToken)
		{
			foreach (var (name, mod) in m_mods)
			{
				await Task.WhenAll(mod.Torrents.Select(t => RemoveTorrentAsync(t, cancellationToken)));

				var resources = JsonSerializer.Deserialize<Resource[]>(
					await m_resourceLocator.GetStringAsync(mod.LatestVersionContent, cancellationToken));
				var torrents = await Task.WhenAll(resources.Select(n => AddTorrentAsync(n, cancellationToken)));
				mod.Torrents = torrents;
			}

			await m_torrentClient.StartAsync();
		}

		// TODO: use this
		public Task StopAsync() => m_torrentClient.StopAsync();

		public string CurrentStatus()
		{
			TorrentClient.Progress p = m_torrentClient.CalculateProgress();

			string ProgressToString(double value)
			{
				const double Step = 1024.0;
				const double Threshold = 1024.0;

				value /= Step;
				string prefix = "KiB";

				if (value >= Threshold)
				{
					value /= Step;
					prefix = "MiB";
				}

				if (value >= Threshold)
				{
					value /= Step;
					prefix = "GiB";
				}

				return $"{value:0.#} {prefix}";
			}

			if (IsExtracting)
			{
				double extractedSize = m_extractionProgress.TotalExtractedBytes();
				return $"Extracting: {extractedSize / m_extractionProgress.TotalBytes:P}\n{ProgressToString(extractedSize)} / {ProgressToString(m_extractionProgress.TotalBytes)}";
			}

			switch (m_torrentClient.CurrentStatus())
			{
				case TorrentClient.Status.Seeding:
					return $"Seeding({p.LeechCount}): {ProgressToString(p.UploadSpeed)}/s\nTorrents: {p.TorrentCount}\nAvailable Peers: {p.AvailablePeerCount}";
				case TorrentClient.Status.Stopped:
					return "Stopped";
				case TorrentClient.Status.Downloading:
					return $"Progress: {p.DownloadProgress:P}\n{ProgressToString(p.DownloadedSize)} / {ProgressToString(p.TotalSize)}\nLeeching({p.SeedCount}): {ProgressToString(p.DownloadSpeed)}/s\nSeeding({p.LeechCount}): {ProgressToString(p.UploadSpeed)}/s";
				case TorrentClient.Status.Checking:
					return $"Checking: {p.HashingProgress:P}";
				case TorrentClient.Status.Error:
					return "Error";
			}

			return "";
		}

		public async Task VerifyIntegrityAsync(string modName, CancellationToken cancellationToken)
		{
			await m_torrentClient.VerifyTorrentsAsync();
			Mod mod = m_mods[modName];
			await ClearDirectoryAsync(mod.Directory, cancellationToken);
			await ExtractModAsync(mod, cancellationToken);
		}

		public async Task ReconfigureTorrentEngineAsync()
		{
			await m_torrentClient.ReconfigureEngineAsync();
		}

		private async Task WriteCurrentVersion()
		{
			byte[] content = Encoding.ASCII.GetBytes(CurrentVersion.ToString());
			await File.OpenWrite(UserSettings.ContentCurrentTagFile).WriteAsync(content, 0, content.Length);
		}

		private void ReadCurrentVersion()
		{
			if (File.Exists(UserSettings.ContentCurrentTagFile))
			{
				CurrentVersion = SemanticVersion.Parse(File.ReadAllText(UserSettings.ContentCurrentTagFile));
			}
		}

		public ValueTask DisposeAsync()
		{
			return new ValueTask(Task.WhenAll(m_extractionTasks));
		}
	}

	public class MotdUpdater
	{
		public string Motd;

		private readonly ResourceLocator m_resourceLocator;

		public MotdUpdater(ResourceLocator resourceLocator)
		{
			m_resourceLocator = resourceLocator;
		}

		public async Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken)
		{
			string oldMotd = Motd;
			Resource resource = await m_resourceLocator.LocateAsync("motd", cancellationToken);
			Motd = await m_resourceLocator.GetStringAsync(resource, cancellationToken);
			return Motd != oldMotd;
		}
	}

	public struct ServerListInfo
	{
		[JsonPropertyName("hostname")]
		public string Hostname { get; set; }
		[JsonPropertyName("port")]
		public ushort Port { get; set; }
		[JsonPropertyName("mods")]
		public IList<string> Mods { get; set; }
	}

	public class ServerUpdater
	{
		public IList<ServerListInfo> ServerList;
		private string m_sha256;

		private readonly ResourceLocator m_resourceLocator;

		public ServerUpdater(ResourceLocator resourceLocator)
		{
			m_resourceLocator = resourceLocator;
		}

		public async Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken)
		{
			Resource resource = await m_resourceLocator.LocateAsync("servers", cancellationToken);
			if (resource.Sha256 != m_sha256)
			{
				ServerList = JsonSerializer.Deserialize<IList<ServerListInfo>>(
					await m_resourceLocator.GetStringAsync(resource, cancellationToken));
				m_sha256 = resource.Sha256;
				return true;
			}

			return false;
		}
	}
}
