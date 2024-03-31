using System.Net;
using System.Net.Sockets;
using ClientServer;
using ClientServer.Enums;
using ClientServer.Messages;
using static System.Net.Sockets.SocketFlags;

namespace ClientServerTests.Servers;

public class UdpServer(string host, ushort port, int retries, int timeout) : Server(host, port) {
  private EndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
  public int Retries { get; init; } = retries;
  public int Timeout { get; init; } = timeout;

  public override void SetUpConnection() {
    Logger.Log("Server up");
    ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    ServerSocket.Bind(new IPEndPoint(IPAddress.Parse(Hostname), Port));
  }

  public override async Task AcceptClient() {
    await Task.Delay(millisecondsDelay: 1);
  }

  public override void EndConnection() {
    ServerSocket.Close();
  }

  public override async Task<bool> SendMessage(Message message) {
    byte[] data = message.ToUdpFormat();
    await ServerSocket.SendToAsync(data, _clientEndPoint);
    return true;
  }

  public override async Task<Message> ReceiveMessage() {
    byte[] recBuffer = new byte[2048];

      var receiveResult = await
        ServerSocket.ReceiveFromAsync(recBuffer, None, _clientEndPoint);

      // Update remote address endpoint after the first message from the server
      _clientEndPoint = receiveResult.RemoteEndPoint;

      Message message = Message.FromUdpFormat(recBuffer);

      return message;
    }
  }