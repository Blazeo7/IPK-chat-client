using ClientServer.Enums;

namespace ClientServer.Messages;

public record AuthMessage(string Username, string Secret, string DisplayName, ushort Id = 0)
  : Message(Id) {
  public override MsgType MType { get; set; } = MsgType.Auth;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Auth, Id, Username, DisplayName, Secret);
  }

  public override string ToTcpFormat() {
    return $"AUTH {Username} AS {DisplayName} USING {Secret}\r\n";
  }
}