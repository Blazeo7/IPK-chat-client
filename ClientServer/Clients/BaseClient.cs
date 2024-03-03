using System.Net.Sockets;
using ClientServer.Messages;

namespace ClientServer.Clients;

public abstract class BaseClient(string host, ushort port) {
  public string HostName { get; set; } = host;
  public int Port { get; set; } = port;

  public Socket ClientSocket { get; set; } = null!;

  public abstract void SetUpConnection();
  public abstract void EndConnection();
  public abstract Task SendMessage(Message message);
  public abstract Task<Message> ReceiveMessage();
}