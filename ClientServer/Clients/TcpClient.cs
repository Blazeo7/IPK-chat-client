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
      ClientSocket.Connect(ipv4, Port);
    } catch (SocketException) {
      Utils.PrintInternalError($"Cannot connect to {HostName}:{Port}");
      Environment.Exit(1);
    }
  }

  public override void EndConnection() {
    ClientSocket.Shutdown(SocketShutdown.Both);
    ClientSocket.Close();
    Logger.Log("Client socket closed");
  }


  public override async Task<Message> ReceiveMessage() {
    byte[] buffer = new byte[2048];
    await ClientSocket.ReceiveAsync(buffer);

    string receivedMessage = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
    return Message.FromTcpFormat(receivedMessage);
  }

  public override async Task<bool> SendMessage(Message message) {
    Logger.Log("Client sent", message);
    string tcpMessage = message.ToTcpFormat();
    byte[] data = Encoding.Default.GetBytes(tcpMessage);
    await ClientSocket.SendAsync(data);
    return true;
  }
}