using System.Net.Sockets;
using System.Threading.Tasks;

namespace EchoTcpServer
{
    /// <summary>
    /// Abstraction over TcpListener to enable unit testing without real sockets.
    /// </summary>
    public interface ITcpListener
    {
        void Start();
        void Stop();
        Task<TcpClient> AcceptTcpClientAsync();
    }
}
