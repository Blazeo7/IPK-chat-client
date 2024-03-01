using System.Text.RegularExpressions;
using System.Text;

namespace ClientServer.Messages;

public enum MsgType {
  Confirm = 0x00,
  Reply = 0x01,
  Auth = 0x02,
  Join = 0x03,
  Msg = 0x04,
  Err = 0xFE,
  Bye = 0xFF,
}

public abstract class Message(short id) {
  public short MsgId { get; init; } = id;

  public abstract MsgType MType { get; set; }

  public virtual byte[] ToUdpFormat() {
    throw new NotImplementedException();
  }

  public virtual string ToTcpFormat() {
    throw new NotImplementedException();
  }

  public virtual void Print() {
    throw new NotImplementedException();
  }

  public static Message FromUdpFormat(byte[] udpMessage) {
    if (udpMessage.Length < 3) { // at least 3 bytes 
      return new ErrorMessage("Server", "Server sent invalid message");
    }

    MsgType type = (MsgType)udpMessage[0];
    short msgId = BitConverter.ToInt16(udpMessage, 1);

    switch (type) {
      case MsgType.Confirm:
        return new ConfirmMessage(msgId);

      case MsgType.Reply:
        byte result = udpMessage[3];
        short refMsgId = BitConverter.ToInt16(udpMessage, 4);
        var content = Encoding.UTF8.GetString(udpMessage, 6, udpMessage.Length - 6);
        return new ReplyMessage(result, content, msgId, refMsgId);

      case MsgType.Auth:
        string[] authParams = Utils.FromBytes(udpMessage, 3);
        if (authParams.Length != 3) { // 3 arguments expected for \auth
          return new ErrorMessage("Server", "Server sent invalid message");
        }

        return new AuthMessage(authParams[0], authParams[1], authParams[2], msgId);

      case MsgType.Join:
        string[] joinParams = Utils.FromBytes(udpMessage, 3);
        if (joinParams.Length != 2) { // 2 arguments expected for \join
          return new ErrorMessage("Server", "Server sent invalid message");
        }

        return new JoinMessage(joinParams[0], joinParams[1], msgId);

      case MsgType.Msg:
        string[] msgParams = Utils.FromBytes(udpMessage, 3);
        if (msgParams.Length != 2) { // 2 arguments expected for text message
          return new ErrorMessage("Server", "Server sent invalid message");
        }

        return new TextMessage(msgParams[0], msgParams[1], msgId);


      case MsgType.Err:
        string[] errParams = Utils.FromBytes(udpMessage, 3);
        if (errParams.Length != 2) { // 2 arguments expected for text message
          return new ErrorMessage("Server", "Server sent invalid message");
        }

        return new ErrorMessage(errParams[0], errParams[1], msgId);

      case MsgType.Bye:
        return new ByeMessage(msgId);

      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public static Message FromTcpFormat(string tcpMessage) {
    // MSG
    var msgMatch = Regex.Match(tcpMessage, @"^MSG FROM ([!-~]+) IS ([ -~]+)$",
      RegexOptions.Multiline);
    if (msgMatch.Success) {
      string displayName = msgMatch.Groups[1].Value;
      string msgContent = msgMatch.Groups[2].Value;
      return new TextMessage(displayName, msgContent);
    }

    // REPLY
    var replyMatch =
      Regex.Match(tcpMessage, @"^REPLY (OK|NOK) IS ([ -~]+)$", RegexOptions.Multiline);
    if (replyMatch.Success) {
      string replyResult = replyMatch.Groups[1].Value;
      string msgContent = replyMatch.Groups[2].Value;

      if (replyResult.Equals("OK")) {
        return new ReplyMessage(1, msgContent);
      }

      return new ReplyMessage(0, msgContent);
    }

    // ERR
    var errMatch = Regex.Match(tcpMessage, @"^ERROR FROM ([!-~]+) IS ([ -~]+)$",
      RegexOptions.Multiline);
    if (errMatch.Success) {
      string displayName = errMatch.Groups[1].Value;
      string msgContent = errMatch.Groups[2].Value;
      return new ErrorMessage(displayName, msgContent);
    }

    // BYE
    var byeMatch = Regex.Match(tcpMessage, @"^BYE$", RegexOptions.Multiline);
    if (byeMatch.Success) {
      return new ByeMessage();
    }

    return new ErrorMessage("Server", "Invalid message from server");
  }
}