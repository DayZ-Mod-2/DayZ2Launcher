using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class ServerQueryClient
	{
		private readonly Server _server;
		private readonly string _ipAddress;
		private readonly int _queryport;		

		public ServerQueryClient(Server server, string ipAddress, int queryPort)
		{
			_server = server;
			_ipAddress = ipAddress;
			_queryport = queryPort;
		}

		public ServerQueryResult Execute()
		{
			var pingTimer = new Stopwatch();
			var infoRetriever = new SSQLib.SSQL();

			var ipaddress = Dns.GetHostAddresses(_ipAddress)[0];
			var serverEndPoint = new IPEndPoint(ipaddress, _queryport);			

			pingTimer.Start();
			var serverInfo = infoRetriever.Server(serverEndPoint);
			pingTimer.Stop();

			ArrayList playersInfo = null;
			try
			{
				playersInfo = infoRetriever.Players(serverEndPoint);
			}
			catch (Exception) //we dont care if player querying fails, really
			{
				playersInfo = new ArrayList();
			}

			var settings = new SortedDictionary<string, string>();
			settings.Add("hostname", serverInfo.Name);
			settings.Add("maxplayers", serverInfo.MaxPlayers);
			settings.Add("numplayers", serverInfo.PlayerCount);
			settings.Add("mapname", serverInfo.Map);
			settings.Add("gamever", serverInfo.Version);
			{
				Version outVer;
				if (Version.TryParse(serverInfo.Version, out outVer))
					settings.Add("reqBuild", outVer.Build.ToString());
			}
			
			settings.Add("password", (serverInfo.Password)?"1":"0");
			settings.Add("sv_battleye", (serverInfo.VAC)?"1":"0");
			
			//split game name and mod folder
			{
				var gameAndMod = serverInfo.Game.Substring(0,serverInfo.Game.Length-1);
				var gameEndIdx = gameAndMod.LastIndexOf(" (");
				if (gameEndIdx >= 0)
				{
					settings.Add("gametype",gameAndMod.Substring(gameEndIdx+2));
					gameAndMod = gameAndMod.Substring(0,gameEndIdx);
				}

				settings.Add("mod",gameAndMod);
			}
			
			var players = new List<Player>();
			foreach (object playerInfo in playersInfo)
			{
				var pinfo = (SSQLib.PlayerInfo)playerInfo;
				var pl = new Player(_server);

				pl.Name = pinfo.Name;
				pl.Score = pinfo.Score;
				pl.Deaths = pinfo.Deaths;

				players.Add(pl);
			}

			return new ServerQueryResult
			{
				Settings = settings,
				Players = players,
				Ping = pingTimer.ElapsedMilliseconds
			};
		}
	}
}