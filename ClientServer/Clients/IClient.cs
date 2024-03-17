using ClientServer.Messages;

namespace ClientServer.Clients;

public interface IClient {
  
  /// <summary>
  /// Sets up the connection for the client or server.
  /// </summary>
  public void SetUpConnection();

  
  /// <summary>
  /// Close the connection and release all associated resources
  /// </summary>
  public void EndConnection();
  
  /// <summary>
  /// Sends message to the remote host asynchronously.
  /// </summary>
  /// <returns><c>true</c> if the message was successfully sent to the remote host otherwise, <c>false</c>.</returns>
  public Task<bool> SendMessage(Message message);

  
  /// <summary>
  /// Asynchronously receives a message from the socket.
  /// </summary>
  public Task<Message> ReceiveMessage();
}