using ClientServer.Enums;

namespace ClientServer.Messages;

public class ReplyMessage(byte result, string content, ushort id = 0, ushort refMsgId = 0)
  : Message(id) {
  public byte Result = result;
  public ushort RefMsgId = refMsgId;
  public string Content = content;
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