using ClientServer.Enums;

namespace ClientServer.Messages;

public record ByeMessage(ushort Id = 0) : Message(Id) {
  public override MsgType MType { get; set; } = MsgType.Bye;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Bye, Id);
  }

  public override string ToTcpFormat() {
    return "BYE\r\n";
  }
}
