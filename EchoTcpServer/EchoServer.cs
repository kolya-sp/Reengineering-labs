using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServer
{
    /// <summary>
    /// TCP echo server. Accepts connections and echoes back all received bytes.
    /// Refactored for testability: accepts ITcpListener so real socket can be
    /// replaced with a mock in unit tests.
    /// </summary>
    public class EchoServer
    {
        private readonly ITcpListener _listener;
        private readonly CancellationTokenSource _cts;

        public EchoServer(ITcpListener listener)
        {
            _listener = listener;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener.Start();

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        // internal static — accessible from EchoServerTests via InternalsVisibleTo
        internal static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using NetworkStream stream = client.GetStream();
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested &&
                       (bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _ = ex; // Exception handled: client disconnected unexpectedly
            }
            finally
            {
                client.Close();
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
            _cts.Dispose();
        }
    }
}
