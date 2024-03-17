using ClientServer.Messages;
using System.Net.Sockets;
using System.Net;
using ClientServer.Enums;

namespace ClientServer.Clients;

public class UdpClient(string hostname, ushort port, int timeout, int retries)
  : BaseClient(hostname, port) {
  private EndPoint _remoteIpEndPoint = null!;
  private Message? _receivedMessage; // the last received message
  private int? _msgToBeConfirm; // id of the message that has to be confirmed
  private int? _expectedReplyId; // id of the message that reply has to refer
  private readonly ManualResetEvent _receiveAccessEvent = new(false);
  private readonly ManualResetEvent _confirmAccessEvent = new(false);

  public override void SetUpConnection() {
    try {
      ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

      IPAddress ipv4 = Utils.ConvertHostname2IPv4(HostName);
      _remoteIpEndPoint = new IPEndPoint(ipv4, Port);

      Task.Run(ContinuousReceiving);
    } catch (SocketException) {
      Utils.PrintInternalError($"Cannot connect to {HostName}:{Port}");
      Environment.Exit(1);
    }
  }


  public override void EndConnection() {
    _receiveAccessEvent.Dispose();
    _confirmAccessEvent.Dispose();
    ClientSocket.Close();
    Logger.Log("Client's connection closed");
  }

  private async Task ContinuousReceiving() {
    while (true) {
      byte[] recBuffer = new byte[2048];

      SocketReceiveFromResult receiveResult =
        await ClientSocket.ReceiveFromAsync(recBuffer, SocketFlags.None, _remoteIpEndPoint);

      // Ignore trash messages
      if (receiveResult.ReceivedBytes < 3) {
        continue;
      }

      // Convert the received message in bytes to `Message`
      Message recMessage = Message.FromUdpFormat(recBuffer);

      Logger.Log("Client received", recMessage);

      switch (recMessage.MType) {
        case MsgType.Confirm:
          HandleConfirmMessage(recMessage, receiveResult);
          continue; // Continue receiving without notifying `ReceiveMessage()` thread

        case MsgType.Reply:
          int refId = ((ReplyMessage)recMessage).RefMsgId;

          // incorrect reply message
          if (refId != _expectedReplyId) {
            Logger.Log($"Expected: {_expectedReplyId}, got: {refId})", recMessage);

            new ErrorMessage(content: "Got an invalid reply message").Print();
            continue; // Continue receiving without notifying `ReceiveMessage()` thread
          }

          // Reply is referring to the correct message
          _expectedReplyId = null;
          break;
      }

      SendConfirm(recMessage.MsgId);
      _receivedMessage = recMessage;

      _receiveAccessEvent.Set(); // Release the `ReceiveMessage()` thread
    }
  }

  private void HandleConfirmMessage(Message recMessage, SocketReceiveFromResult receiveResult) {
    Logger.Log($"Expected: {_msgToBeConfirm}, got: {recMessage.MsgId}", recMessage);

    // Invalid confirm received => ignored
    if (_msgToBeConfirm == null || recMessage.MsgId != _msgToBeConfirm) return;

    Logger.Log("Confirmed!", recMessage);

    // Update remote address endpoint after the first message from the server
    _remoteIpEndPoint = receiveResult.RemoteEndPoint;

    // Update remote endpoint
    _msgToBeConfirm = null;
    _confirmAccessEvent.Set(); // Release `SendMessage()` thread
  }

  /// <summary>
  /// Sends one confirmation message to the server
  /// </summary>
  /// <param name="id"></param>
  private void SendConfirm(ushort id) {
    Logger.Log($"Confirming message {id}");
    var confirmation = new ConfirmMessage(id);
    Logger.Log("Client sent", confirmation);
    ClientSocket.SendToAsync(confirmation.ToUdpFormat(), _remoteIpEndPoint);
  }

  public override async Task<Message> ReceiveMessage() {
    // Wait until other thread release it
    await Task.Run(_receiveAccessEvent.WaitOne);

    // reset to force waiting when calling `WaitOne`
    _receiveAccessEvent.Reset();

    return _receivedMessage!;
  }

  public override async Task<bool> SendMessage(Message message) {
    byte[] data = message.ToUdpFormat();
    Logger.Log("Client sent", message);

    if (message.MType is MsgType.Auth or MsgType.Join) {
      Logger.Log($"Expecting reply id: {message.MsgId}");
      _expectedReplyId = message.MsgId;
    }

    for (int i = 0; i < retries + 1; i++) {
      await ClientSocket.SendToAsync(data, _remoteIpEndPoint);
      Logger.Log($"Sent", message);
      _msgToBeConfirm = message.MsgId;

      // Set up a delay task for the specified timeout
      Task timeoutTask = Task.Delay(millisecondsDelay: timeout);
      Task confirmTask = Task.Run(_confirmAccessEvent.WaitOne);

      await Task.WhenAny(timeoutTask, confirmTask);

      if (confirmTask.IsCompleted) {
        _confirmAccessEvent.Reset();
        return true; // Message was successfully received by server
      }

      Logger.Log("Confirmation timeout", message);
    }

    return false;
  }
}