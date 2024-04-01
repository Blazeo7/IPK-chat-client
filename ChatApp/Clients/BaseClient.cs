using System.Net.Sockets;
using ChatApp.Messages;

namespace ChatApp.Clients;

/// <summary>
/// Abstract base class for client communication with a server, providing common functionality for both UDP and TCP clients.
/// </summary>
/// <remarks>
/// This base class defines properties and methods necessary for initializing and managing the connection, as well as sending and receiving messages.
/// </remarks>
/// <param name="host">Remote hostname or IP address</param>
/// <param name="port">Port where server is listening</param>
public abstract class BaseClient(string host, ushort port) : IClient {
  /// <summary>
  /// Remote hostname or IP address
  /// </summary>
  protected string Hostname { get; set; } = host;

  /// <summary>
  /// Port where server is listening
  /// </summary>
  protected int Port { get; set; } = port;

  protected Socket ClientSocket { get; set; } = null!;

  public abstract void SetUpConnection();

  public abstract void EndConnection();

  public abstract Task<bool> SendMessage(Message message);

  public abstract Task<Message> ReceiveMessage();
}