using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoTcpServer;
using Moq;
using NUnit.Framework;

namespace EchoServerTests
{
    public class EchoServerTests
    {
        private Mock<ITcpListener> _listenerMock = null!;
        private EchoServer _server = null!;

        [SetUp]
        public void Setup()
        {
            _listenerMock = new Mock<ITcpListener>();
            _server = new EchoServer(_listenerMock.Object);
        }

        // ---------------------------------------------------------------
        // StartAsync — запускає listener і зупиняється при скасуванні
        // ---------------------------------------------------------------

        [Test]
        public async Task StartAsync_CallsListenerStart()
        {
            // Arrange: AcceptTcpClientAsync кидає ObjectDisposedException одразу
            // щоб вийти з циклу без реального сокета
            _listenerMock
                .Setup(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new ObjectDisposedException("listener"));

            // Act
            await _server.StartAsync();

            // Assert
            _listenerMock.Verify(l => l.Start(), Times.Once);
        }

        [Test]
        public async Task StartAsync_ExitsLoop_OnObjectDisposedException()
        {
            // Arrange
            _listenerMock
                .Setup(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new ObjectDisposedException("listener"));

            // Act — не має зависнути
            var task = _server.StartAsync();
            await Task.WhenAny(task, Task.Delay(2000));

            // Assert — метод завершився без timeout
            Assert.That(task.IsCompleted, Is.True);
        }

        // ---------------------------------------------------------------
        // Stop — скасовує токен і зупиняє listener
        // ---------------------------------------------------------------

        [Test]
        public void Stop_CallsListenerStop()
        {
            // Act
            _server.Stop();

            // Assert
            _listenerMock.Verify(l => l.Stop(), Times.Once);
        }

        [Test]
        public void Stop_CanBeCalledWithoutStart()
        {
            // Act & Assert — не кидає виняток
            Assert.DoesNotThrow(() => _server.Stop());
        }

        // ---------------------------------------------------------------
        // HandleClientAsync — основна логіка echo
        // ---------------------------------------------------------------

        [Test]
        public async Task HandleClientAsync_EchoesDataBack()
        {
            // Arrange: два TcpClient спілкуються через loopback
            using var serverSocket = new TcpListener(System.Net.IPAddress.Loopback, 0);
            serverSocket.Start();
            int port = ((System.Net.IPEndPoint)serverSocket.LocalEndpoint).Port;

            using var clientTcp = new TcpClient();
            await clientTcp.ConnectAsync(System.Net.IPAddress.Loopback, port);
            using var serverClient = await serverSocket.AcceptTcpClientAsync();
            serverSocket.Stop();

            var cts = new CancellationTokenSource();
            var handleTask = _server.HandleClientAsync(serverClient, cts.Token);

            // Act: надіслати дані і прочитати echo
            byte[] sent = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var clientStream = clientTcp.GetStream();
            await clientStream.WriteAsync(sent, 0, sent.Length);

            byte[] received = new byte[sent.Length];
            int bytesRead = await clientStream.ReadAsync(received, 0, received.Length);

            // Assert
            Assert.That(bytesRead, Is.EqualTo(sent.Length));
            Assert.That(received, Is.EqualTo(sent));

            // Cleanup
            cts.Cancel();
            clientTcp.Close();
            await Task.WhenAny(handleTask, Task.Delay(1000));
        }

        [Test]
        public async Task HandleClientAsync_StopsOnCancellation()
        {
            // Arrange
            using var serverSocket = new TcpListener(System.Net.IPAddress.Loopback, 0);
            serverSocket.Start();
            int port = ((System.Net.IPEndPoint)serverSocket.LocalEndpoint).Port;

            using var clientTcp = new TcpClient();
            await clientTcp.ConnectAsync(System.Net.IPAddress.Loopback, port);
            using var serverClient = await serverSocket.AcceptTcpClientAsync();
            serverSocket.Stop();

            var cts = new CancellationTokenSource();

            // Act: скасувати токен одразу
            cts.Cancel();
            var handleTask = _server.HandleClientAsync(serverClient, cts.Token);
            await Task.WhenAny(handleTask, Task.Delay(2000));

            // Assert — завершилось без зависання
            Assert.That(handleTask.IsCompleted, Is.True);
        }

        [Test]
        public async Task HandleClientAsync_ClosesClientOnCompletion()
        {
            // Arrange
            using var serverSocket = new TcpListener(System.Net.IPAddress.Loopback, 0);
            serverSocket.Start();
            int port = ((System.Net.IPEndPoint)serverSocket.LocalEndpoint).Port;

            using var clientTcp = new TcpClient();
            await clientTcp.ConnectAsync(System.Net.IPAddress.Loopback, port);
            using var serverClient = await serverSocket.AcceptTcpClientAsync();
            serverSocket.Stop();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await Task.WhenAny(
                _server.HandleClientAsync(serverClient, cts.Token),
                Task.Delay(2000));

            // Assert — після завершення клієнт закритий
            Assert.That(serverClient.Connected, Is.False);
        }
    }
}
