
using ClientServer.Enums;

namespace ClientServer.Messages;
public record JoinMessage(string ChannelId, string DisplayName, ushort Id = 0) : Message(Id) {
  public override MsgType MType { get; set; } = MsgType.Join;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Join, Id, ChannelId, DisplayName);
  }

  public override string ToTcpFormat() {
    return $"JOIN {ChannelId} AS {DisplayName}\r\n";
  }
}