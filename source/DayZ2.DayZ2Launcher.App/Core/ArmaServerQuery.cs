using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamQueryNet;
using SteamQueryNet.Enums;
using SteamQueryNet.Models;

namespace DayZ2.DayZ2Launcher.App.Core
{
	internal class ArmaServerQuery
	{
		public static async Task Run(Server server, CancellationToken cancellationToken)
		{
			ServerQuery serverQuery = new ServerQuery()
			{
				ReceiveTimeout = 5000,
				SendTimeout = 5000
			};
			serverQuery.Connect(server.Hostname, server.QueryPort);
			var info = await serverQuery.GetServerInfoAsync(cancellationToken);
			server.Name = info.Name;
			server.Ping = info.Ping;
			server.Slots = info.MaxPlayers;
			server.Version = info.Version;
			server.Dedicated = info.ServerType == ServerType.Dedicated;
			if (Enum.TryParse(info.Environment.ToString(), true, out ServerPlatform result))
				server.Platform = result;

			ParseKeywords(server, info.Keywords);

			var players = await serverQuery.GetPlayersAsync(cancellationToken);
			server.PlayerCount = players.Count;
			server.Players = players.Select(p => new Player()
			{
				Name = p.Name,
				Score = p.Score,
				Duration = p.TotalDurationAsString
			}).ToArray();

			ParseRules(server, await serverQuery.GetRulesAsync(cancellationToken));
		}

		private static void ParseKeywords(Server server, string keywords)
		{
			// https://community.bistudio.com/wiki/Arma_3:_STEAMWORKSquery#Table
			foreach (string keyword in keywords.Split(','))
			{
				if (keyword.Length < 1)
					continue;

				bool ToBool(string s, bool defaultValue)
				{
					if (s == "t")
						return true;
					if (s == "f")
						return false;
					return defaultValue;
				}

				string payload = keyword.Substring(1, keyword.Length - 1);
				switch (keyword[0])
				{
					case 'b':
						server.Battleye = ToBool(payload, false);
						break;
					case 'r':
						server.RequiredVersion = payload;
						break;
					case 'n':
						server.RequiredBuildNo = payload;
						break;
					case 's':
						server.State = (ServerState)int.Parse(payload);
						break;
					case 'i':
						server.Difficulty = (ServerDifficulty)int.Parse(payload);
						switch (server.Difficulty)
						{
							case ServerDifficulty.Recruit:
								server.Perspective = ServerPerspective.ThirdPerson;
								break;
							case ServerDifficulty.Regular:
								server.Perspective = ServerPerspective.ThirdPerson;
								break;
							case ServerDifficulty.Veteran:
								server.Perspective = ServerPerspective.ThirdPerson;
								break;
							case ServerDifficulty.Mercenary:
								server.Perspective = ServerPerspective.FirstPerson;
								break;
						}
						break;
					case 'm':
						server.EqualModRequired = ToBool(payload, false);
						break;
					case 'l':
						server.Lock = ToBool(payload, false);
						break;
					case 'v':
						server.VerifySignatures = ToBool(payload, false);
						break;
					case 'd':
						server.Dedicated = ToBool(payload, server.Dedicated);
						break;
					case 't':
						server.GameType = payload;
						break;
					case 'g':
						server.Language = payload;
						break;
					case 'c':
						server.LatLong = payload;
						break;
					case 'p':
						if (server.Platform == ServerPlatform.Undefined)
							server.Platform = (ServerPlatform)payload[0];
						break;
					case 'h':
						server.ContentLoadedHash = payload;
						break;
					case 'o':
						server.Country = payload;
						break;
					case 'e':
						server.TimeLeft = payload;
						break;
				}
			}
		}

		private static void ParseRules(Server server, List<Rule> rules)
		{
			foreach (Rule rule in rules)
			{
				switch (rule.Name)
				{
					case "country":
						server.Country ??= rule.Value;
						break;
					case "timeLeft":
						server.TimeLeft ??= rule.Value;
						break;
				}
			}
		}
	}
}
