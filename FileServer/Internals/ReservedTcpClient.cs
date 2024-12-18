using System.Net;

namespace FileServer.Internals
{
    internal class ReservedTcpClient
    {
        public IPEndPoint ReservedByEndpoint { get; set; }

        public TcpClientWithPortBytes TcpClientWithPortBytes { get; init; }

        public DateTime ReservedAt { get; init; }
    }
}
