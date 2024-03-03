using ClientServer.Enums;

namespace ClientServer.Messages;

public class ReplyMessage(byte result, string content, short id = 0, short refMsgId = 0)
  : Message(id) {
  public byte Result = result;
  public short RefMsgId = refMsgId;
  public string Content = content;
  public override MsgType MType { get; set; } = MsgType.Reply;

  public override void Print() {
    Console.Error.WriteLine(Result == 1 ? $"Success: {Content}" : $"Failure: {Content}");
  }
}