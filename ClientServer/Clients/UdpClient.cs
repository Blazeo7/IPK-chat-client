using ClientServer.Messages;
using System.Net.Sockets;
using System.Net;
using ClientServer.Enums;

namespace ClientServer.Clients;

public class UdpClient(string hostname, ushort port, int timeout, int retries)
  : BaseClient(hostname, port) {
  private EndPoint _remoteIpEndPoint = null!;
  private readonly Queue<Message> _receivedMessages = new(5); // the 5 last received message
  private int? _msgToBeConfirmed; // id of the message that has to be confirmed
  private int? _expectedReplyId; // id of the message that reply has to refer
  private readonly AutoResetEvent _receiveAccessEvent = new(false);
  private readonly ManualResetEvent _confirmAccessEvent = new(false);
  private readonly HashSet<int> _confirmedMessages = [];

  public override void SetUpConnection() {
    try {
      ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      ClientSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

      IPAddress ipv4 = Utils.ConvertHostname2IPv4(Hostname);
      _remoteIpEndPoint = new IPEndPoint(ipv4, Port);

      Task.Run(ContinuousReceiving);
    } catch (SocketException) {
      Utils.PrintInternalError($"Cannot connect to {Hostname}:{Port}");
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

      Message recMessage = Message.FromUdpFormat(recBuffer);

      Logger.Log("Client received", recMessage);

      // Handle Confirmation
      if (recMessage.MType is MsgType.Confirm) {
        HandleConfirmMessage(recMessage);
        continue; // Continue receiving without notifying `ReceiveMessage` method
      }

      // Confirm whatever message was received
      SendConfirm(recMessage.Id);

      // Skip messages that had been received
      if (!_confirmedMessages.Add(recMessage.Id)) continue;

      // Handle Reply message
      if (recMessage.MType is MsgType.Reply) {
        // Update remote address endpoint after receiving reply message
        _remoteIpEndPoint = receiveResult.RemoteEndPoint;

        if (!CheckReplyMessage((ReplyMessage)recMessage)) {
          recMessage = new InvalidMessage(Content: "Got an invalid reply message");
        }
      }

      _receivedMessages.Enqueue(recMessage);

      if (_receivedMessages.Count == 1) {
        _receiveAccessEvent.Set(); // Notify `ReceiveMessage` method
      }
    }
  }

  /// <summary>Check if the <see cref="message"/> refer to the correct message (last sent).</summary>
  private bool CheckReplyMessage(ReplyMessage message) {
    ushort refId = message.RefMsgId;

    // Incorrect reply message
    if (refId != _expectedReplyId) {
      Logger.Log($"Expected: {_expectedReplyId}, got: {refId}", message);
      return false;
    }

    // No reply is expected from now on
    _expectedReplyId = null;

    return true;
  }

  /// <summary>
  /// If the confirmation message reference to the correct message, notify `SendMessage` about
  /// successful confirmation otherwise ignore this message.
  /// </summary>
  private void HandleConfirmMessage(Message recMessage) {
    Logger.Log($"Expected: {_msgToBeConfirmed}, got: {recMessage.Id}", recMessage);

    // Ignore invalid confirm message
    if (_msgToBeConfirmed == null || recMessage.Id != _msgToBeConfirmed) return;

    Logger.Log("Confirmed!", recMessage);

    _msgToBeConfirmed = null;

    // Notify `SendMessage` method about successful confirmation
    _confirmAccessEvent.Set();
  }

  /// <summary>Sends confirmation message with the <see cref="id"/> to the server</summary>
  private void SendConfirm(ushort id) {
    var confirmation = new ConfirmMessage(id);
    Logger.Log("Client sent", confirmation);
    ClientSocket.SendToAsync(confirmation.ToUdpFormat(), _remoteIpEndPoint);
  }

  public override async Task<Message> ReceiveMessage() {
    if (_receivedMessages.Count != 0) return _receivedMessages.Dequeue();

    // Wait if no message in the queue
    await Task.Run(_receiveAccessEvent.WaitOne);

    return _receivedMessages.Dequeue();
  }

  public override async Task<bool> SendMessage(Message message) {
    Logger.Log("Client sent", message);

    _msgToBeConfirmed = message.Id;

    // Expect reply when after `Auth` or `Join` is sent
    if (message.MType is MsgType.Auth or MsgType.Join) {
      _expectedReplyId = message.Id;
    }

    byte[] data = message.ToUdpFormat();

    for (int i = 0; i < retries + 1; i++) {
      await ClientSocket.SendToAsync(data, _remoteIpEndPoint);

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