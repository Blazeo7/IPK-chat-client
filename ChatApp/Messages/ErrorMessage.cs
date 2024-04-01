
using ChatApp.Enums;

namespace ChatApp.Messages;

public record ErrorMessage(string Content, string DisplayName, ushort Id = 0)
  : Message(Id) {
  public override MsgType MType { get; set; } = MsgType.Err;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Err, Id, DisplayName, Content);
  }

  public override string ToTcpFormat() {
    return $"ERR FROM {DisplayName} IS {Content}\r\n";
  }

  public override void Print() {
    Console.Error.WriteLine($"ERR FROM {DisplayName}: {Content}");
  }
}