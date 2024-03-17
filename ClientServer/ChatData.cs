using ClientServer.Enums;

namespace ClientServer;

public class ChatData {
  public string DisplayName = "default";
  public State CurrentState { get; set; } = State.Start;

  public bool Connected;
  public bool ReplyExpected;
}