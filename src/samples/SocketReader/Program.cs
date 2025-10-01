using System.Net;
using System.Net.Sockets;
using FastHl7;

namespace SocketReader;

/// <summary>
/// Sample program to read MLLP messages from a socket
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        using var server = new TcpListener(IPAddress.Loopback, 3000);
        server.Start();
        Console.WriteLine("Sever started...");

        for (;;)
        {
            // Run the listening loop forever
            // this will keep accepting and servicing client connections

            Console.WriteLine("Waiting for incoming client connections...");

            using var client = await server.AcceptTcpClientAsync(); // Get client connection

            Console.WriteLine("Handling incoming client connection...");

            await using var stream = client.GetStream();
            await using var reader = new MllpReader(stream);

            // Synchronous handler
            //await reader.ReadMessagesAsync(OnMessageReceived);
            
            // or async handler (but has to allocate a string for each message)
            await reader.ReadMessagesAsync(OnMessageReceivedAsync);
            
        }
    }

    // lightweight, synchronous handler which allows for ReadOnlySpan
    private static void OnMessageReceived(ReadOnlySpan<char> message)
    {
        Console.WriteLine("Message received - processing...");

        var msg = new Message(message);
        var messageId = msg.Query("MSH.10");

        Console.WriteLine($"Received Message ID: {messageId}");
        Console.WriteLine();
    }
    
    
    private static async Task OnMessageReceivedAsync(string message)
    {
        Console.WriteLine("Message received - processing...");

        var msg = new Message(message);
        var messageId = msg.Query("MSH.10");

        Console.WriteLine($"Received Message ID: {messageId}");
        Console.WriteLine();
        
        await Task.Delay(1);
    }
}