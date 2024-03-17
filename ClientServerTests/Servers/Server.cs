using System.Net.Sockets;
using ClientServer.Clients;
using ClientServer.Messages;

namespace ClientServerTests.Servers;

public abstract class Server(string hostname, ushort port) : IClient {
  protected Socket ServerSocket = null!;
  public string Hostname { get; init; } = hostname;
  public ushort Port{ get; init; } = port;

  public abstract Task AcceptClient();

  public abstract void SetUpConnection();


  public abstract void EndConnection();

  public abstract Task<bool> SendMessage(Message message);


  public abstract Task<Message> ReceiveMessage();
}