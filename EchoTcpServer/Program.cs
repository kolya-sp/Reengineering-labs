using System;
using System.Threading.Tasks;
using EchoTcpServer;

/// <summary>
/// Entry point. Not for review — infrastructure/composition root only.
/// </summary>
internal class Program
{
    public static async Task Main(string[] args)
    {
        var listener = new TcpListenerWrapper(5000);
        var server = new EchoServer(listener);

        _ = Task.Run(() => server.StartAsync());

        using var sender = new UdpTimedSender("127.0.0.1", 60000);
        Console.WriteLine("Press any key to stop sending...");
        sender.StartSending(5000);

        Console.WriteLine("Press 'q' to quit...");
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q) { }

        sender.StopSending();
        server.Stop();
        Console.WriteLine("Sender stopped.");
    }
}
