using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static List<TcpClient> clients = new List<TcpClient>();
    static readonly object _lock = new object();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("✅ Server started on port 5000...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            lock (_lock) clients.Add(client);
            Console.WriteLine("🔗 New client connected!");

            Task.Run(() => HandleClient(client));
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        try
        {
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"💬 {msg}");
                Broadcast(msg, client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Client disconnected: {ex.Message}");
        }
        finally
        {
            lock (_lock) clients.Remove(client);
            stream.Close();
            client.Close();
        }
    }

    static void Broadcast(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (_lock)
        {
            foreach (var c in clients)
            {
                if (c != sender)
                {
                    try
                    {
                        c.GetStream().Write(data, 0, data.Length);
                    }
                    catch { }
                }
            }
        }
    }
}
