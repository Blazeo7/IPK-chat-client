using ClientServer.Enums;

namespace ClientServer.Messages;

public class TextMessage(string displayName, string content, ushort id = 0) : Message(id) {
  public string DisplayName = displayName;
  public string Content = content;

  public override MsgType MType { get; set; } = MsgType.Msg;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Msg, MsgId, DisplayName, Content);
  }

  public override string ToTcpFormat() {
    return $"MSG FROM {DisplayName} IS {Content}\r\n";
  }

  public override void Print() {
    Console.WriteLine($"{DisplayName}: {Content}");
  }
}