using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FileServer.TCP
{
    public sealed class CustomTcpClient : IDisposable
    {
        private readonly TcpClient _innerTcpClient;
        private NetworkStream _stream;
        private bool _disposed;

        public bool IsConnected => _innerTcpClient.Connected;

        public CustomTcpClient()
        {
            _innerTcpClient = new TcpClient();
        }

        public CustomTcpClient(TcpClient tcpClient)
        {
            _innerTcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _stream = _innerTcpClient.GetStream();
        }

        public async Task ConnectAsync(IPEndPoint remoteEndPoint)
        {
            ThrowIfDisposed();

            if (remoteEndPoint == null)
                throw new ArgumentNullException(nameof(remoteEndPoint));

            try
            {
                await _innerTcpClient.ConnectAsync(remoteEndPoint.Address, remoteEndPoint.Port);
                _stream = _innerTcpClient.GetStream();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to connect to the remote host.", ex);
            }
        }

        public NetworkStream GetStream()
        {
            ThrowIfDisposed();

            if (!IsConnected)
                throw new InvalidOperationException("The CustomTcpClient is not connected to a remote host.");

            return _stream ?? throw new InvalidOperationException("Stream is not available.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _stream?.Dispose();
            _innerTcpClient?.Dispose();

            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomTcpClient));
        }
    }
}
