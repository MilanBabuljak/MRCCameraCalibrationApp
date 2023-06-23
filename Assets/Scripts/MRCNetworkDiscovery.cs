using System.Net.NetworkInformation;
using System.Net;
using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MRCNetworkDiscovery : MonoBehaviour
{
	private const int hostId = -1;
	private const int broacastKey = 2222;
	private const int broadcastVersion = 1;
	private const int broadcastSubVersion = 1;
	private const int broacastPort = 47898;

	private string broadcastMessage = "";

	public float timeInterval = 2.0f;

	private float lastBroadcastTime;
	private UdpClient udpBroadcaster;

	private void Start()
	{
		broadcastMessage = SystemInfo.deviceName + ": " + SystemInfo.deviceModel;
	}

	private void Update()
	{
		if (udpBroadcaster == null)
			return;

		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup - lastBroadcastTime > timeInterval)
		{
			var msg = CreateMessage();

			int numInterfacesBroadcasted = 0;

			var interfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (var iface in interfaces)
			{
				if (iface.GetIPProperties().UnicastAddresses.Count == 0)
					continue;

				// usually gateway addresses are have same base
				var addr = iface.GetIPProperties().UnicastAddresses[0].Address;
				if (addr.AddressFamily != AddressFamily.InterNetwork)
					continue;

				var broadcastIp = new IPAddress(addr.GetAddressBytes().Take(3).Concat(new[] { (byte)255 }).ToArray());
				udpBroadcaster.Send(msg, msg.Length, broadcastIp.ToString(), broacastPort);
				numInterfacesBroadcasted++;
			}

			Debug.Log($"[MRCNetworkDiscovery] Broadcasted message over {numInterfacesBroadcasted} interfaces");

			lastBroadcastTime = realtimeSinceStartup;
		}
	}

	private byte[] GetBytesForInt(int i)
	{
		return BitConverter.GetBytes(i).Reverse().ToArray();
	}

	private byte[] CreateMessage()
	{
		var l = new List<byte>();

		l.AddRange(new byte[] { 0x00, 0x00, 0x09 }); // prefix, could be HostId

		var rand = new byte[2];
		new System.Random().NextBytes(rand);
		l.AddRange(rand); // random value per started session

		l.AddRange(GetBytesForInt(broacastKey)); // KEY

		l.AddRange(Enumerable.Repeat(0, 4*8).Select(x => (byte)x)); // padding

		l.AddRange(GetBytesForInt(broadcastVersion)); // VER
		l.AddRange(GetBytesForInt(broadcastSubVersion)); // SUBVER

		l.AddRange(Encoding.ASCII.GetBytes(broadcastMessage).Select(x => new byte[] { x, 0x00 }).SelectMany(y => y)); // DATA

		return l.ToArray();
	}

	public void StartBroadcast()
	{
		udpBroadcaster = new UdpClient();
		udpBroadcaster.MulticastLoopback = true;
		udpBroadcaster.EnableBroadcast = true;

		Debug.Log("[MRCNetworkDiscovery] Server Created with default port");
	}

	public void StopBroadcast()
	{
		udpBroadcaster.Close();
		udpBroadcaster = null;
	}
}
