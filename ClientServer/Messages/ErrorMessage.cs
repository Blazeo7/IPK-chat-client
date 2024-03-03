
using ClientServer.Enums;

namespace ClientServer.Messages;

public class ErrorMessage(string content, string? displayName = null, short id = 0) : Message(id) {
  public string? DisplayName = displayName;
  public string Content = content;
  public override MsgType MType { get; set; } = MsgType.Err;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Err, MsgId, DisplayName!, Content);
  }

  public override string ToTcpFormat() {
    return $"ERROR FROM {DisplayName!} IS {Content}\r\n";
  }

  public override void Print() {
    Console.Error.WriteLine(DisplayName == null
      ? $"ERROR: {Content}"
      : $"ERROR FROM {DisplayName}: {Content}");
  }
}