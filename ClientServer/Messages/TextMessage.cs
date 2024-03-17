using ClientServer.Enums;

namespace ClientServer.Messages;

public record TextMessage(string DisplayName, string Content, ushort Id = 0) : Message(Id) {

  public override MsgType MType { get; set; } = MsgType.Msg;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes([(byte)MsgType.Msg, Id], DisplayName, Content);
  }

  public override string ToTcpFormat() {
    return $"MSG FROM {DisplayName} IS {Content}\r\n";
  }

  public override void Print() {
    Console.WriteLine($"{DisplayName}: {Content}");
  }
}