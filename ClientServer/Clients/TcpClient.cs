using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using ClientServer.Messages;

namespace ClientServer.Clients;

public class TcpClient(string hostname, ushort port) : BaseClient(hostname, port) {
  private readonly Queue<string> _receivedMessages = [];
  private string _incompleteMessage = "";

  public override void SetUpConnection() {
    try {
      ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      IPAddress ipv4 = Utils.ConvertHostname2IPv4(Hostname);
      ClientSocket.Connect(ipv4, Port);
    } catch (SocketException) {
      Utils.PrintInternalError($"Cannot connect to {Hostname}:{Port}");
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

    if (_receivedMessages.Count == 0) {
      await ClientSocket.ReceiveAsync(buffer);
    } else {
      return Message.FromTcpFormat(_receivedMessages.Dequeue());
    }

    string receivedMessage = Encoding.ASCII.GetString(buffer).TrimEnd('\0');

    ProcessReceivedStream(receivedMessage);

    return Message.FromTcpFormat(_receivedMessages.Dequeue());
  }

  /// <summary>
  /// Handles the received stream of messages from the socket. Splits the stream into individual
  /// messages, queues them for processing, and ensures incomplete messages are handled properly.
  /// </summary>
  /// <param name="receivedMessage">The received stream of messages.</param>
  private void ProcessReceivedStream(string receivedMessage) {
    // Split the received message stream into individual messages.
    string[] messages = Regex.Split(receivedMessage, @"(?<=\r\n)")
      .Where(i => i.Length > 0)
      .ToArray();

    // Combine any incomplete message from previous receive operations with the first complete message.
    string fixedMessage = _incompleteMessage + messages.First();
    _incompleteMessage = ""; // Reset the incomplete message holder.

    // Queue the fixed message for further processing.
    _receivedMessages.Enqueue(fixedMessage);

    // Process remaining complete messages and handle any incomplete messages.
    for (int i = 1; i < messages.Length; i++) {
      // If the current message does not end with "\n", it's incomplete.
      // Save it for the next receive operation and break out of the loop.
      if (!messages[i].EndsWith('\n')) {
        _incompleteMessage = messages[i];
        break;
      }

      // Otherwise, queue the complete message for processing.
      _receivedMessages.Enqueue(messages[i]);
    }
  }


  public override async Task<bool> SendMessage(Message message) {
    Logger.Log("Client sent", message);
    string tcpMessage = message.ToTcpFormat();
    byte[] data = Encoding.ASCII.GetBytes(tcpMessage);
    await ClientSocket.SendAsync(data);
    return true;
  }
}