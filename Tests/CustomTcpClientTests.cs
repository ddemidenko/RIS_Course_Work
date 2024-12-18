using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FileServer.TCP;
using Xunit;


namespace Tests
{
    public class CustomTcpClientTests
    {
        [Fact]
        public void Constructor_Should_InitializeTcpClient()
        {
            // Act
            var client = new CustomTcpClient();

            // Assert
            Assert.NotNull(client);
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void Constructor_WithTcpClient_Should_ThrowException_WhenNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CustomTcpClient(null));
        }

        [Fact]
        public async Task ConnectAsync_Should_ConnectToRemoteHost()
        {
            // Arrange
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var client = new CustomTcpClient();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;

            // Act
            var acceptTask = listener.AcceptTcpClientAsync();
            await client.ConnectAsync(endpoint);

            // Assert
            Assert.True(client.IsConnected);
            Assert.NotNull(await acceptTask);

            // Cleanup
            listener.Stop();
        }

        [Fact]
        public async Task ConnectAsync_Should_ThrowException_WhenRemoteEndPointIsNull()
        {
            // Arrange
            var client = new CustomTcpClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.ConnectAsync(null));
        }

        [Fact]
        public async Task ConnectAsync_Should_ThrowException_WhenConnectionFails()
        {
            // Arrange
            var client = new CustomTcpClient();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 9999); // Unused port

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.ConnectAsync(endpoint));
            Assert.Contains("Failed to connect to the remote host", exception.Message);
        }

        [Fact]
        public async Task GetStream_Should_ReturnNetworkStream_WhenConnected()
        {
            // Arrange
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var client = new CustomTcpClient();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;

            await client.ConnectAsync(endpoint);
            var acceptedClient = await listener.AcceptTcpClientAsync();

            // Act
            var stream = client.GetStream();

            // Assert
            Assert.NotNull(stream);
            Assert.IsType<NetworkStream>(stream);

            // Cleanup
            listener.Stop();
            acceptedClient.Dispose();
        }

        [Fact]
        public void GetStream_Should_ThrowException_WhenNotConnected()
        {
            // Arrange
            var client = new CustomTcpClient();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => client.GetStream());
            Assert.Contains("not connected to a remote host", exception.Message);
        }



        [Fact]
        public void Dispose_Should_BeIdempotent()
        {
            // Arrange
            var client = new CustomTcpClient();

            // Act
            client.Dispose();
            client.Dispose(); // Should not throw an exception

            // Assert
            Assert.True(true); // If no exception is thrown, the test passes
        }

        [Fact]
        public void ThrowIfDisposed_Should_ThrowException_WhenDisposed()
        {
            // Arrange
            var client = new CustomTcpClient();

            client.Dispose();

            // Act & Assert
            var exception = Assert.Throws<ObjectDisposedException>(() => client.GetStream());
            Assert.Contains(nameof(CustomTcpClient), exception.Message);
        }
    }
}