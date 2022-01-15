using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace DayZ2.DayZ2Launcher.App.Core
{
	[DataContract]
	public class UserSettings
	{
		private static UserSettings _current;
		private static readonly object _fileLock = new object();
		private static string _localDataPath;
		private static string _torrentJunkPath;
		private static string _contentPath;
		private static string _contentMetaPath;
		private static string _contentDataPath;
		private static string _contentPackedDataPath;
		private static string _patchesPath;
		private static string _roamingDataPath;
		private static string _notesPath;
		private static string _settingsPath;

		[DataMember] private AppOptions _appOptions = new AppOptions();
		[DataMember] private List<string> _enabledPlugins = new List<string>();
		[DataMember] private List<FavoriteServer> _favorites = new List<FavoriteServer>();
		[DataMember] private Filters _filter = new Filters();
		[DataMember] private List<string> _friends = new List<string>();
		[DataMember] private GameOptions _gameOptions = new GameOptions();
		[DataMember] private bool _hideAU;
		[DataMember] private bool _hideEU;
		[DataMember] private bool _hideUS;
		[DataMember] private List<RecentServer> _recentServers = new List<RecentServer>();
		[DataMember] private TorrentOptions _torrentOptions = new TorrentOptions();

		[DataMember] private WindowSettings _windowSettings;
		//This is null on purpose so the MainWindow view can set defaults if needed

		public bool IncludeUS
		{
			get { return !_hideUS; }
			set { _hideUS = !value; }
		}

		public bool IncludeEU
		{
			get { return !_hideEU; }
			set { _hideEU = !value; }
		}

		public bool IncludeAU
		{
			get { return !_hideAU; }
			set { _hideAU = !value; }
		}

		public Filters Filters
		{
			get
			{
				if (_filter == null)
					_filter = new Filters();

				return _filter;
			}
			set { _filter = value; }
		}

		public WindowSettings WindowSettings
		{
			get { return _windowSettings; }
			set { _windowSettings = value; }
		}

		public GameOptions GameOptions
		{
			get
			{
				if (_gameOptions == null)
					_gameOptions = new GameOptions();

				return _gameOptions;
			}
			set { _gameOptions = value; }
		}

		public TorrentOptions TorrentOptions
		{
			get
			{
				if (_torrentOptions == null)
					_torrentOptions = new TorrentOptions();

				return _torrentOptions;
			}
			set { _torrentOptions = value; }
		}

		public AppOptions AppOptions
		{
			get
			{
				if (_appOptions == null)
					_appOptions = new AppOptions();

				return _appOptions;
			}
			set { _appOptions = value; }
		}

		public List<FavoriteServer> Favorites
		{
			get
			{
				if (_favorites == null)
				{
					_favorites = new List<FavoriteServer>();
				}
				return _favorites;
			}
			set { _favorites = value; }
		}

		public List<string> EnabledPlugins
		{
			get
			{
				if (_enabledPlugins == null)
				{
					_enabledPlugins = new List<string>();
				}
				return _enabledPlugins;
			}
			set { _enabledPlugins = value; }
		}

		public List<RecentServer> RecentServers
		{
			get
			{
				if (_recentServers == null)
				{
					_recentServers = new List<RecentServer>();
				}
				return _recentServers;
			}
			set { _recentServers = value; }
		}

		public static UserSettings Current
		{
			get
			{
				lock (_fileLock)
				{
					if (_current == null)
					{
						try
						{
							_current = Load();
						}
						catch (Exception)
						{
							_current = new UserSettings();
						}
					}
				}
				return _current;
			}
		}

		private static string LocalDataPath
		{
			get
			{
				if (_localDataPath == null)
				{
					string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					var dayzAppDataDir = new DirectoryInfo(Path.Combine(appDataFolder, "DayZ2Launcher"));
					if (!dayzAppDataDir.Exists)
						dayzAppDataDir.Create();

					_localDataPath = dayzAppDataDir.FullName;
				}

				return _localDataPath;
			}
		}

		public static string TorrentJunkPath
		{
			get
			{
				if (_torrentJunkPath == null)
				{
					string torrentJunkPathLocation = Path.Combine(LocalDataPath, "torrent");
					var dirInfo = new DirectoryInfo(torrentJunkPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_torrentJunkPath = dirInfo.FullName;
				}
				return _torrentJunkPath;
			}
		}

		public static string ContentPath
		{
			get
			{
				if (_contentPath == null)
				{
					string contentPathLocation = Path.Combine(LocalDataPath, "content");
					var dirInfo = new DirectoryInfo(contentPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_contentPath = dirInfo.FullName;
				}
				return _contentPath;
			}
		}

		public static string ContentMetaPath
		{
			get
			{
				if (_contentMetaPath == null)
				{
					string contentMetaPathLocation = Path.Combine(ContentPath, "meta");
					var dirInfo = new DirectoryInfo(contentMetaPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_contentMetaPath = dirInfo.FullName;
				}
				return _contentMetaPath;
			}
		}

		public static string ContentPackedDataPath
		{
			get
			{
				if (_contentPackedDataPath == null)
				{
					string contentPackedDataPathLocation = Path.Combine(ContentPath, "packeddata");
					var dirInfo = new DirectoryInfo(contentPackedDataPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_contentPackedDataPath = dirInfo.FullName;
				}
				return _contentPackedDataPath;
			}
		}

		public static string ContentDataPath
		{
			get
			{
				if (_contentDataPath == null)
				{
					string contentDataPathLocation = Path.Combine(ContentPath, "data");
					var dirInfo = new DirectoryInfo(contentDataPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_contentDataPath = dirInfo.FullName;
				}
				return _contentDataPath;
			}
		}

		public static string ContentCurrentTagFile => Path.Combine(ContentPath, "current");

		public static string PatchesPath
		{
			get
			{
				if (_patchesPath == null)
				{
					string patchesPathLocation = Path.Combine(LocalDataPath, "patches");
					var dirInfo = new DirectoryInfo(patchesPathLocation);
					if (!dirInfo.Exists)
						dirInfo.Create();

					_patchesPath = dirInfo.FullName;
				}
				return _patchesPath;
			}
		}

		private static string RoamingDataPath
		{
			get
			{
				if (_roamingDataPath == null)
				{
					string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					var dayzAppDataDir = new DirectoryInfo(Path.Combine(appDataFolder, "DayZ2Launcher"));
					if (!dayzAppDataDir.Exists)
						dayzAppDataDir.Create();

					_roamingDataPath = dayzAppDataDir.FullName;
				}

				return _roamingDataPath;
			}
		}

		private static string NotesPath
		{
			get
			{
				if (_notesPath == null)
				{
					const string notesFolderName = "notes";
					string newNotesLocation = Path.Combine(RoamingDataPath, notesFolderName);
					if (!Directory.Exists(newNotesLocation))
					{
						DirectoryInfo newFolder = Directory.CreateDirectory(newNotesLocation);
						//migrate old notes
						try
						{
							var oldFolder = new DirectoryInfo(LocalDataPath);
							if (oldFolder.Exists)
							{
								FileInfo[] noteFiles = oldFolder.GetFiles("Notes_*.txt");
								foreach (FileInfo noteFile in noteFiles)
								{
									try
									{
										string newName = Path.Combine(newFolder.FullName, noteFile.Name.Replace("Notes_", ""));
										File.Move(noteFile.FullName, newName);
									}
									catch (Exception)
									{
									}
								}
							}
						}
						catch (Exception)
						{
						}
					}
					_notesPath = newNotesLocation;
				}
				return _notesPath;
			}
		}

		private static string SettingsPath
		{
			get
			{
				if (_settingsPath == null)
				{
					const string settingsFileName = "settings.xml";
					_settingsPath = Path.Combine(RoamingDataPath, settingsFileName);
				}
				return _settingsPath;
			}
		}

		public void Save()
		{
			try
			{
				lock (_fileLock)
				{
					using (FileStream fs = GetSettingsFileStream(FileMode.Create))
					{
						var serializer = new DataContractSerializer(GetType());
						serializer.WriteObject(fs, this);
						fs.Flush(true);
					}
				}
			}
			catch (Exception)
			{
			}
		}

		private static UserSettings Load()
		{
			try
			{
				using (FileStream fs = GetSettingsFileStream(FileMode.Open))
				{
					using (var reader = new StreamReader(fs))
					{
						string rawXml = reader.ReadToEnd();
						if (string.IsNullOrWhiteSpace(rawXml))
							return new UserSettings();
						return LoadFromXml(XDocument.Parse(rawXml));
					}
				}
			}
			catch (FileNotFoundException)
			{
				return new UserSettings();
			}
		}

		private static FileStream GetSettingsFileStream(FileMode fileMode)
		{
			return new FileStream(SettingsPath, fileMode);
		}

		private static UserSettings LoadFromXml(XDocument xDocument)
		{
			var serializer = new DataContractSerializer(typeof(UserSettings));
			var parsedVal = (UserSettings)serializer.ReadObject(xDocument.CreateReader());
			if (parsedVal != null && parsedVal.TorrentOptions != null)
			{
				if (parsedVal.TorrentOptions.RandomizePort)
					parsedVal.TorrentOptions.ListeningPort = 0; //this calls Random internally
			}
			return parsedVal;
		}

		public bool IsFavorite(Server server)
		{
			return Favorites.Any(f => f.Matches(server));
		}

		public void AddFavorite(Server server)
		{
			if (Favorites.Any(f => f.Matches(server)))
				return;
			Favorites.Add(new FavoriteServer(server));
			Save();
		}

		public void RemoveFavorite(Server server)
		{
			FavoriteServer favorite = Favorites.FirstOrDefault(f => f.Matches(server));
			if (favorite == null)
				return;
			Favorites.Remove(favorite);
			Save();
		}

		public void AddRecent(Server server)
		{
			var recentServer = new RecentServer(server, DateTime.Now);
			if (RecentServers.Count > 50)
			{
				RecentServer oldest = RecentServers.OrderBy(x => x.On).FirstOrDefault();
				RecentServers.Remove(oldest);
			}
			RecentServers.Add(recentServer);
			recentServer.Server = server;
			Save();
		}

		public string GetNotes(Server server)
		{
			string fileName = GetNoteFileName(server);
			if (!File.Exists(fileName))
				return "";
			return File.ReadAllText(fileName, Encoding.UTF8);
		}

		public void SetNotes(Server server, string text)
		{
			string fileName = GetNoteFileName(server);
			if (string.IsNullOrWhiteSpace(text))
			{
				if (File.Exists(fileName))
					File.Delete(fileName);
			}
			else
			{
				File.WriteAllText(fileName, text, Encoding.UTF8);
			}
		}

		private static string GetNoteFileName(Server server)
		{
			return Path.Combine(NotesPath, $"{server.Hostname.Replace(".", "_")}_{server.QueryPort}.txt");
		}

		public bool HasNotes(Server server)
		{
			return File.Exists(GetNoteFileName(server));
		}
	}

	public class RecentAdded
	{
		public RecentAdded(RecentServer recent)
		{
			Recent = recent;
		}

		public RecentServer Recent { get; set; }
	}

	public class FavoritesUpdated
	{
		public FavoritesUpdated(Server server)
		{
			Server = server;
		}

		public Server Server { get; set; }
	}
}
