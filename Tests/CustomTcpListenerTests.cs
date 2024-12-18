using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FileServer.TCP;
using Xunit;


namespace Tests
{
    public class CustomTcpListenerTests
    {
        [Fact]
        public void Constructor_Should_Initialize_Listener()
        {
            // Arrange & Act
            var listener = new CustomTcpListener(5000);

            // Assert
            Assert.NotNull(listener);
        }

        [Fact]
        public void Start_Should_Not_Throw_When_Listener_Initialized()
        {
            // Arrange
            var listener = new CustomTcpListener(5000);

            // Act & Assert
            listener.Start();
            listener.Stop();
        }

        [Fact]
        public async Task AcceptTcpClientAsync_Should_Return_Client_When_Client_Connects()
        {
            // Arrange
            int port = 5001;
            var listener = new CustomTcpListener(port);
            listener.Start();

            var clientTask = Task.Run(async () =>
            {
                await Task.Delay(100); // Ensure the listener is ready
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
            });

            // Act
            var customClient = await listener.AcceptTcpClientAsync();

            // Assert
            Assert.NotNull(customClient);
            Assert.True(customClient.IsConnected);

            // Cleanup
            listener.Stop();
        }

    }
}