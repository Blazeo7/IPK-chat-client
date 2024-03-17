using System.Net;
using System.Net.Sockets;
using System.Text;
using ClientServer;
using ClientServer.Messages;

namespace ClientServerTests.Servers;

public class TcpServer(string host, ushort port) : Server(host, port) {
  private Socket _clientSocket = null!;

  public override void SetUpConnection() {
    Logger.Log("Server up");
    ServerSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    ServerSocket.Bind(new IPEndPoint(IPAddress.Parse(Hostname), Port));
    ServerSocket.Listen(1); // Allow only one pending connection
  }

  public override async Task AcceptClient() {
    try {
      _clientSocket = await ServerSocket.AcceptAsync();
      Logger.Log("Client connected");
    } finally {
      ServerSocket.Close();
    }
  }

  public override void EndConnection() {
    try {
      _clientSocket.Close();
      ServerSocket.Close();
      Logger.Log("Server shut down");
    } catch (SocketException e) {
      Logger.Log($"Socket exception when closing server: {e.Message}");
    }
  }

  public override async Task<bool> SendMessage(Message message) {
    string tcpMessage = message.ToTcpFormat();
    byte[] data = Encoding.Default.GetBytes(tcpMessage);
    await _clientSocket.SendAsync(data);
    return true;
  }

  public override async Task<Message> ReceiveMessage() {
    var buffer = new byte[2048];
    var bytesRead = await _clientSocket.ReceiveAsync(buffer);

    if (bytesRead == 0) {
      Logger.Log("Nothing received from client");
      throw new SocketException();
    }

    // Process the received data
    var receivedMessage = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
    Logger.Log(receivedMessage);
    var message = Message.FromTcpFormat(receivedMessage);
    return message;
  }
}