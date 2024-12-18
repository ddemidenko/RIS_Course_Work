using FileServer.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class CustomTcpStreamTests
    {

        [Fact]
        public void Constructor_Should_Throw_When_TcpClient_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CustomTcpStream(null));
        }

        

        [Fact]
        public async Task ReadAsync_Should_Return_Data_When_Client_Sends_Data()
        {
            // Arrange
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endPoint = (IPEndPoint)listener.LocalEndpoint;

            var clientTask = Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(endPoint.Address, endPoint.Port);
                using var networkStream = client.GetStream();
                var data = new byte[] { 1, 2, 3, 4, 5 };
                await networkStream.WriteAsync(data, 0, data.Length);
            });

            var serverClient = await listener.AcceptTcpClientAsync();
            var stream = new CustomTcpStream(serverClient);

            var buffer = new byte[10];

            // Act
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(5, bytesRead);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, buffer[..bytesRead]);

            // Cleanup
            listener.Stop();
        }

        [Fact]
        public async Task WriteAsync_Should_Send_Data_To_Client()
        {
            // Arrange
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endPoint = (IPEndPoint)listener.LocalEndpoint;

            var clientTask = Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(endPoint.Address, endPoint.Port);
                using var networkStream = client.GetStream();
                var buffer = new byte[5];
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                // Assert
                Assert.Equal(5, bytesRead);
                Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, buffer);
            });

            var serverClient = await listener.AcceptTcpClientAsync();
            var stream = new CustomTcpStream(serverClient);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            await stream.WriteAsync(data, 0, data.Length);

            // Cleanup
            listener.Stop();
        }

        
    }
}
