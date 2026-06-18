using System;
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
            Console.WriteLine("Server started.");

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");
                    _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        // internal — accessible from EchoServerTests via InternalsVisibleTo
        internal async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using NetworkStream stream = client.GetStream();
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested &&
                       (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead, token);
                    Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
            _cts.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }
}
