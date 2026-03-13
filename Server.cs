using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class ChatServer
{
    static TcpListener server;

    static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();

    static void Main(string[] args)
    {
        server = new TcpListener(IPAddress.Any, 5007);
        server.Start();

        Console.WriteLine("Chat server started on port 5007.");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();

            Thread t = new Thread(HandleClient);
            t.Start(client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        // Primer mensaje = nombre del usuario
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string username = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        clients.Add(username, client);

        Console.WriteLine(username + " se conectó.");

        while (true)
        {
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            SendPrivateMessage(username, message);
        }
    }

    static void SendPrivateMessage(string sender, string message)
    {
        string[] parts = message.Split('|');

        if (parts.Length < 2)
            return;

        string receiver = parts[0];
        string text = parts[1];

        if (clients.ContainsKey(receiver))
        {
            TcpClient client = clients[receiver];
            NetworkStream stream = client.GetStream();

            string finalMessage = sender + ": " + text;

            byte[] data = Encoding.UTF8.GetBytes(finalMessage);

            stream.Write(data, 0, data.Length);
        }
    }
}