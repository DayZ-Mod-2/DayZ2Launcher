using System;
using System.Net;

namespace Mono.Nat
{
	public delegate void NatDeviceCallback(INatDevice device);

	internal interface ISearcher
	{
		DateTime NextSearch { get; }
		event EventHandler<DeviceEventArgs> DeviceFound;
		event EventHandler<DeviceEventArgs> DeviceLost;

		void Search();
		void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint);
	}
}