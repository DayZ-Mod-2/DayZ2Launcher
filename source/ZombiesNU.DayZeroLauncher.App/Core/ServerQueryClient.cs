using System;
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
		private readonly string _ipAddress;
		private readonly int _port;
		private readonly Server _server;

		public ServerQueryClient(Server server, string ipAddress, int port)
		{
			_server = server;
			_ipAddress = ipAddress;
			_port = port;
		}

		public ServerQueryResult Execute()
		{
			byte[] reply;
			var s = new Stopwatch();
			var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
			using (var client = new UdpClient(ipEndPoint))
			{
				client.Client.ReceiveTimeout = 1000*6;
				client.Client.SendTimeout = 1000*6;

				s.Start();
				client.Connect(_ipAddress, _port);
				var challengePacket = new byte[] {0xFE, 0xFD, 0x09};
				var basePacket = new byte[] {0xFE, 0xFD, 0x00};
				var idPacket = new byte[] {0x04, 0x05, 0x06, 0x07};
				var fullInfoPacket = new byte[] {0xFF, 0xFF, 0xFF, 0x01};

				byte[] firstRequestPacket = challengePacket.Concat(idPacket).ToArray();
				client.Send(firstRequestPacket, firstRequestPacket.Length);

				byte[] challengeResponse = client.Receive(ref ipEndPoint);
				s.Stop();

				challengePacket = challengeResponse.Skip(5).ToArray();
				string challengeString = Encoding.ASCII.GetString(challengePacket);

				challengePacket = BitConverter.GetBytes(Convert.ToInt32(challengeString)).Reverse().ToArray();
				byte[] secondPacket = basePacket.Concat(idPacket).Concat(challengePacket).Concat(fullInfoPacket).ToArray();
				client.Send(secondPacket, secondPacket.Length);
				reply = client.Receive(ref ipEndPoint);
				client.Close();
			}
			string response = Encoding.ASCII.GetString(reply.Skip(16).ToArray());
			string[] items = response.Split(new[] {"\0"}, StringSplitOptions.None);

			var settings = new SortedDictionary<string, string>();
			for (int index = 0; index < items.Length; index++)
			{
				if (index == 60)
					break;
				string name = items[index];
				string value = items[index + 1];

				settings.Add(name, value);
				index++;
			}

			IEnumerable<string> playerItem = items.SkipWhile(x => Convert.ToString(x).IndexOf("player_") == -1);
			List<Player> players = playerItem
				.Skip(2)
				.TakeWhile(x => x != "team_" && x != "")
				.Select(x => new Player(_server) {Name = x})
				.ToList();

			List<string> scores = items.SkipWhile(x => x != "score_")
				.Skip(2)
				.Take(players.Count)
				.ToList();


			List<string> deaths = items.SkipWhile(x => x != "deaths_")
				.Skip(2)
				.Take(players.Count)
				.ToList();


			for (int index = 0; index < players.Count; index++)
			{
				Player player = players[index];
				player.Score = scores.Count > index
					? scores.ElementAt(index).TryInt()
					: 0;
				player.Deaths = deaths.Count > index
					? deaths.ElementAt(index).TryInt()
					: 0;
			}

			return new ServerQueryResult
			{
				Settings = settings,
				Players = players,
				Ping = s.ElapsedMilliseconds
			};
		}
	}
}