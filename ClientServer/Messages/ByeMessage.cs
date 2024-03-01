
namespace ClientServer.Messages;
public class ByeMessage(short id = 0) : Message(id) {
  public override MsgType MType { get; set; } = MsgType.Bye;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Bye, MsgId);
  }

  public override string ToTcpFormat() {
    return "BYE\r\n";
  }

}
