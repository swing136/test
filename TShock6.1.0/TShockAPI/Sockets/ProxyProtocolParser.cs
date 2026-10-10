using System;
using System.Net;
using System.Net.Sockets;

namespace TShockAPI.Sockets
{
	public static class ProxyProtocolParser
	{
		static readonly byte[] SIG =
		{
			0x0D,0x0A,0x0D,0x0A,
			0x00,0x0D,0x0A,0x51,
			0x55,0x49,0x54,0x0A
		};

		public static bool TryParseV2(TcpClient client, out IPEndPoint real)
		{
			real = null;

			Socket socket = client.Client;

			if (!socket.Poll(4000, SelectMode.SelectRead))
				return false;

			byte[] header = new byte[16];

			int peek = socket.Receive(header, 0, 16, SocketFlags.Peek);

			if (peek < 16)
				return false;

			for (int i = 0; i < 12; i++)
			{
				if (header[i] != SIG[i])
					return false;
			}

			ushort len = (ushort)((header[14] << 8) | header[15]);

			int total = 16 + len;

			byte[] buf = new byte[total];

			int read = 0;

			while (read < total)
			{
				int r = socket.Receive(buf, read, total - read, SocketFlags.None);

				if (r <= 0)
					return false;

				read += r;
			}

			byte fam = buf[13];

			if ((fam & 0xF0) == 0x10) // IPv4
			{
				byte[] ip =
				{
					buf[16],
					buf[17],
					buf[18],
					buf[19]
				};

				int port = (buf[24] << 8) | buf[25];

				real = new IPEndPoint(new IPAddress(ip), port);
				return true;
			}

			if ((fam & 0xF0) == 0x20) // IPv6
			{
				byte[] ip = new byte[16];

				Buffer.BlockCopy(buf, 16, ip, 0, 16);

				int port = (buf[48] << 8) | buf[49];

				real = new IPEndPoint(new IPAddress(ip), port);
				return true;
			}

			return false;
		}
	}
}
