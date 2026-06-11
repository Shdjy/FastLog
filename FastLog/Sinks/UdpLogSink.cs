using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FastLog.Models;

namespace FastLog.Sinks
{
    public sealed class UdpLogSink : LogSinkBase
    {
        private readonly UdpClient _client;
        private readonly IPEndPoint _remoteEndPoint;

        public UdpLogSink(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("UDP host cannot be empty.", nameof(host));
            }

            if (port <= 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "UDP port must be between 1 and 65535.");
            }

            IPAddress[] addresses = Dns.GetHostAddresses(host);

            if (addresses.Length == 0)
            {
                throw new InvalidOperationException("Cannot resolve UDP host.");
            }

            _remoteEndPoint = new IPEndPoint(addresses[0], port);
            _client = new UdpClient();
        }

        public override void Write(LogMessage message)
        {
            if (message == null)
            {
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(FormatShort(message));
            _client.Send(data, data.Length, _remoteEndPoint);
        }

        public override void Dispose()
        {
            _client.Dispose();
        }
    }
}



