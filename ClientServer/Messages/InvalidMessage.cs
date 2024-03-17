using ClientServer.Enums;

namespace ClientServer.Messages;

public record InvalidMessage(string Content, ushort Id = 0) : Message(Id) {
  public override MsgType MType { get; set; } = MsgType.Invalid;

  public override void Print() {
   Utils.PrintInternalError($"ERR: Invalid message: {Content}");
  }

  public override string ToTcpFormat() {
    return "INVALID MESSAGE\r\n";
  }

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes([(byte)MType, Id], Content);
  }
}