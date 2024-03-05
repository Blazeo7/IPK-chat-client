using ClientServer.Enums;

namespace ClientServer.Messages;

public class AuthMessage(string username, string secret, string displayName, ushort id = 0)
  : Message(id) {
  public string Username = username;
  public string Secret = secret;
  public string DisplayName = displayName;

  public override MsgType MType { get; set; } = MsgType.Auth;

  public override byte[] ToUdpFormat() {
    return Utils.AsBytes((byte)MsgType.Auth, MsgId, Username, Secret, DisplayName);
  }

  public override string ToTcpFormat() {
    return $"AUTH {Username} AS {DisplayName} USING {Secret}\r\n";
  }
}