using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace EchoTcpServer
{
    /// <summary>
    /// Production wrapper over System.Net.Sockets.TcpListener.
    /// Excluded from coverage — contains real socket I/O.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TcpListenerWrapper : ITcpListener
    {
        private readonly TcpListener _listener;

        public TcpListenerWrapper(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start() => _listener.Start();
        public void Stop() => _listener.Stop();
        public Task<TcpClient> AcceptTcpClientAsync() => _listener.AcceptTcpClientAsync();
    }
}
