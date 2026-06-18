using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    /// <summary>
    /// Lab 8 — Additional tests to cover new code introduced in lab8 refactoring.
    /// Targets the 82 new lines: EnsureConnected(), volatile responseTaskSource,
    /// IQStarted private set, TrySetResult pattern.
    /// </summary>
    public class Lab8AdditionalTests
    {
        private NetSdrClient _client = null!;
        private Mock<ITcpClient> _tcpMock = null!;
        private Mock<IUdpClient> _udpMock = null!;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
                _tcpMock.Setup(tcp => tcp.Connected).Returns(true));
            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
                _tcpMock.Setup(tcp => tcp.Connected).Returns(false));
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>(bytes =>
                    _tcpMock.Raise(tcp => tcp.MessageReceived += null,
                        _tcpMock.Object, bytes));
            _udpMock = new Mock<IUdpClient>();
            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        // EnsureConnected — шлях "не підключено" в StartIQAsync
        [Test]
        public async Task StartIQAsync_WhenNotConnected_DoesNotSendMessage()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            await _client.StartIQAsync();
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        // EnsureConnected — шлях "не підключено" в StopIQAsync
        [Test]
        public async Task StopIQAsync_WhenNotConnected_DoesNotSendMessage()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            await _client.StopIQAsync();
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        // EnsureConnected — шлях "не підключено" в ChangeFrequencyAsync
        [Test]
        public async Task ChangeFrequencyAsync_WhenNotConnected_DoesNotSendMessage()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            await _client.ChangeFrequencyAsync(100000, 1);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        // IQStarted private set — перевірка через StartIQ/StopIQ
        [Test]
        public async Task IQStarted_StartsAsFalse()
        {
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task IQStarted_TrueAfterStart_FalseAfterStop()
        {
            await _client.ConnectAsync();
            await _client.StartIQAsync();
            Assert.That(_client.IQStarted, Is.True);
            await _client.StopIQAsync();
            Assert.That(_client.IQStarted, Is.False);
        }

        // TrySetResult — кілька відповідей не кидають виняток
        [Test]
        public async Task SendTcpRequest_MultipleResponses_DoNotThrow()
        {
            await _client.ConnectAsync();
            // ConnectAsync надсилає 3 повідомлення і отримує echo-відповіді
            // (mock повертає ті самі байти назад)
            // Перевіряємо що не кинуто виняток
            Assert.DoesNotThrowAsync(() => _client.ConnectAsync());
        }

        // ChangeFrequency з різними значеннями — покриваємо BitConverter.GetBytes(hz).Take(5)
        [Test]
        public async Task ChangeFrequencyAsync_ZeroHz_Succeeds()
        {
            await _client.ConnectAsync();
            await _client.ChangeFrequencyAsync(0L, 0);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
        }

        [Test]
        public async Task ChangeFrequencyAsync_HighFrequency_Succeeds()
        {
            await _client.ConnectAsync();
            await _client.ChangeFrequencyAsync(2_400_000_000L, 1);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
        }

        // ConnectAsync вже підключений — не підключається знову
        [Test]
        public async Task ConnectAsync_AlreadyConnected_SkipsConnect()
        {
            await _client.ConnectAsync();
            await _client.ConnectAsync();
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        }

        // Disconnect після connect
        [Test]
        public async Task Disconnect_AfterConnect_Works()
        {
            await _client.ConnectAsync();
            _client.Disconect();
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }
        // Покриваємо гілку де ConnectAsync вже connected
        [Test]
        public async Task ConnectAsync_WhenAlreadyConnected_DoesNotCallConnectAgain()
        {
            await _client.ConnectAsync();
            var connectCallsBefore = 1;
            await _client.ConnectAsync(); // вже connected
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Exactly(connectCallsBefore));
        }

        // StartIQ + StopIQ повний цикл з перевіркою UDP
        [Test]
        public async Task StartAndStop_FullCycle_UdpListenAndStop()
        {
            await _client.ConnectAsync();
            await _client.StartIQAsync();
            _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
            await _client.StopIQAsync();
            _udpMock.Verify(udp => udp.StopListening(), Times.Once);
        }

        // Перевірка що Disconect викликає Disconnect на tcp
        [Test]
        public void Disconect_CallsTcpDisconnect()
        {
            _client.Disconect();
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public void TcpMessageReceived_WithoutPendingRequest_DoesNotThrow()
        {
            // Якщо MessageReceived спрацьовує без pending SendTcpRequest
            // tcs == null — має мовчки проігнорувати
            Assert.DoesNotThrow(() =>
                _tcpMock.Raise(tcp => tcp.MessageReceived += null,
                    _tcpMock.Object, new byte[] { 0x01 })
            );
        }

        [Test]
        public async Task ChangeFrequencyAsync_NoConnection_ReturnsWithoutSending()
        {
            // EnsureConnected повертає false → метод виходить
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            await _client.ChangeFrequencyAsync(100_000_000L, 0);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

    }
}
