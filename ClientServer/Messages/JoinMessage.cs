
namespace ClientServer.Messages;
public class JoinMessage(string channelId, string displayName, short id = 0) : Message(id) {
  public string ChannelId = channelId;
  public string DisplayName = displayName;
  public override MsgType MType { get; set; } = MsgType.Join;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Join, MsgId, ChannelId, DisplayName);
  }

  public override string ToTcpFormat() {
    return $"JOIN {ChannelId} AS {DisplayName}\r\n";
  }

}