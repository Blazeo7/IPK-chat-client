
using ClientServer.Enums;

namespace ClientServer.Messages;

public class ConfirmMessage(ushort id = 0) : Message(id) {
  public override MsgType MType { get; set; } = MsgType.Confirm;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Confirm, MsgId);
  }

  // Tcp does not use Confirmation messages
  public override string ToTcpFormat() {
    throw new NotImplementedException();
  }

}