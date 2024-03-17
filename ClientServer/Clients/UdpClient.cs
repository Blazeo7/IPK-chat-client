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

      Message recMessage = Message.FromUdpFormat(recBuffer);

      Logger.Log("Client received", recMessage);

      switch (recMessage.MType) {
        case MsgType.Confirm:
          HandleConfirmMessage(recMessage);
          continue; // Continue receiving without notifying `ReceiveMessage` method

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

  /// <summary>
  /// If the confirmation message reference to the correct message, notify `SendMessage` about
  /// successful confirmation otherwise ignore this message.
  /// </summary>
  private void HandleConfirmMessage(Message recMessage) {
    Logger.Log($"Expected: {_msgToBeConfirm}, got: {recMessage.Id}", recMessage);

    // Ignore invalid confirm message
    if (_msgToBeConfirm == null || recMessage.Id != _msgToBeConfirm) return;

    Logger.Log("Confirmed!", recMessage);

    // Update remote address endpoint after the first message from the server
    _remoteIpEndPoint = receiveResult.RemoteEndPoint;

    // Update remote endpoint
    _msgToBeConfirm = null;

    // Notify `SendMessage` method about successful confirmation
    _confirmAccessEvent.Set();
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
      _expectedReplyId = message.Id;
    }

    for (int i = 0; i < retries + 1; i++) {
      await ClientSocket.SendToAsync(data, _remoteIpEndPoint);
      _msgToBeConfirm = message.Id;

      Task timeoutTask = Task.Delay(millisecondsDelay: timeout);
      Task confirmTask = Task.Run(_confirmAccessEvent.WaitOne);

      await Task.WhenAny(timeoutTask, confirmTask);

      if (confirmTask.IsCompleted) {
        _confirmAccessEvent.Reset();
        return true; // Message was confirmed
      }

      Logger.Log("Confirmation timeout", message);
    }

    return false; // Message was not confirmed
  }
}