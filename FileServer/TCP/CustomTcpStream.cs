using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FileServer.TCP
{
    public class CustomTcpStream : Stream
    {
        private const int BufferSize = 1048576; 
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _networkStream;

        public IPEndPoint RemoteEndPoint { get; }
        public bool IsConnected => _tcpClient.Connected;

        private bool _disposed;

        public CustomTcpStream(TcpClient tcpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _networkStream = _tcpClient.GetStream();

            _tcpClient.ReceiveBufferSize = BufferSize;
            _tcpClient.SendBufferSize = BufferSize;

            RemoteEndPoint = _tcpClient.Client.RemoteEndPoint as IPEndPoint
                             ?? throw new InvalidOperationException("Failed to retrieve remote endpoint.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);

            try
            {
                return _networkStream.Read(buffer, offset, count);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read from the TCP stream.", ex);
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);

            try
            {
                return await _networkStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read asynchronously from the TCP stream.", ex);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);

            try
            {
                _networkStream.Write(buffer, offset, count);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to write to the TCP stream.", ex);
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);

            try
            {
                await _networkStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to write asynchronously to the TCP stream.", ex);
            }
        }

        public override bool CanRead => _networkStream.CanRead;
        public override bool CanWrite => _networkStream.CanWrite;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException("Seeking is not supported in CustomTcpStream.");
        public override long Position
        {
            get => throw new NotSupportedException("Seeking is not supported in CustomTcpStream.");
            set => throw new NotSupportedException("Seeking is not supported in CustomTcpStream.");
        }

        public override void Flush()
        {
            ThrowIfDisposed();
            _networkStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _networkStream.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _networkStream?.Dispose();
                _tcpClient?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomTcpStream));
        }

        private static void ValidateBufferArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            if (offset + count > buffer.Length)
                throw new ArgumentException("Offset and count exceed buffer length.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek is not supported in CustomTcpStream.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("SetLength is not supported in CustomTcpStream.");
        }
    }
}
