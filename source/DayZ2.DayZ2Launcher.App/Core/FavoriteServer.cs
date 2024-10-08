﻿using System.Runtime.Serialization;

namespace DayZ2.DayZ2Launcher.App.Core
{
	[DataContract]
	public class FavoriteServer
	{
		[DataMember] private readonly string _ipAddress;
		[DataMember] private readonly string _name;
		[DataMember] private readonly int _port;

		public FavoriteServer(Server server)
		{
			_ipAddress = server.Hostname;
			_port = server.QueryPort;
			_name = server.Name;
		}

		public bool Matches(Server server)
		{
			return server.Hostname == _ipAddress && server.QueryPort == _port;
		}

		/*
		public Server CreateServer()
		{
			
			var server = new Server(_ipAddress, _port);
			server.Settings = new SortedDictionary<string, string>()
								{
									{"hostname",_name}
								};
			return server;
		}
		 */
	}
}
