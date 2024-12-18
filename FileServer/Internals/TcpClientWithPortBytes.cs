using System.Net;
using System.Net.Sockets;

namespace FileServer.Internals
{
    internal class TcpClientWithPortBytes
    {
        public TcpClient TcpClient { get; init; }

        public byte FirstPortByte { get; init; }

        public byte SecondPortByte { get; init; }

        public TcpClientWithPortBytes()
        {
            TcpClient = new(0);

            IPEndPoint localEndPoint = (IPEndPoint)TcpClient.Client.LocalEndPoint;
            byte[] portBytes = BitConverter.GetBytes(localEndPoint.Port);
            FirstPortByte = portBytes[0];
            SecondPortByte = portBytes[1];
        }
    }
}
