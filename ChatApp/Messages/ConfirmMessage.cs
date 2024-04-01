
using ChatApp.Enums;

namespace ChatApp.Messages;

public record ConfirmMessage(ushort Id = 0) : Message(Id) {
  public override MsgType MType { get; set; } = MsgType.Confirm;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Confirm, Id);
  }
}