using System.Net;
using System.Net.Sockets;
using System.Text;
using ClientServer.Messages;

namespace ClientServer.Clients;

public class TcpClient(string hostname, ushort port) : BaseClient(hostname, port) {
  public override void SetUpConnection() {
    try {
      ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      IPAddress ipv4 = Utils.ConvertHostname2IPv4(HostName);

      // Connect to the server
      ClientSocket.Connect(ipv4, Port);

    } catch (SocketException) {
      Console.Error.WriteLine($"Cannot connect to {HostName}:{Port}");
      Environment.Exit(1);
    }
  }

  // Shutdown and close the connection
  public override void EndConnection() {
    ClientSocket.Shutdown(SocketShutdown.Both);
    ClientSocket.Close();
    Console.Error.WriteLine("Socket closed");
  }


  public override async Task<Message> ReceiveMessage() {
    try {
      var buffer = new byte[256];
      while (true) {
        var bytesRead = await ClientSocket.ReceiveAsync(buffer);

        if (bytesRead == 0) {
          // Connection closed by the server
          await Console.Error.WriteLineAsync("Server closed the connection.");
          Environment.Exit(1);
        }

        // Process the received data
        var receivedMessage = Encoding.UTF8.GetString(buffer);
        return Message.FromTcpFormat(receivedMessage);
      }
    } catch (IOException e) {
      Console.WriteLine(e);
      throw;
    }
  }


  public override async Task SendMessage(Message message) {
    string tcpMessage = message.ToTcpFormat();
    byte[] data = Encoding.Default.GetBytes(tcpMessage);
    //await Stream.WriteAsync(data);
    await ClientSocket.SendAsync(data);
  }
}