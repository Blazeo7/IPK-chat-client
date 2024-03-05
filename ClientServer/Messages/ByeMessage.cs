
using ClientServer.Enums;

namespace ClientServer.Messages;
public class ByeMessage(ushort id = 0) : Message(id) {
  public override MsgType MType { get; set; } = MsgType.Bye;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Bye, MsgId);
  }

  public override string ToTcpFormat() {
    return "BYE\r\n";
  }

}
