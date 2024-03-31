using System.Text.RegularExpressions;
using ClientServer.Enums;

namespace ClientServer.Messages;

public abstract record Message(ushort Id) {
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
    if (udpMessage.Length < 3) return new InvalidMessage("Too short message");

    MsgType type = (MsgType)udpMessage[0];
    ushort msgId = Utils.HostToNetworkOrder(BitConverter.ToUInt16(udpMessage, 1));

    switch (type) {
      case MsgType.Confirm:
        return new ConfirmMessage(msgId);

      case MsgType.Reply:
        byte result = udpMessage[3];
        ushort refMsgId = Utils.HostToNetworkOrder(BitConverter.ToUInt16(udpMessage, 4));
        string[] replyParams = Utils.FromBytes(udpMessage, 6);

        if (replyParams.Length != 1)
          return new InvalidMessage(Id: msgId, Content: "Invalid reply message");

        return new ReplyMessage(Utils.ReplyResultFromInt(result), replyParams[0], msgId, refMsgId);

      case MsgType.Auth:
        string[] authParams = Utils.FromBytes(udpMessage, 3);

        if (authParams.Length != 3) // 3 arguments expected for \auth
          return new InvalidMessage(Id: msgId, Content: "Server sent invalid message");

        return new AuthMessage(authParams[0], authParams[2], authParams[1], msgId);

      case MsgType.Join:
        string[] joinParams = Utils.FromBytes(udpMessage, 3);
        if (joinParams.Length != 2) // 2 arguments expected for \join
          return new InvalidMessage(Id: msgId, Content: "Server sent invalid message");

        return new JoinMessage(joinParams[0], joinParams[1], msgId);

      case MsgType.Msg:
        string[] msgParams = Utils.FromBytes(udpMessage, 3);
        if (msgParams.Length != 2) // 2 arguments expected for text message
          return new InvalidMessage(Id: msgId, Content: "Server sent invalid message");

        return new TextMessage(msgParams[0], msgParams[1], msgId);


      case MsgType.Err:
        string[] errParams = Utils.FromBytes(udpMessage, 3);
        if (errParams.Length != 2) // 2 arguments expected for text message
          return new InvalidMessage(Id: msgId, Content: "Server sent invalid message");

        return new ErrorMessage(DisplayName: errParams[0], Content: errParams[1], Id: msgId);

      case MsgType.Bye:
        return new ByeMessage(msgId);

      default:
        return new InvalidMessage(Id: msgId, Content: $"code: {udpMessage[0]}");
    }
  }

  public static Message FromTcpFormat(string tcpMessage) {
    // MSG
    var msgMatch = Regex.Match(tcpMessage, @"^MSG FROM ([!-~]{1,20}) IS ([ -~]{1,1400})\r\n$",
      RegexOptions.IgnoreCase);
    if (msgMatch.Success) {
      string displayName = msgMatch.Groups[1].Value;
      string msgContent = msgMatch.Groups[2].Value;
      return new TextMessage(displayName, msgContent);
    }

    // REPLY
    var replyMatch =
      Regex.Match(tcpMessage, @"^REPLY (OK|NOK) IS ([ -~]{1,1400})\r\n$", RegexOptions.IgnoreCase);

    if (replyMatch.Success) {
      string replyResult = replyMatch.Groups[1].Value.ToUpper();
      string msgContent = replyMatch.Groups[2].Value;

      return replyResult switch {
        "OK" => new ReplyMessage(ReplyResult.Ok, msgContent),
        "NOK" => new ReplyMessage(ReplyResult.Nok, msgContent),
        _ => new InvalidMessage("Invalid reply result")
      };
    }

    // ERR
    var errMatch = Regex.Match(tcpMessage, @"^ERR FROM ([!-~]{1,20}) IS ([ -~]{1,1400})\r\n$",
      RegexOptions.IgnoreCase);
    if (errMatch.Success) {
      string displayName = errMatch.Groups[1].Value;
      string msgContent = errMatch.Groups[2].Value;
      return new ErrorMessage(DisplayName: displayName, Content: msgContent);
    }

    // AUTH
    var authMatch = Regex.Match(tcpMessage,
      @"^AUTH ([a-zA-Z0-9-]{1,20}) AS ([!-~]{1,20}) USING ([a-zA-Z0-9-]{1,128})\r\n$",
      RegexOptions.IgnoreCase);
    if (authMatch.Success) {
      string username = authMatch.Groups[1].Value;
      string displayName = authMatch.Groups[2].Value;
      string secret = authMatch.Groups[3].Value;
      return new AuthMessage(username, secret, displayName);
    }

    // JOIN
    var joinMatch = Regex.Match(tcpMessage, @"^JOIN ([a-zA-Z0-9-]{1,20}) AS ([!-~]{1,20})\r\n$",
      RegexOptions.IgnoreCase);
    if (joinMatch.Success) {
      string channelId = joinMatch.Groups[1].Value;
      string displayName = joinMatch.Groups[2].Value;
      return new JoinMessage(channelId, displayName);
    }

    // BYE
    var byeMatch = Regex.Match(tcpMessage, @"^BYE\r\n$", RegexOptions.IgnoreCase);
    if (byeMatch.Success) {
      return new ByeMessage();
    }

    return new InvalidMessage("INVALID MESSAGE");
  }
}