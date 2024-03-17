using ClientServer.Clients;

namespace ClientServer;

public class Program {
  public static async Task Main(string[] args) {
    CommandLineParser.Options options = CommandLineParser.GetCommandLineArguments(args);

    IClient client = options.Client switch {
      "tcp" => new TcpClient(options.Hostname, (ushort)options.Port),
      "udp" => new UdpClient(options.Hostname, (ushort)options.Port, options.Timeout,
        options.Retransmissions),
      _ => throw new ArgumentException("Invalid client name"),
    };

    Logger.Init(options.Verbose);
    Logger.Log("Starting program");

    ChatClient chat = new(client);
    try {
      await chat.Communicate();
    } catch (Exception e) {
      Logger.Log($"Exception: {e.Message}");
      Environment.Exit(1);
    }
  }
}
