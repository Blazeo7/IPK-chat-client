using ClientServer.Messages;

namespace ClientServer.Clients;

public class UdpClient(string hostname, ushort port, int timeout, int retries)
  : BaseClient(hostname, port) {
  private int _timeout = timeout;
  private int _retries = retries;

  public override void EndConnection() {
    throw new NotImplementedException();
  }

  public override Task<Message> ReceiveMessage() {
    throw new NotImplementedException();
  }

  public override Task SendMessage(Message message) {
    throw new NotImplementedException();
  }

  public override void SetUpConnection() {
    throw new NotImplementedException();
  }
}