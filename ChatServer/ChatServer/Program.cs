using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

class Program
{
    static List<TcpClient> chatClients = new List<TcpClient>();
    static List<TcpClient> fileClients = new List<TcpClient>();
    static readonly object _lock = new object();

    static void Main()
    {
        var chatListener = new TcpListener(IPAddress.Any, 5000);
        var fileListener = new TcpListener(IPAddress.Any, 5001);
        chatListener.Start();
        fileListener.Start();
        Console.WriteLine("✅ Server started. Chat:5000, Files:5001");

        Task.Run(() => AcceptLoop(chatListener, isFile:false));
        Task.Run(() => AcceptLoop(fileListener, isFile:true));

        // Keep main thread alive
        Console.WriteLine("Press Ctrl+C to stop.");
        System.Threading.Thread.Sleep(-1);
    }

    static void AcceptLoop(TcpListener listener, bool isFile)
    {
        while (true)
        {
            var client = listener.AcceptTcpClient();
            lock (_lock)
            {
                if (isFile) fileClients.Add(client); else chatClients.Add(client);
            }
            Console.WriteLine(isFile ? "🔗 New file client" : "🔗 New chat client");
            Task.Run(() => HandleClient(client, isFile));
        }
    }

    static void HandleClient(TcpClient client, bool isFile)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[8192];

        try
        {
            while (true)
            {
                string header = ReadLine(stream);
                if (header == null) break;

                if (isFile && header.StartsWith("FILE:"))
                {
                    // Parse header: FILE:username:filename:filesize\n
                    var parts = header.TrimEnd('\r').Split(':');
                    string filename = parts[2];
                    long filesize = long.Parse(parts[3]);

                    // Forward header with trailing \n to clients
                    Broadcast(header + "\n", client, isFile:true);

                    long sent = 0;
                    while (sent < filesize)
                    {
                        int toRead = (int)Math.Min(buffer.Length, filesize - sent);
                        int read = stream.Read(buffer, 0, toRead);
                        if (read <= 0) break;
                        Broadcast(buffer, read, client, isFile:true); // Send chunk to others
                        sent += read;
                    }
                }
                else
                {
                    Console.WriteLine($"💬 {header}");
                    Broadcast(header + "\n", client, isFile:false);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Client disconnected: {ex.Message}");
        }
        finally
        {
            lock (_lock)
            {
                chatClients.Remove(client);
                fileClients.Remove(client);
            }
            stream.Close();
            client.Close();
        }
    }

    static void Broadcast(string message, TcpClient sender, bool isFile)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (_lock)
        {
            var list = isFile ? fileClients : chatClients;
            foreach (var c in list)
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

    // Overload for broadcasting byte[] data
    static void Broadcast(byte[] data, int length, TcpClient sender, bool isFile)
    {
        lock (_lock)
        {
            var list = isFile ? fileClients : chatClients;
            foreach (var c in list)
            {
                if (c != sender)
                {
                    try
                    {
                        c.GetStream().Write(data, 0, length);
                    }
                    catch { }
                }
            }
        }
    }

    static string ReadLine(NetworkStream stream)
    {
        using (var ms = new MemoryStream())
        {
            while (true)
            {
                int b = stream.ReadByte();
                if (b == -1) return null;
                if (b == (byte)'\n') break;
                ms.WriteByte((byte)b);
                if (ms.Length > 1024 * 1024) return null; // avoid abuse
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
