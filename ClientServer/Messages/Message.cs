using System.Text.RegularExpressions;
using System.Text;
using ClientServer.Enums;

namespace ClientServer.Messages;

public abstract class Message(ushort id) {
  public ushort MsgId { get; init; } = id;

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
    MsgType type = (MsgType)udpMessage[0];
    ushort msgId = BitConverter.ToUInt16(udpMessage, 1);

    switch (type) {
      case MsgType.Confirm:
        return new ConfirmMessage(msgId);

      case MsgType.Reply:
        byte result = udpMessage[3];
        ushort refMsgId = BitConverter.ToUInt16(udpMessage, 4);
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
        return new ErrorMessage(displayName: "Server",
          content: $"Invalid message code: `{udpMessage[0]}`");
    }
  }

  public static Message FromTcpFormat(string tcpMessage) {
    // MSG
    var msgMatch = Regex.Match(tcpMessage, @"^MSG FROM ([!-~]+) IS ([ -~]+)\r\n$",
      RegexOptions.Multiline);
    if (msgMatch.Success) {
      string displayName = msgMatch.Groups[1].Value;
      string msgContent = msgMatch.Groups[2].Value;
      return new TextMessage(displayName, msgContent);
    }

    // REPLY
    var replyMatch =
      Regex.Match(tcpMessage, @"^REPLY (OK|NOK) IS ([ -~]+)\r\n$", RegexOptions.Multiline);
    if (replyMatch.Success) {
      string replyResult = replyMatch.Groups[1].Value;
      string msgContent = replyMatch.Groups[2].Value;

      return replyResult switch {
        "OK" => new ReplyMessage(ReplyResult.Ok, msgContent),
        "NOK" => new ReplyMessage(ReplyResult.Nok, msgContent),
        _ => new InvalidMessage("Invalid reply result")
      };
    }

    // ERR
    var errMatch = Regex.Match(tcpMessage, @"^ERR FROM ([!-~]+) IS ([ -~]+)\r\n$",
      RegexOptions.Multiline);
    if (errMatch.Success) {
      string displayName = errMatch.Groups[1].Value;
      string msgContent = errMatch.Groups[2].Value;
      return new ErrorMessage(displayName, msgContent);
    }

    // BYE
    var byeMatch = Regex.Match(tcpMessage, @"^BYE\r\n$", RegexOptions.Multiline);
    if (byeMatch.Success) {
      return new ByeMessage();
    }

    return new ErrorMessage($"`{tcpMessage}`", displayName: "Server");
  }
}