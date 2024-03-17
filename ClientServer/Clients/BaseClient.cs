using System.Net.Sockets;
using ClientServer.Messages;

namespace ClientServer.Clients;

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
  public string HostName { get; set; } = host;

  /// <summary>
  /// Port where server is listening
  /// </summary>
  public int Port { get; set; } = port;

  public Socket ClientSocket { get; set; } = null!;

  /// <summary>
  /// Initialize <see cref="ClientSocket"/> for establishing a connection using the specified
  /// transport protocol.
  /// </summary>
  public abstract void SetUpConnection();


  /// <summary>
  /// Close the connection
  /// </summary>
  public abstract void EndConnection();

  /// <summary>
  /// Sends message
  /// </summary>
  /// <param name="message">Message to be sent</param>
  /// <returns>True if the message was received by receiver, otherwise false</returns>
  public abstract Task<bool> SendMessage(Message message);


  /// <summary>
  /// Gets message from the server
  /// </summary>
  /// <returns><see cref="Message"/> that represents the server message</returns>
  public abstract Task<Message> ReceiveMessage();
}