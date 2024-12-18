using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FileServer.TCP
{
    public sealed class CustomTcpListener : IDisposable
    {
        private readonly TcpListener _tcpListener;
        private readonly Dictionary<IPEndPoint, ReservedTcpClient> _reservedClientTable;
        private readonly TimeSpan _reserveTimeout = TimeSpan.FromSeconds(10);
        private bool _disposed;

        public CustomTcpListener(int port)
        {
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _reservedClientTable = new Dictionary<IPEndPoint, ReservedTcpClient>();
        }

        public void Start()
        {
            ThrowIfDisposed();
            _tcpListener.Start();
        }

        public void Stop()
        {
            ThrowIfDisposed();
            _tcpListener.Stop();
        }

        public async Task<CustomTcpClient> AcceptTcpClientAsync()
        {
            ThrowIfDisposed();

            // Проверяем, запущен ли слушатель, если нет, запускаем его
            if (!_tcpListener.Server.IsBound)
                _tcpListener.Start();

            TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
            IPEndPoint remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            // Удаление из кэша, если уже есть запись
            if (_reservedClientTable.ContainsKey(remoteEndPoint))
                _reservedClientTable.Remove(remoteEndPoint);

            // Резервирование нового клиента
            var reservedClient = new ReservedTcpClient
            {
                TcpClient = tcpClient,
                ReservedAt = DateTime.Now
            };

            _reservedClientTable[remoteEndPoint] = reservedClient;

            return new CustomTcpClient(tcpClient);
        }


        public void ClearConnectionCache()
        {
            ThrowIfDisposed();

            DateTime now = DateTime.Now;

            // Удаляем устаревшие записи
            foreach (var key in new List<IPEndPoint>(_reservedClientTable.Keys))
            {
                var reservedClient = _reservedClientTable[key];
                if (now - reservedClient.ReservedAt >= _reserveTimeout)
                {
                    reservedClient.TcpClient.Dispose();
                    _reservedClientTable.Remove(key);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();

            foreach (var kvp in _reservedClientTable)
            {
                kvp.Value.TcpClient.Dispose();
            }

            _reservedClientTable.Clear();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomTcpListener));
        }
    }

    internal class ReservedTcpClient
    {
        public TcpClient TcpClient { get; set; }
        public DateTime ReservedAt { get; set; }
    }
}
