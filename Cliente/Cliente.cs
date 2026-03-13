using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class ChatClient
{
    static TcpClient client;
    static NetworkStream stream;

    static void Main()
    {
        client = new TcpClient("127.0.0.1", 5007);
        stream = client.GetStream();

        Console.Write("Ingresa tu nombre: ");
        string username = Console.ReadLine();

        byte[] nameData = Encoding.UTF8.GetBytes(username);
        stream.Write(nameData, 0, nameData.Length);

        Console.WriteLine("Connected to server.");
        Console.WriteLine("Formato para enviar mensaje: usuario|mensaje");

        Thread receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start();

        while (true)
        {
            string message = Console.ReadLine();
            byte[] data = Encoding.UTF8.GetBytes(message);

            stream.Write(data, 0, data.Length);
        }
    }

    static void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine(">> " + message);
        }
    }
}