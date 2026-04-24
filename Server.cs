using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class WarshipServer
{
    static TcpClient[] jugadores = new TcpClient[2];
    static StreamWriter[] escritores = new StreamWriter[2];
    static StreamReader[] lectores = new StreamReader[2];
    static int turnoActual = 0;
    static object candado = new object();
    static bool juegoActivo = false;

    static void Main()
    {
        TcpListener servidor = new TcpListener(IPAddress.Any, 5007);
        servidor.Start();
        Console.WriteLine("Servidor Warship iniciado en puerto 5007...");
        Console.WriteLine("Esperando jugadores...\n");

        for (int i = 0; i < 2; i++)
        {
            jugadores[i] = servidor.AcceptTcpClient();
            escritores[i] = new StreamWriter(jugadores[i].GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            lectores[i] = new StreamReader(jugadores[i].GetStream(), Encoding.UTF8);
            Console.WriteLine("Jugador " + (i + 1) + " conectado.");
        }

        Console.WriteLine("\nAmbos jugadores conectados. ¡Iniciando juego!");

        Enviar(0, "START|0");
        Enviar(1, "START|1");
        Enviar(0, "YOUR_TURN");
        Enviar(1, "WAIT");

        juegoActivo = true;

        Thread t0 = new Thread(() => EscucharJugador(0));
        Thread t1 = new Thread(() => EscucharJugador(1));
        t0.IsBackground = true;
        t1.IsBackground = true;
        t0.Start();
        t1.Start();

        Console.WriteLine("Presiona Enter para detener el servidor.");
        Console.ReadLine();
        juegoActivo = false;
    }

    static void EscucharJugador(int idx)
    {
        try
        {
            while (juegoActivo)
            {
                string msg = lectores[idx].ReadLine();
                if (msg == null) break;
                msg = msg.Trim();
                if (msg.Length == 0) continue;
                Console.WriteLine("  J" + (idx + 1) + " -> servidor: " + msg);
                ProcesarMensaje(idx, msg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Jugador " + (idx + 1) + " desconectado: " + ex.Message);
        }
    }

    static void ProcesarMensaje(int de, string msg)
    {
        int para = 1 - de;
        string[] parts = msg.Split('|');

        lock (candado)
        {
            switch (parts[0])
            {
                case "SHOT":
                    if (de != turnoActual)
                    {
                        Console.WriteLine("  [Ignorado] No es el turno del Jugador " + (de + 1));
                        return;
                    }
                    if (parts.Length < 3) return;
                    Console.WriteLine("  Jugador " + (de + 1) + " dispara en (" + parts[1] + ", " + parts[2] + ")");
                    Enviar(para, "INCOMING|" + parts[1] + "|" + parts[2]);
                    break;

                case "RESULT":
                    if (parts.Length < 2) return;
                    string resultado = parts[1];

                    if (resultado == "HIT")
                    {
                        Console.WriteLine("  Impacto! Jugador " + (de + 1) + " sigue disparando.");
                        Enviar(para, "HIT");
                        Enviar(para, "YOUR_TURN");
                    }
                    else if (resultado == "MISS")
                    {
                        Console.WriteLine("  Agua. Turno del Jugador " + (de + 1) + ".");
                        Enviar(para, "MISS");
                        turnoActual = de;
                        Enviar(de, "YOUR_TURN");
                        Enviar(para, "WAIT");
                    }
                    else if (resultado == "WIN")
                    {
                        Console.WriteLine("  Jugador " + (para + 1) + " gano el juego!");
                        Enviar(para, "WIN");
                        Enviar(de, "LOSE");
                        juegoActivo = false;
                    }
                    break;
            }
        }
    }

    static void Enviar(int idx, string msg)
    {
        Console.WriteLine("  servidor -> J" + (idx + 1) + ": " + msg);
        escritores[idx].WriteLine(msg);
    }
}
