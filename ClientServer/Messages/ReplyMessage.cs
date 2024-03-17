using ClientServer.Enums;

namespace ClientServer.Messages;

public record ReplyMessage(ReplyResult Result, string Content, ushort Id = 0, ushort RefMsgId = 0)
  : Message(Id) {
  public ReplyResult Result { get; set; } = Result;
  public ushort RefMsgId { get; set; } = RefMsgId;
  public override MsgType MType { get; set; } = MsgType.Reply;

  public override void Print() {
    switch (Result) {
      case ReplyResult.Ok:
        Console.Error.WriteLine($"Success: {Content}");
        break;

      case ReplyResult.Nok:
        Console.Error.WriteLine($"Failure: {Content}");
        break;

      default:
        Utils.PrintInternalError("Invalid reply message result");
        break;
    }
  }
}