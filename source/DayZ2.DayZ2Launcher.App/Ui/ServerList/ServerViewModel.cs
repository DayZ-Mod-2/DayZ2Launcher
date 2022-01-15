using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using DayZ2.DayZ2Launcher.App;
using Microsoft.Extensions.DependencyInjection;

using DayZ2.DayZ2Launcher.App.Core;
using DayZ2.DayZ2Launcher.App.Ui;

public struct Rational
{
	public int Numerator { get; private set; }
	public int Denominator { get; private set; }

	public Rational(int numerator, int denominator)
	{
		Numerator = numerator;
		Denominator = denominator;
	}

	public void Deconstruct(out int numerator, out int denominator)
	{
		numerator = Numerator;
		denominator = Denominator;
	}

	public override string ToString() => $"{Numerator}/{Denominator}";
}

public class ServerViewModel : ViewModelBase, IRefreshable
{
	readonly Server m_server;
	readonly GameLauncher m_gameLauncher;
	readonly CancellationToken m_cancellationToken;

	public IList<Player> Players => m_server.Players;
	public string Name => m_server.IsResponding ? m_server.Name : m_server.Hostname;
	public ServerPerspective Perspective => m_server.Perspective;
	public long? Ping => m_server.IsResponding ? m_server.Ping : null;
	public Rational Fullness => new Rational(m_server.PlayerCount, m_server.Slots);

	private bool m_isRefreshing;
	public bool IsRefreshing
	{
		get => m_isRefreshing;
		private set => SetValue(ref m_isRefreshing, value);
	}

	public ServerViewModel(Server server, GameLauncher gameLauncher, AppCancellation cancellation)
	{
		m_server = server;
		m_gameLauncher = gameLauncher;
		m_cancellationToken = cancellation.Token;

		m_server.RefreshStarted += (object sender, EventArgs e) => IsRefreshing = true;
		m_server.RefreshFinished += (object sender, EventArgs e) => IsRefreshing = false;

		m_server.Refreshed += (object sender, EventArgs e) =>
		{
			OnPropertyChanged(nameof(Players));
			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(Perspective));
			OnPropertyChanged(nameof(Ping));
			OnPropertyChanged(nameof(Fullness));
		};
	}

	public void Refresh()
	{
		async Task RefreshAsync()
		{
			try
			{
				await m_server.RefreshAsync(m_cancellationToken);
			}
			catch (TimeoutException)
			{
			}
			catch (OperationCanceledException)
			{
			}
		}
		RefreshAsync();
	}

	public void Join()
	{
		if (m_gameLauncher.CanLaunch)
		{
			m_gameLauncher.LaunchGame(m_server);
		}
	}
}
